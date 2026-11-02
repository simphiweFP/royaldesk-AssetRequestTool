using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611020001)]
public sealed class AddStaffPortalAccess : Migration
{
    public override void Up()
    {
        if (!Schema.Table("StaffPortalAccess").Exists())
        {
            Create.Table("StaffPortalAccess")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DisplayName").AsString(150).NotNullable()
                .WithColumn("Username").AsString(100).Nullable()
                .WithColumn("PasswordHash").AsString(500).NotNullable()
                .WithColumn("PasswordSalt").AsString(200).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("SessionVersion").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("FailedAttempts").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("LockedUntilUtc").AsDateTime2().Nullable()
                .WithColumn("CreatedBy").AsString(150).NotNullable()
                .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
                .WithColumn("PasswordChangedAtUtc").AsDateTime2().NotNullable()
                .WithColumn("LastAccessAtUtc").AsDateTime2().Nullable();
        }

        if (!Schema.Table("StaffPageAccess").Exists())
        {
            Create.Table("StaffPageAccess")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StaffAccessId").AsInt32().NotNullable().ForeignKey("StaffPortalAccess", "Id")
                .WithColumn("PageKey").AsString(80).NotNullable();
            Create.Index("IX_StaffPageAccess_Profile_Page")
                .OnTable("StaffPageAccess")
                .OnColumn("StaffAccessId").Ascending()
                .OnColumn("PageKey").Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("StaffAccessSessions").Exists())
        {
            Create.Table("StaffAccessSessions")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StaffAccessId").AsInt32().NotNullable().ForeignKey("StaffPortalAccess", "Id")
                .WithColumn("TokenHash").AsString(100).NotNullable()
                .WithColumn("SessionVersion").AsInt32().NotNullable()
                .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
                .WithColumn("LastSeenAtUtc").AsDateTime2().NotNullable()
                .WithColumn("ExpiresAtUtc").AsDateTime2().NotNullable();
            Create.Index("IX_StaffAccessSessions_TokenHash")
                .OnTable("StaffAccessSessions").OnColumn("TokenHash").Ascending().WithOptions().Unique();
        }
    }

    public override void Down()
    {
        if (Schema.Table("StaffAccessSessions").Exists()) Delete.Table("StaffAccessSessions");
        if (Schema.Table("StaffPageAccess").Exists()) Delete.Table("StaffPageAccess");
        if (Schema.Table("StaffPortalAccess").Exists()) Delete.Table("StaffPortalAccess");
    }
}
