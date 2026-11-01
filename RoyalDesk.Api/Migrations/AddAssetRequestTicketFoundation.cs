using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611010001)]
public sealed class AddAssetRequestTicketFoundation : Migration
{
    public override void Up()
    {
        // Some development databases already contain one or more of these
        // columns from earlier iterations. Add only what is missing so the
        // migration can safely recover a partially-upgraded SQLite database.
        if (!Schema.Table("AssetRequests").Column("TicketNumber").Exists())
        {
            Alter.Table("AssetRequests")
                .AddColumn("TicketNumber").AsString(30).Nullable();
        }

        if (!Schema.Table("AssetRequests").Column("RequesterName").Exists())
        {
            Alter.Table("AssetRequests")
                .AddColumn("RequesterName").AsString(150).Nullable();
        }

        if (!Schema.Table("AssetRequests").Column("RequesterEmail").Exists())
        {
            Alter.Table("AssetRequests")
                .AddColumn("RequesterEmail").AsString(200).Nullable();
        }

        if (!Schema.Table("AssetRequests").Column("Status").Exists())
        {
            Alter.Table("AssetRequests")
                .AddColumn("Status").AsString(40).NotNullable().WithDefaultValue("New");
        }

        // Keep existing requests usable in the new ticket workflow.
        Execute.Sql("""
            UPDATE AssetRequests
            SET TicketNumber = COALESCE(TicketNumber, printf('RT-IT-%06d', Id)),
                RequesterName = COALESCE(NULLIF(RequesterName, ''), RequestedBy),
                Status = COALESCE(NULLIF(Status, ''), 'New')
            WHERE TicketNumber IS NULL
               OR RequesterName IS NULL
               OR RequesterName = ''
               OR Status IS NULL
               OR Status = '';
            """);

        if (!Schema.Table("AssetRequests").Index("IX_AssetRequests_TicketNumber").Exists())
        {
            Create.Index("IX_AssetRequests_TicketNumber")
                .OnTable("AssetRequests")
                .OnColumn("TicketNumber").Ascending()
                .WithOptions().Unique();
        }
    }

    public override void Down()
    {
        if (Schema.Table("AssetRequests").Index("IX_AssetRequests_TicketNumber").Exists())
            Delete.Index("IX_AssetRequests_TicketNumber").OnTable("AssetRequests");

        if (Schema.Table("AssetRequests").Column("RequesterEmail").Exists())
            Delete.Column("RequesterEmail").FromTable("AssetRequests");

        if (Schema.Table("AssetRequests").Column("RequesterName").Exists())
            Delete.Column("RequesterName").FromTable("AssetRequests");

        if (Schema.Table("AssetRequests").Column("TicketNumber").Exists())
            Delete.Column("TicketNumber").FromTable("AssetRequests");

        // Status may pre-date this migration in older development databases,
        // so do not remove it during rollback.
    }
}
