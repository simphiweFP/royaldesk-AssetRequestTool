using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.Models;
using RoyalDesk.Api.Repositories;

namespace RoyalDesk.Api.Tests;

public sealed class AssetRequestRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"royaldesk-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task CreateAsync_InsertsRequestAndReturnsId()
    {
        var connectionString = $"Data Source={_databasePath}";
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(CreateAssetRequestsTable).Assembly).For.Migrations())
            .BuildServiceProvider();

        services.GetRequiredService<IMigrationRunner>().MigrateUp();

        var repository = new AssetRequestRepository(
            new SqliteConnectionFactory(connectionString));

        var id = await repository.CreateAsync(new AssetRequest
        {
            Branch = "Durban Commercial",
            Department = "Workshop",
            ItemType = "Barcode Scanner",
            Quantity = 1,
            Reason = "Replace a damaged scanner",
            RequestedBy = "simphiwe",
            RequestedAtUtc = DateTime.UtcNow
        });

        Assert.True(id > 0);
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
