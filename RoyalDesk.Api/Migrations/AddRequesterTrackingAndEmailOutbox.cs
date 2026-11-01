using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611010003)]
public sealed class AddRequesterTrackingAndEmailOutbox : Migration
{
    public override void Up()
    {
        Alter.Table("AssetRequests")
            .AddColumn("TrackingPinHash").AsString(128).Nullable();

        Create.Table("EmailOutbox")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("TicketId").AsInt32().Nullable()
            .WithColumn("Recipient").AsString(300).NotNullable()
            .WithColumn("Cc").AsString(500).Nullable()
            .WithColumn("Subject").AsString(300).NotNullable()
            .WithColumn("Body").AsString(int.MaxValue).NotNullable()
            .WithColumn("Status").AsString(30).NotNullable().WithDefaultValue("Pending")
            .WithColumn("Attempts").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LastError").AsString(2000).Nullable()
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("SentAtUtc").AsDateTime2().Nullable();

        Create.Index("IX_EmailOutbox_Status")
            .OnTable("EmailOutbox")
            .OnColumn("Status").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_EmailOutbox_Status").OnTable("EmailOutbox");
        Delete.Table("EmailOutbox");
        Delete.Column("TrackingPinHash").FromTable("AssetRequests");
    }
}
