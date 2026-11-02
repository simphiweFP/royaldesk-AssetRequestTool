using FluentMigrator.Runner;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Email;
using RoyalDesk.Api.Exceptions;
using RoyalDesk.Api.HelpDesk;
using RoyalDesk.Api.Lifecycle;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.Repositories;
using RoyalDesk.Api.Services;
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
            .AllowAnyMethod());
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
builder.Services.AddScoped<ILifecycleRepository, LifecycleRepository>();
builder.Services.AddScoped<ILifecycleService, LifecycleService>();
builder.Services.AddScoped<IHelpDeskRepository, HelpDeskRepository>();
builder.Services.AddScoped<HelpDeskService>();
builder.Services.AddScoped<JustIctEscalationRepository>();
builder.Services.AddScoped<JustIctEscalationService>();
builder.Services.AddScoped<PublicTrackingService>();
builder.Services.AddScoped<StoreRoomService>();
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
using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("RoyalDeskClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
