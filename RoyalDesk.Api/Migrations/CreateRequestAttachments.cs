using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611040001)]
public sealed class CreateRequestAttachments : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS RequestAttachments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TicketId INTEGER NOT NULL,
                FileName TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                Content BLOB NOT NULL,
                SizeBytes INTEGER NOT NULL,
                UploadedAtUtc TEXT NOT NULL,
                FOREIGN KEY (TicketId) REFERENCES AssetRequests(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_RequestAttachments_TicketId ON RequestAttachments(TicketId);
            """);
    }

    public override void Down() => Delete.Table("RequestAttachments");
}
