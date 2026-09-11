using FluentMigrator.Runner;
using Microsoft.OpenApi;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Exceptions;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.Repositories;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Use the demo Basic Auth credentials to call protected endpoints from Swagger."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("basic", document)] = []
    });
});
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

builder.Services.AddSingleton<IDbConnectionFactory>(
    new SqliteConnectionFactory(connectionString));
builder.Services.AddScoped<IAssetRequestRepository, AssetRequestRepository>();
builder.Services.AddScoped<IAssetRequestService, AssetRequestService>();
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
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnablePersistAuthorization();
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("RoyalDeskClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
