using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611050002)]
public sealed class AddEquipmentDeliveries : Migration
{
    public override void Up()
    {
        // SQLite native references avoid FluentMigrator ALTER FOREIGN KEY limitations.
        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS EquipmentDeliveries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TicketId INTEGER NOT NULL UNIQUE REFERENCES AssetRequests(Id),
                StoreItemId INTEGER REFERENCES StoreItems(Id),
                AssetId INTEGER REFERENCES Assets(Id),
                DestinationBranch TEXT NOT NULL,
                DeliveryRequired INTEGER NOT NULL,
                RecipientName TEXT NOT NULL,
                Quantity INTEGER NOT NULL CHECK(Quantity > 0),
                Status TEXT NOT NULL,
                DriverName TEXT,
                TrackingReference TEXT,
                ReceivedQuantity INTEGER NOT NULL DEFAULT 0 CHECK(ReceivedQuantity >= 0),
                ReceivedBy TEXT,
                ReceiptReference TEXT,
                ExceptionNotes TEXT,
                PreparedAtUtc TEXT,
                PickedUpAtUtc TEXT,
                ReceivedAtUtc TEXT,
                CompletedAtUtc TEXT,
                CreatedBy TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                CHECK(ReceivedQuantity <= Quantity)
            );
            CREATE INDEX IF NOT EXISTS IX_EquipmentDeliveries_Status ON EquipmentDeliveries(Status);
            CREATE INDEX IF NOT EXISTS IX_EquipmentDeliveries_Branch ON EquipmentDeliveries(DestinationBranch);
            CREATE TABLE IF NOT EXISTS EquipmentDeliveryEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeliveryId INTEGER NOT NULL REFERENCES EquipmentDeliveries(Id),
                FromStatus TEXT,
                ToStatus TEXT NOT NULL,
                Quantity INTEGER,
                Details TEXT,
                PerformedBy TEXT NOT NULL,
                PerformedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_EquipmentDeliveryEvents_DeliveryId ON EquipmentDeliveryEvents(DeliveryId);
            """);
    }

    public override void Down()
    {
        Execute.Sql("""
            DROP TABLE IF EXISTS EquipmentDeliveryEvents;
            DROP TABLE IF EXISTS EquipmentDeliveries;
            """);
    }
}
