using Dapper;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Deliveries;
using RoyalDesk.Api.HelpDesk;
using RoyalDesk.Api.Lifecycle;
using RoyalDesk.Api.Migrations;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Tests;

public sealed class EquipmentDeliveryIntegrationTests
{
    private static async Task<(string Path, SqliteConnectionFactory Factory, StoreRoomService Store,
        EquipmentDeliveryService Deliveries, int TicketId, int StoreItemId)> SetupAsync(int count, int quantity=3, bool assetTracked=false)
    {
        var file=Path.Combine(Path.GetTempPath(),$"royaldesk-delivery-{Guid.NewGuid():N}.db");
        var cs=$"Data Source={file}";
        using(var provider=new ServiceCollection().AddFluentMigratorCore()
            .ConfigureRunner(r=>r.AddSQLite().WithGlobalConnectionString(cs)
                .ScanIn(typeof(CreateAssetRequestsTable).Assembly).For.Migrations())
            .BuildServiceProvider())
            provider.GetRequiredService<IMigrationRunner>().MigrateUp();
        var factory=new SqliteConnectionFactory(cs);
        var store=new StoreRoomService(factory);
        var item=await store.CreateItemAsync(new CreateStoreItemDto{
            Name="Keyboard",Category="Peripheral",OpeningQuantity=count,MinimumStock=0,IsAssetTracked=assetTracked
        },"IT");
        await using(var db=new SqliteConnection(cs))
        {
            var now=DateTime.UtcNow;
            await db.ExecuteAsync("""
                INSERT INTO AssetRequests(TicketNumber,RequestType,RequesterName,Branch,Department,ItemType,
                    Quantity,Reason,Status,RequestedBy,RequestedAtUtc,LastUpdatedAtUtc)
                VALUES('RT-IT-900001','Equipment','Branch Employee','Alberton','Sales','Keyboard',
                    @quantity,'Required equipment','In Progress','IT',@now,@now)
                """,new{now,quantity});
        }
        return (file,factory,store,new EquipmentDeliveryService(factory),
            1,item.Id);
    }

    [Fact]
    public async Task RemoteDelivery_ReservesIssuesOnceAndBlocksClosureUntilReceipt()
    {
        var data=await SetupAsync(5);
        try
        {
            var d=await data.Deliveries.PrepareAsync(data.TicketId,
                new PrepareEquipmentDeliveryDto(data.StoreItemId,null,true,"Branch Employee",null),"IT");
            Assert.Equal("Waiting for Driver Pickup",d.Status);
            Assert.Equal(5,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal(3,Assert.Single(await data.Store.GetItemsAsync()).ReservedQuantity);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>data.Deliveries.PrepareAsync(data.TicketId,
                new PrepareEquipmentDeliveryDto(data.StoreItemId,null,true,"Duplicate",null),"IT"));
            d=await data.Deliveries.PickupAsync(data.TicketId,new PickupEquipmentDeliveryDto("Driver Joe","WB-123"),"IT");
            Assert.Equal("In Transit",d.Status);
            Assert.Equal(2,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal(0,Assert.Single(await data.Store.GetItemsAsync()).ReservedQuantity);
            var repository=new HelpDeskRepository(data.Factory);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>repository.UpdateStatusAsync(
                data.TicketId,new UpdateTicketStatusDto{Status="Resolved"},"IT","IT"));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>data.Deliveries.ConfirmAsync(
                data.TicketId,new ConfirmEquipmentReceiptDto(3,"Branch Employee","POD-123",null),"IT"));
            d=await data.Deliveries.ArrivedAsync(data.TicketId,"IT");
            d=await data.Deliveries.ConfirmAsync(data.TicketId,
                new ConfirmEquipmentReceiptDto(1,"Branch Employee","POD-123",null),"IT");
            Assert.Equal("Partially Received",d.Status);
            Assert.Equal(1,d.ReceivedQuantity);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>data.Deliveries.ConfirmAsync(
                data.TicketId,new ConfirmEquipmentReceiptDto(3,"Branch Employee","POD-123",null),"IT"));
            d=await data.Deliveries.ConfirmAsync(data.TicketId,
                new ConfirmEquipmentReceiptDto(2,"Branch Employee","POD-124",null),"IT");
            Assert.Equal("Done",d.Status);
            Assert.Equal(3,d.ReceivedQuantity);
            Assert.Equal(2,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal("Done",(await repository.GetTicketAsync(data.TicketId))!.Status);
            Assert.Contains(await data.Deliveries.HistoryAsync(data.TicketId),e=>e.ToStatus=="Waiting for Driver Pickup");
        }
        finally { if(File.Exists(data.Path))File.Delete(data.Path); }
    }

    [Fact]
    public async Task FailedShipment_ReturnsUndeliveredItemsToStoreWithoutCompletingTicket()
    {
        var data=await SetupAsync(4,quantity:2);
        try
        {
            await data.Deliveries.PrepareAsync(data.TicketId,
                new PrepareEquipmentDeliveryDto(data.StoreItemId,null,true,"Branch Employee",null),"IT");
            await data.Deliveries.PickupAsync(data.TicketId,new PickupEquipmentDeliveryDto("Driver Joe",null),"IT");
            var failed=await data.Deliveries.FailAsync(data.TicketId,new DeliveryExceptionDto("No access to branch"),"IT");
            Assert.Equal("Delivery Failed",failed.Status);
            Assert.Equal(2,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            var returned=await data.Deliveries.ReturnedAsync(data.TicketId,
                new DeliveryExceptionDto("Both keyboards physically returned"),"IT");
            Assert.Equal("Returned to Sender",returned.Status);
            Assert.Equal(4,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal("Returned to Sender",(await new HelpDeskRepository(data.Factory).GetTicketAsync(data.TicketId))!.Status);
            var retried=await data.Deliveries.RetryAsync(data.TicketId,"IT");
            Assert.Equal("Waiting for Driver Pickup",retried.Status);
            Assert.Equal(2,Assert.Single(await data.Store.GetItemsAsync()).ReservedQuantity);
            await data.Deliveries.PickupAsync(data.TicketId,new PickupEquipmentDeliveryDto("Driver 2","WB-2"),"IT");
            await data.Deliveries.ArrivedAsync(data.TicketId,"IT");
            var received=await data.Deliveries.ConfirmAsync(data.TicketId,
                new ConfirmEquipmentReceiptDto(2,"Alberton Receiver","POD-2",null),"IT");
            Assert.Equal("Done",received.Status);
            Assert.Equal(2,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            Assert.Equal(0,Assert.Single(await data.Store.GetItemsAsync()).ReservedQuantity);
            Assert.Contains(await data.Deliveries.HistoryAsync(data.TicketId),h=>h.ToStatus=="Returned to Sender");
        }
        finally { if(File.Exists(data.Path))File.Delete(data.Path); }
    }

    [Fact]
    public async Task DamagedReturn_IsQuarantinedAndCannotIncreaseAvailableStock()
    {
        var data=await SetupAsync(4,quantity:2);
        try
        {
            await data.Deliveries.PrepareAsync(data.TicketId,
                new PrepareEquipmentDeliveryDto(data.StoreItemId,null,true,"Branch Employee",null),"IT");
            await data.Deliveries.PickupAsync(data.TicketId,new PickupEquipmentDeliveryDto("Driver","WB-DAMAGE"),"IT");
            await data.Deliveries.FailAsync(data.TicketId,new DeliveryExceptionDto("Damaged in transit"),"IT");
            await data.Deliveries.ReturnedAsync(data.TicketId,new DeliveryExceptionDto("Both items damaged",true),"IT");
            var item=Assert.Single(await data.Store.GetItemsAsync());
            Assert.Equal(2,item.QuantityOnHand);
            Assert.Equal(2,item.QuarantinedQuantity);
            Assert.Equal(2,item.AvailableQuantity);
            Assert.Contains(await data.Store.GetMovementsAsync(),m=>m.MovementType=="Quarantine" && m.Quantity==2);
        }
        finally { if(File.Exists(data.Path))File.Delete(data.Path); }
    }

    [Fact]
    public async Task TaggedAsset_RemainsUnavailableDuringTransitAndMovesToDestinationAfterProof()
    {
        var data=await SetupAsync(1,quantity:1,assetTracked:true);
        try
        {
            await using(var db=new SqliteConnection($"Data Source={data.Path}"))
            {
                await db.ExecuteAsync("""
                    INSERT INTO Assets(AssetTag,Category,Status,Condition,CreatedAtUtc,UpdatedAtUtc)
                    VALUES('RT-K-01','Peripheral','Available','Good',@now,@now)
                    """,new{now=DateTime.UtcNow});
            }
            await data.Store.LinkAssetAsync(data.StoreItemId,new LinkStoreAssetDto(1,true),"IT");
            await data.Deliveries.PrepareAsync(data.TicketId,
                new PrepareEquipmentDeliveryDto(data.StoreItemId,1,true,"Branch Employee",null),"IT");
            await data.Deliveries.PickupAsync(data.TicketId,new PickupEquipmentDeliveryDto("Driver Joe","WB-555"),"IT");
            var inTransit=Assert.Single(await data.Store.GetAssetInventoryAsync());
            Assert.Equal("In Transit",inTransit.Status);
            Assert.False(inTransit.PhysicallyInStore);
            Assert.Equal(0,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            await data.Deliveries.ArrivedAsync(data.TicketId,"IT");
            var done=await data.Deliveries.ConfirmAsync(data.TicketId,
                new ConfirmEquipmentReceiptDto(1,"Branch Employee","Signed POD",null),"IT");
            Assert.Equal("Done",done.Status);
            Assert.Equal("Delivered",Assert.Single(await data.Store.GetAssetInventoryAsync()).Status);
            Assert.Equal(0,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
            await using(var db=new SqliteConnection($"Data Source={data.Path}"))
                await db.ExecuteAsync("""
                    INSERT INTO Employees(EmployeeNumber,FirstName,LastName,CreatedAtUtc,UpdatedAtUtc)
                    VALUES('DEL-001','Branch','Employee',@now,@now)
                    """,new{now=DateTime.UtcNow});
            var assignment=await new LifecycleRepository(data.Factory).AssignAssetAsync(
                new AssignAssetCommand(1,1,"Good"),"IT",CancellationToken.None);
            Assert.Equal(1,assignment.AssetId);
            Assert.Equal("Assigned",Assert.Single(await data.Store.GetAssetInventoryAsync()).Status);
            Assert.Equal(0,Assert.Single(await data.Store.GetItemsAsync()).QuantityOnHand);
        }
        finally { if(File.Exists(data.Path))File.Delete(data.Path); }
    }
}
