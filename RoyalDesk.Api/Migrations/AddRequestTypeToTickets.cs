using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611020002)]
public sealed class AddRequestTypeToTickets : Migration
{
    public override void Up()
    {
        if (!Schema.Table("AssetRequests").Column("RequestType").Exists())
        {
            Alter.Table("AssetRequests")
                .AddColumn("RequestType").AsString(40).NotNullable().WithDefaultValue("Equipment");
        }

        Execute.Sql("""
            UPDATE AssetRequests
            SET RequestType = 'Equipment'
            WHERE RequestType IS NULL OR trim(RequestType) = '';
            """);
    }

    public override void Down()
    {
        if (Schema.Table("AssetRequests").Column("RequestType").Exists())
            Delete.Column("RequestType").FromTable("AssetRequests");
    }
}
