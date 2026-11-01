using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611010001)]
public sealed class AddAssetRequestTicketFoundation : Migration
{
    public override void Up()
    {
        Alter.Table("AssetRequests")
            .AddColumn("TicketNumber").AsString(30).Nullable()
            .AddColumn("RequesterName").AsString(150).Nullable()
            .AddColumn("RequesterEmail").AsString(200).Nullable()
            .AddColumn("Status").AsString(40).NotNullable().WithDefaultValue("New");

        Create.Index("IX_AssetRequests_TicketNumber")
            .OnTable("AssetRequests")
            .OnColumn("TicketNumber").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Index("IX_AssetRequests_TicketNumber").OnTable("AssetRequests");

        Delete.Column("Status").FromTable("AssetRequests");
        Delete.Column("RequesterEmail").FromTable("AssetRequests");
        Delete.Column("RequesterName").FromTable("AssetRequests");
        Delete.Column("TicketNumber").FromTable("AssetRequests");
    }
}
