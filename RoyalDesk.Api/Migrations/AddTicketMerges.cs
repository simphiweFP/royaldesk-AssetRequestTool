using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

// Additive schema: existing asset requests, delivery records and ticket history are retained.
[Migration(202611180901)]
public sealed class AddTicketMerges : Migration
{
    public override void Up()
    {
        Create.Table("TicketMerges")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("PrimaryTicketId").AsInt32().NotNullable()
            .WithColumn("MergedTicketId").AsInt32().NotNullable().Unique()
            .WithColumn("MergedBy").AsString(150).NotNullable()
            .WithColumn("Reason").AsString(1000).NotNullable()
            .WithColumn("MergedAtUtc").AsDateTime2().NotNullable();
        Create.Index("IX_TicketMerges_PrimaryTicketId").OnTable("TicketMerges")
            .OnColumn("PrimaryTicketId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("TicketMerges");
    }
}
