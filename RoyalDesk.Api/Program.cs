using FluentMigrator.Runner;
using Dapper;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.BusinessValue;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Deliveries;
using RoyalDesk.Api.Email;
using RoyalDesk.Api.Exceptions;
using RoyalDesk.Api.HelpDesk;
using RoyalDesk.Api.Lifecycle;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.Repositories;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.StaffAccess;
using RoyalDesk.Api.StoreRoom;
using RoyalDesk.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("RoyalDeskClient", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.SchemeName,
        options => builder.Configuration.GetSection("BasicAuthentication").Bind(options));
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.Configure<SmtpOptions>(options =>
{
    builder.Configuration.GetSection("Smtp").Bind(options);
    options.Host = builder.Configuration["SMTP_HOST"] ?? options.Host;
    if (int.TryParse(builder.Configuration["SMTP_PORT"], out var port)) options.Port = port;
    options.Username = builder.Configuration["SMTP_USERNAME"] ?? options.Username;
    options.Password = builder.Configuration["SMTP_PASSWORD"] ?? builder.Configuration["Smtp:Password"] ?? string.Empty;
    if (bool.TryParse(builder.Configuration["SMTP_USE_TLS"], out var useTls)) options.UseTls = useTls;
    options.FromAddress = builder.Configuration["SMTP_FROM"] ?? options.FromAddress;
});

builder.Services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
builder.Services.AddScoped<IAssetRequestRepository, AssetRequestRepository>();
builder.Services.AddScoped<IAssetRequestService, AssetRequestService>();
builder.Services.AddScoped<RequestAttachmentService>();
builder.Services.AddScoped<ILifecycleRepository, LifecycleRepository>();
builder.Services.AddScoped<ILifecycleService, LifecycleService>();
builder.Services.AddScoped<IHelpDeskRepository, HelpDeskRepository>();
builder.Services.AddScoped<HelpDeskService>();
builder.Services.AddScoped<TicketMergeService>();
builder.Services.AddScoped<JustIctEscalationRepository>();
builder.Services.AddScoped<JustIctEscalationService>();
builder.Services.AddScoped<PublicTrackingService>();
builder.Services.AddScoped<StoreRoomService>();
builder.Services.AddScoped<EquipmentDeliveryService>();
builder.Services.AddScoped<BusinessValueService>();
builder.Services.AddScoped<StaffAccessService>();
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.AddScoped<IEmailOutbox, EmailOutbox>();
builder.Services.AddScoped<TicketNotificationService>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddSingleton<IAssetRequestAuditLogger, FileAssetRequestAuditLogger>();
builder.Services.AddSingleton<CreateAssetRequestValidator>();

builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(runner => runner
        .AddSQLite()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(CreateAssetRequestsTable).Assembly).For.Migrations())
    .AddLogging(logging => logging.AddFluentMigratorConsole());

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    if (app.Environment.IsDevelopment())
    {
        // Diagnostics are emitted only to the local API logs, never the login response.
        // SQLite may resolve relative paths against a different working directory
        // depending on whether the server is launched by Visual Studio or a script.
        var diagnosticsFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        await using (var db = diagnosticsFactory.CreateConnection())
        {
            await db.OpenAsync();
            var databasePath = await db.ExecuteScalarAsync<string>("SELECT file FROM pragma_database_list WHERE name='main'");
            var adminCount = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM StaffPortalAccess WHERE lower(trim(Username))='royaldesk.admin' AND IsActive=1");
            app.Logger.LogInformation("Development SQLite database: {DatabasePath}", databasePath);
            app.Logger.LogInformation("Active royaldesk.admin found: {Found}", adminCount == 1);
            if (adminCount == 0)
                app.Logger.LogWarning(
                    "royaldesk.admin is not active in this database. If using seeded local credentials, " +
                    "start the API with scripts/seed-local-accounts.ps1 and avoid a second Visual Studio API instance.");
        }

        await DevelopmentStaffSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();

        if (string.Equals(app.Configuration["ROYALDESK_SEED_DEMO_USERS"], "true", StringComparison.OrdinalIgnoreCase))
        {
            // A successful seed is not sufficient: verify against the SAME connection
            // that the server will use for browser logins.
            var staffAccess = scope.ServiceProvider.GetRequiredService<StaffAccessService>();
            foreach (var (username, key) in new[]
            {
                ("royaldesk.admin", "ROYALDESK_DEMO_ADMIN_PASSWORD"),
                ("royaldesk.technician", "ROYALDESK_DEMO_TECH_PASSWORD")
            })
            {
                var password = app.Configuration[key];
                if (string.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException($"Missing {key} for development login verification.");
                try
                {
                    _ = await staffAccess.LoginAsync(username, password);
                    app.Logger.LogInformation("Verified local development login for {Username}.", username);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException(
                        $"Development login verification failed for {username}. " +
                        "Check the configured SQLite database and demo seeder ownership; " +
                        "the browser must connect to this same API instance.", ex);
                }
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("RoyalDeskClient");
app.UseMiddleware<StaffAccessMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
