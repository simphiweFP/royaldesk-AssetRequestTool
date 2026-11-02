using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611020001)]
public sealed class AddStoreRoom : Migration
{
    public override void Up()
    {
        if (!Schema.Table("StoreItems").Exists())
        {
            Create.Table("StoreItems")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(160).NotNullable()
                .WithColumn("Category").AsString(100).NotNullable()
                .WithColumn("Sku").AsString(80).Nullable()
                .WithColumn("Location").AsString(120).Nullable()
                .WithColumn("QuantityOnHand").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ReservedQuantity").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("MinimumStock").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("IsAssetTracked").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedAtUtc").AsDateTime2().Nullable();
        }

        if (!Schema.Table("StoreMovements").Exists())
        {
            Create.Table("StoreMovements")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StoreItemId").AsInt32().NotNullable()
                .WithColumn("TicketId").AsInt32().Nullable()
                .WithColumn("MovementType").AsString(40).NotNullable()
                .WithColumn("Quantity").AsInt32().NotNullable()
                .WithColumn("Reference").AsString(150).Nullable()
                .WithColumn("Notes").AsString(1000).Nullable()
                .WithColumn("PerformedBy").AsString(150).NotNullable()
                .WithColumn("PerformedAtUtc").AsDateTime2().NotNullable();
        }

        if (!Schema.Table("StoreMovements").Index("IX_StoreMovements_StoreItemId").Exists())
            Create.Index("IX_StoreMovements_StoreItemId").OnTable("StoreMovements").OnColumn("StoreItemId").Ascending();

        if (!Schema.Table("StoreMovements").Index("IX_StoreMovements_TicketId").Exists())
            Create.Index("IX_StoreMovements_TicketId").OnTable("StoreMovements").OnColumn("TicketId").Ascending();
    }

    public override void Down()
    {
        if (Schema.Table("StoreMovements").Exists()) Delete.Table("StoreMovements");
        if (Schema.Table("StoreItems").Exists()) Delete.Table("StoreItems");
    }
}
