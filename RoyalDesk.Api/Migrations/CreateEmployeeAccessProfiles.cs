using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202610260002)]
public sealed class CreateEmployeeAccessProfiles : Migration
{
    public override void Up()
    {
        Create.Table("EmployeeAccessProfiles")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EmployeeId").AsInt32().NotNullable().Unique().ForeignKey("Employees", "Id")
            .WithColumn("EmailAddress").AsString(200).Nullable()
            .WithColumn("SapUsername").AsString(100).Nullable()
            .WithColumn("CisTechUsername").AsString(100).Nullable()
            .WithColumn("VpnUsername").AsString(100).Nullable()
            .WithColumn("PhoneExtension").AsString(30).Nullable()
            .WithColumn("UserGroupsAdded").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("EmailGroupsAdded").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("PrintScanConfigured").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("SharedFoldersConfigured").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("PrintersConfigured").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("UpdatedBy").AsString(100).NotNullable()
            .WithColumn("UpdatedAtUtc").AsDateTime2().NotNullable();
    }

    public override void Down() => Delete.Table("EmployeeAccessProfiles");
}
