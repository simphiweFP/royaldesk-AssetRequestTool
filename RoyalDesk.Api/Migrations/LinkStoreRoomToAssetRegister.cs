using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

// Preserve all existing inventory; old assets must be explicitly reconciled with stock.
[Migration(202611050001)]
public sealed class LinkStoreRoomToAssetRegister : Migration
{
    public override void Up()
    {
        Create.Table("StoreAssetLinks")
            .WithColumn("AssetId").AsInt32().PrimaryKey().ForeignKey("Assets", "Id")
            .WithColumn("StoreItemId").AsInt32().NotNullable().ForeignKey("StoreItems", "Id")
            .WithColumn("PhysicallyInStore").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("LinkedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("LinkedBy").AsString(150).NotNullable();
        Create.Index("IX_StoreAssetLinks_StoreItemId").OnTable("StoreAssetLinks").OnColumn("StoreItemId").Ascending();
        Alter.Table("StoreMovements").AddColumn("AssetId").AsInt32().Nullable().ForeignKey("Assets", "Id");
        Create.Index("IX_StoreMovements_AssetId").OnTable("StoreMovements").OnColumn("AssetId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_StoreMovements_AssetId").OnTable("StoreMovements");
        Delete.Column("AssetId").FromTable("StoreMovements");
        Delete.Table("StoreAssetLinks");
    }
}
