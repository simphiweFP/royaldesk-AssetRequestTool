using System.Net;
using System.Net.Mail;
using Dapper;
using Microsoft.Extensions.Options;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.HelpDesk;

namespace RoyalDesk.Api.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "smtp.office365.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseTls { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string ItTeamAddress { get; set; } = "itteam@royaltyres.co.za";
    public string JustIctAddress { get; set; } = "support@justict.co.za";
}

public sealed class OutboxEmail
{
    public int Id { get; set; }
    public int? TicketId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
}

public interface IEmailOutbox
{
    Task EnqueueAndTrySendAsync(int? ticketId, string recipient, string? cc, string subject, string body, CancellationToken ct = default);
    Task RetryPendingAsync(CancellationToken ct = default);
}

public sealed class EmailOutbox(
    IDbConnectionFactory connectionFactory,
    IOptions<SmtpOptions> options,
    ILogger<EmailOutbox> logger) : IEmailOutbox
{
    private readonly SmtpOptions _options = options.Value;

    public async Task EnqueueAndTrySendAsync(int? ticketId, string recipient, string? cc, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipient)) return;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO EmailOutbox (TicketId, Recipient, Cc, Subject, Body, Status, Attempts, CreatedAtUtc)
            VALUES (@TicketId, @Recipient, @Cc, @Subject, @Body, 'Pending', 0, @CreatedAtUtc);
            SELECT last_insert_rowid();
            """, new { TicketId = ticketId, Recipient = recipient.Trim(), Cc = cc, Subject = subject, Body = body, CreatedAtUtc = DateTime.UtcNow }, cancellationToken: ct));

        await TrySendAsync(new OutboxEmail { Id = id, TicketId = ticketId, Recipient = recipient.Trim(), Cc = cc, Subject = subject, Body = body }, ct);
    }

    public async Task RetryPendingAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = (await connection.QueryAsync<OutboxEmail>(new CommandDefinition("""
            SELECT Id, TicketId, Recipient, Cc, Subject, Body, Status, Attempts
            FROM EmailOutbox
            WHERE Status IN ('Pending', 'Failed') AND Attempts < 5
            ORDER BY CreatedAtUtc
            LIMIT 20;
            """, cancellationToken: ct))).AsList();

        foreach (var email in rows)
        {
            if (ct.IsCancellationRequested) break;
            await TrySendAsync(email, ct);
        }
    }

    private async Task TrySendAsync(OutboxEmail email, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Password))
                throw new InvalidOperationException("SMTP password is not configured. Set Smtp__Password (or SMTP_PASSWORD mapped to configuration) on the server.");

            using var message = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(_options.FromAddress) ? _options.Username : _options.FromAddress),
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = false
            };
            message.To.Add(email.Recipient);
            if (!string.IsNullOrWhiteSpace(email.Cc))
                foreach (var address in email.Cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    message.CC.Add(address);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseTls,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };
            ct.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, ct);
            await MarkAsync(email.Id, "Sent", null, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RoyalDesk email {EmailId} could not be sent; it remains in the outbox.", email.Id);
            await MarkAsync(email.Id, "Failed", ex.Message, ct);
        }
    }

    private async Task MarkAsync(int id, string status, string? error, CancellationToken ct)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE EmailOutbox
            SET Status = @Status,
                Attempts = Attempts + 1,
                LastError = @Error,
                SentAtUtc = CASE WHEN @Status = 'Sent' THEN @Now ELSE SentAtUtc END
            WHERE Id = @Id;
            """, new { Id = id, Status = status, Error = error, Now = DateTime.UtcNow }, cancellationToken: ct));
    }
}

public sealed class EmailOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IEmailOutbox>().RetryPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RoyalDesk email outbox retry failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

public sealed class TicketNotificationService(IEmailOutbox outbox, IOptions<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value;

    public async Task TicketCreatedAsync(int ticketId, string ticketNumber, string requesterName, string? requesterEmail,
        string branch, string department, string itemType, string reason, string? trackingPin, CancellationToken ct = default)
    {
        var teamBody = $"""
            New RoyalDesk IT request

            Ticket: {ticketNumber}
            Employee: {requesterName}
            Branch: {branch}
            Department: {department}
            Request: {itemType}
            Details: {reason}

            Status: New
            Open RoyalDesk > IT Tickets to assign and manage this request.
            """;
        await outbox.EnqueueAndTrySendAsync(ticketId, _options.ItTeamAddress, null, $"RoyalDesk {ticketNumber} - New IT Request", teamBody, ct);

        if (!string.IsNullOrWhiteSpace(requesterEmail))
        {
            var requesterBody = $"""
                Hi {requesterName},

                Your RoyalDesk IT request has been received.

                Ticket: {ticketNumber}
                Status: Submitted
                Request: {itemType}

                Keep your ticket number. You can use it together with your email address to track progress in RoyalDesk.

                Royal Tyres IT
                """;
            await outbox.EnqueueAndTrySendAsync(ticketId, requesterEmail, null, $"RoyalDesk {ticketNumber} - Request Received", requesterBody, ct);
        }
    }

    public Task AssignedAsync(HelpDeskTicketDto ticket, string technicianName, CancellationToken ct = default)
        => NotifyRequesterAsync(ticket, "Looking Into It",
            $"Your request has been assigned to {technicianName}. Our IT team is looking into it.", ct);

    public Task StartedAsync(HelpDeskTicketDto ticket, string technicianName, CancellationToken ct = default)
        => NotifyRequesterAsync(ticket, "In Progress",
            $"{technicianName} has started investigating your request.", ct);

    public Task StatusChangedAsync(HelpDeskTicketDto ticket, string status, string? comment, CancellationToken ct = default)
    {
        var message = status switch
        {
            "Waiting for User" => "Royal Tyres IT needs information or action from you before work can continue.",
            "Waiting for Third Party" => "Your request is currently waiting for assistance from an external support provider.",
            "Escalated to Just ICT" => "Your request has been escalated to Just ICT for further assistance.",
            "Resolved" => "Your IT request has been marked as resolved.",
            "Closed" => "Your IT request has been closed.",
            _ => $"Your request status has changed to {status}."
        };
        if (!string.IsNullOrWhiteSpace(comment)) message += $"\n\nUpdate: {comment}";
        return NotifyRequesterAsync(ticket, status, message, ct);
    }

    private Task NotifyRequesterAsync(HelpDeskTicketDto ticket, string publicStatus, string update, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ticket.RequesterEmail)) return Task.CompletedTask;
        var body = $"""
            Hi {ticket.RequesterName},

            Ticket: {ticket.TicketNumber}
            Status: {publicStatus}
            Assigned Technician: {ticket.AssignedToName ?? "Royal Tyres IT"}

            {update}

            Royal Tyres IT
            """;
        return outbox.EnqueueAndTrySendAsync(ticket.Id, ticket.RequesterEmail, null,
            $"RoyalDesk {ticket.TicketNumber} - {publicStatus}", body, ct);
    }
}
