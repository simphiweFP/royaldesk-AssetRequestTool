using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611010004)]
public sealed class AddJustIctEscalations : Migration
{
    public override void Up()
    {
        Create.Table("TicketEscalations")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("TicketId").AsInt32().NotNullable()
            .WithColumn("Provider").AsString(100).NotNullable()
            .WithColumn("ProviderEmail").AsString(200).NotNullable()
            .WithColumn("TeamViewerId").AsString(100).NotNullable()
            .WithColumn("IssueDescription").AsString(2000).NotNullable()
            .WithColumn("Troubleshooting").AsString(3000).NotNullable()
            .WithColumn("AdditionalNotes").AsString(2000).Nullable()
            .WithColumn("EmailBody").AsString(8000).NotNullable()
            .WithColumn("EscalatedBy").AsString(150).NotNullable()
            .WithColumn("EscalatedByName").AsString(150).NotNullable()
            .WithColumn("EscalatedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("ResolvedAtUtc").AsDateTime2().Nullable();

        Create.Index("IX_TicketEscalations_TicketId")
            .OnTable("TicketEscalations")
            .OnColumn("TicketId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_TicketEscalations_TicketId").OnTable("TicketEscalations");
        Delete.Table("TicketEscalations");
    }
}
