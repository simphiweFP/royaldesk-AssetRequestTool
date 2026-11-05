using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

// Preserve existing inventory balances and require explicit asset reconciliation.
[Migration(202611050001)]
public sealed class LinkStoreRoomToAssetRegister : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS StoreAssetLinks (
                AssetId INTEGER NOT NULL PRIMARY KEY REFERENCES Assets(Id),
                StoreItemId INTEGER NOT NULL REFERENCES StoreItems(Id),
                PhysicallyInStore INTEGER NOT NULL DEFAULT 0,
                LinkedAtUtc TEXT NOT NULL,
                LinkedBy TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_StoreAssetLinks_StoreItemId ON StoreAssetLinks(StoreItemId);
            ALTER TABLE StoreMovements ADD COLUMN AssetId INTEGER NULL REFERENCES Assets(Id);
            CREATE INDEX IF NOT EXISTS IX_StoreMovements_AssetId ON StoreMovements(AssetId);
            """);
    }

    public override void Down()
    {
        Execute.Sql("""
            DROP INDEX IF EXISTS IX_StoreMovements_AssetId;
            ALTER TABLE StoreMovements DROP COLUMN AssetId;
            DROP TABLE IF EXISTS StoreAssetLinks;
            """);
    }
}
