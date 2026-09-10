using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202609100001)]
public sealed class CreateAssetRequestsTable : Migration
{
    public override void Up()
    {
        Create.Table("AssetRequests")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Branch").AsString(100).NotNullable()
            .WithColumn("Department").AsString(100).NotNullable()
            .WithColumn("ItemType").AsString(100).NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("Reason").AsString(500).NotNullable()
            .WithColumn("RequestedBy").AsString(100).NotNullable()
            .WithColumn("RequestedAtUtc").AsDateTime2().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("AssetRequests");
    }
}
