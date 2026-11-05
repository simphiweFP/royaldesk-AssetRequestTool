using Dapper;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Lifecycle;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Tests;

public sealed class StoreRoomAssetIntegrationTests
{
    [Fact]
    public async Task LinkedAsset_AssignmentAndDamagedReturn_UpdatesOneInventoryLedger()
    {
        var file = Path.Combine(Path.GetTempPath(), $"royaldesk-store-{Guid.NewGuid():N}.db");
        try
        {
            var cs = $"Data Source={file}";
            using (var provider = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(r => r.AddSQLite().WithGlobalConnectionString(cs)
                    .ScanIn(typeof(CreateAssetRequestsTable).Assembly).For.Migrations())
                .BuildServiceProvider())
                provider.GetRequiredService<IMigrationRunner>().MigrateUp();

            var factory = new SqliteConnectionFactory(cs);
            var store = new StoreRoomService(factory);
            var item = await store.CreateItemAsync(new CreateStoreItemDto {
                Name = "Laptop", Category = "Hardware", OpeningQuantity = 1, MinimumStock = 0,
                IsAssetTracked = true
            }, "IT");

            await using var db = new SqliteConnection(cs);
            await db.OpenAsync();
            await db.ExecuteAsync("""
                INSERT INTO Assets(AssetTag,Category,Status,Condition,CreatedAtUtc,UpdatedAtUtc)
                VALUES('RT-L001','Laptop','Available','Good',@now,@now);
                INSERT INTO Employees(EmployeeNumber,FirstName,LastName,CreatedAtUtc,UpdatedAtUtc)
                VALUES('E001','Test','Employee',@now,@now);
                """, new {now=DateTime.UtcNow});
            var assetId=await db.QuerySingleAsync<int>("SELECT Id FROM Assets WHERE AssetTag='RT-L001'");
            var employeeId=await db.QuerySingleAsync<int>("SELECT Id FROM Employees WHERE EmployeeNumber='E001'");
            await store.LinkAssetAsync(item.Id,new LinkStoreAssetDto(assetId,true),"IT");

            var lifecycle=new LifecycleRepository(factory);
            var assignment=await lifecycle.AssignAssetAsync(new AssignAssetCommand(assetId,employeeId,"Good"),"IT",CancellationToken.None);
            var afterIssue=Assert.Single(await store.GetItemsAsync());
            Assert.Equal(0,afterIssue.QuantityOnHand);
            Assert.False(Assert.Single(await store.GetAssetInventoryAsync()).PhysicallyInStore);

            await lifecycle.ReturnAssetAsync((int)assignment.Id,
                new ReturnAssetCommand("Screen broken","Damaged","Needs repair"),"IT",CancellationToken.None);
            var afterReturn=Assert.Single(await store.GetItemsAsync());
            Assert.Equal(0,afterReturn.QuantityOnHand);
            Assert.Equal(1,afterReturn.QuarantinedQuantity);
            var damaged=Assert.Single(await store.GetAssetInventoryAsync());
            Assert.True(damaged.PhysicallyInStore);
            Assert.Equal("Damaged",damaged.Condition);
            Assert.Equal("Pending Write-Off",damaged.Status);
            Assert.Contains(await store.GetMovementsAsync(),m=>m.MovementType=="Quarantine" && m.StoreItemId==item.Id);

            await Assert.ThrowsAsync<InvalidOperationException>(()=>store.ApplyMovementAsync(item.Id,
                new StoreMovementRequest{MovementType="Issue",Quantity=1},"IT"));

            var writeOff=await lifecycle.CreateWriteOffAsync(
                new CreateWriteOffCommand(assetId,"Damaged","Damaged",null,"Repair not economical"),"IT",CancellationToken.None);
            await lifecycle.ApproveWriteOffAsync((int)writeOff.Id,
                new ApproveWriteOffCommand(true,"Approved"),"IT",CancellationToken.None);
            await lifecycle.DisposeWriteOffAsync((int)writeOff.Id,
                new DisposeWriteOffCommand("Recycling",null,DateTime.UtcNow,null,"Disposed"),"IT",CancellationToken.None);
            var disposed=Assert.Single(await store.GetAssetInventoryAsync());
            Assert.Equal("Disposed",disposed.Status);
            Assert.False(disposed.PhysicallyInStore);
            Assert.Equal(0,Assert.Single(await store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal(0,Assert.Single(await store.GetItemsAsync()).QuarantinedQuantity);
            Assert.Contains(await store.GetMovementsAsync(),m=>m.MovementType=="Write-Off" && m.StoreItemId==item.Id);
        }
        finally { if(File.Exists(file)) File.Delete(file); }
    }

    [Fact]
    public async Task StoreItems_CannotLinkTheSameAssetTwice()
    {
        var file=Path.Combine(Path.GetTempPath(),$"royaldesk-link-{Guid.NewGuid():N}.db");
        try
        {
            var cs=$"Data Source={file}";
            using(var provider=new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(r=>
                r.AddSQLite().WithGlobalConnectionString(cs).ScanIn(typeof(CreateAssetRequestsTable).Assembly).For.Migrations()).BuildServiceProvider())
                provider.GetRequiredService<IMigrationRunner>().MigrateUp();
            var store=new StoreRoomService(new SqliteConnectionFactory(cs));
            var item=await store.CreateItemAsync(new CreateStoreItemDto{
                Name="Laptop",Category="Hardware",OpeningQuantity=1,IsAssetTracked=true
            },"IT");
            await using var db=new SqliteConnection(cs);
            await db.ExecuteAsync("""
                INSERT INTO Assets(AssetTag,Category,Status,Condition,CreatedAtUtc,UpdatedAtUtc)
                VALUES('RT-L002','Laptop','Available','Good',@now,@now)
                """,new{now=DateTime.UtcNow});
            var id=await db.QuerySingleAsync<int>("SELECT Id FROM Assets WHERE AssetTag='RT-L002'");
            await store.LinkAssetAsync(item.Id,new LinkStoreAssetDto(id,true),"IT");
            await Assert.ThrowsAsync<InvalidOperationException>(()=>store.LinkAssetAsync(item.Id,new LinkStoreAssetDto(id,true),"IT"));
        }
        finally { if(File.Exists(file)) File.Delete(file); }
    }
}
