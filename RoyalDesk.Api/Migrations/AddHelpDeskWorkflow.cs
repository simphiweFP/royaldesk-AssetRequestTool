using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611010002)]
public sealed class AddHelpDeskWorkflow : Migration
{
    public override void Up()
    {
        Create.Table("ITTeamMembers")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("DisplayName").AsString(150).NotNullable()
            .WithColumn("Email").AsString(200).Nullable()
            .WithColumn("Username").AsString(100).NotNullable()
            .WithColumn("Role").AsString(100).NotNullable().WithDefaultValue("IT Technician")
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable();

        Create.Index("IX_ITTeamMembers_Username")
            .OnTable("ITTeamMembers")
            .OnColumn("Username").Ascending()
            .WithOptions().Unique();

        Insert.IntoTable("ITTeamMembers").Row(new
        {
            DisplayName = "demo.user",
            Email = (string?)null,
            Username = "demo.user",
            Role = "Administrator",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        Alter.Table("AssetRequests")
            .AddColumn("AssignedToId").AsInt32().Nullable()
            .AddColumn("AssignedToName").AsString(150).Nullable()
            .AddColumn("AssignedAtUtc").AsDateTime2().Nullable()
            .AddColumn("StartedAtUtc").AsDateTime2().Nullable()
            .AddColumn("ResolvedAtUtc").AsDateTime2().Nullable()
            .AddColumn("LastUpdatedAtUtc").AsDateTime2().Nullable();

        Create.Table("TicketStatusHistory")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("TicketId").AsInt32().NotNullable()
            .WithColumn("FromStatus").AsString(40).Nullable()
            .WithColumn("ToStatus").AsString(40).NotNullable()
            .WithColumn("ChangedBy").AsString(150).NotNullable()
            .WithColumn("ChangedByName").AsString(150).Nullable()
            .WithColumn("Comment").AsString(1000).Nullable()
            .WithColumn("PublicVisible").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("ChangedAtUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TicketStatusHistory_TicketId")
            .OnTable("TicketStatusHistory")
            .OnColumn("TicketId").Ascending();

        Execute.Sql("""
            INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
            SELECT Id, NULL, Status, RequestedBy, RequesterName,
                   'Ticket created', 1, RequestedAtUtc
            FROM AssetRequests
            WHERE NOT EXISTS (
                SELECT 1 FROM TicketStatusHistory h WHERE h.TicketId = AssetRequests.Id
            );
            """);
    }

    public override void Down()
    {
        Delete.Index("IX_TicketStatusHistory_TicketId").OnTable("TicketStatusHistory");
        Delete.Table("TicketStatusHistory");

        Delete.Column("LastUpdatedAtUtc").FromTable("AssetRequests");
        Delete.Column("ResolvedAtUtc").FromTable("AssetRequests");
        Delete.Column("StartedAtUtc").FromTable("AssetRequests");
        Delete.Column("AssignedAtUtc").FromTable("AssetRequests");
        Delete.Column("AssignedToName").FromTable("AssetRequests");
        Delete.Column("AssignedToId").FromTable("AssetRequests");

        Delete.Index("IX_ITTeamMembers_Username").OnTable("ITTeamMembers");
        Delete.Table("ITTeamMembers");
    }
}
