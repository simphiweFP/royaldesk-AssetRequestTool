using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202610260001)]
public sealed class CreateAssetLifecycleTables : Migration
{
    public override void Up()
    {
        Create.Table("Branches")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Code").AsString(20).NotNullable().Unique()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        Create.Table("Departments")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable().Unique()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        Create.Table("Employees")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EmployeeNumber").AsString(50).NotNullable().Unique()
            .WithColumn("FirstName").AsString(100).NotNullable()
            .WithColumn("LastName").AsString(100).NotNullable()
            .WithColumn("Email").AsString(200).Nullable()
            .WithColumn("Username").AsString(100).Nullable()
            .WithColumn("JobTitle").AsString(150).Nullable()
            .WithColumn("BranchId").AsInt32().Nullable().ForeignKey("Branches", "Id")
            .WithColumn("DepartmentId").AsInt32().Nullable().ForeignKey("Departments", "Id")
            .WithColumn("EmploymentStatus").AsString(30).NotNullable().WithDefaultValue("Active")
            .WithColumn("StartDate").AsDate().Nullable()
            .WithColumn("EndDate").AsDate().Nullable()
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAtUtc").AsDateTime2().NotNullable();

        Create.Table("Assets")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("AssetTag").AsString(50).NotNullable().Unique()
            .WithColumn("SerialNumber").AsString(150).Nullable()
            .WithColumn("Category").AsString(100).NotNullable()
            .WithColumn("Make").AsString(100).Nullable()
            .WithColumn("Model").AsString(100).Nullable()
            .WithColumn("BranchId").AsInt32().Nullable().ForeignKey("Branches", "Id")
            .WithColumn("DepartmentId").AsInt32().Nullable().ForeignKey("Departments", "Id")
            .WithColumn("Status").AsString(40).NotNullable().WithDefaultValue("Available")
            .WithColumn("Condition").AsString(40).NotNullable().WithDefaultValue("Good")
            .WithColumn("PurchaseDate").AsDate().Nullable()
            .WithColumn("PurchaseValue").AsDecimal(18, 2).Nullable()
            .WithColumn("WarrantyExpiry").AsDate().Nullable()
            .WithColumn("FullyDepreciated").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAtUtc").AsDateTime2().NotNullable();

        Create.Table("AssetAssignments")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("AssetId").AsInt32().NotNullable().ForeignKey("Assets", "Id")
            .WithColumn("EmployeeId").AsInt32().NotNullable().ForeignKey("Employees", "Id")
            .WithColumn("AssignedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("AssignedBy").AsString(100).NotNullable()
            .WithColumn("ConditionAtAssignment").AsString(40).NotNullable()
            .WithColumn("ReturnedAtUtc").AsDateTime2().Nullable()
            .WithColumn("ReturnedTo").AsString(100).Nullable()
            .WithColumn("ReturnReason").AsString(100).Nullable()
            .WithColumn("ConditionAtReturn").AsString(40).Nullable()
            .WithColumn("Status").AsString(30).NotNullable().WithDefaultValue("Assigned")
            .WithColumn("EmployeeAcceptedAtUtc").AsDateTime2().Nullable()
            .WithColumn("EmployeeSignatureName").AsString(200).Nullable();

        Create.Table("AssetHistory")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("AssetId").AsInt32().NotNullable().ForeignKey("Assets", "Id")
            .WithColumn("EventType").AsString(50).NotNullable()
            .WithColumn("Description").AsString(1000).NotNullable()
            .WithColumn("PerformedBy").AsString(100).NotNullable()
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable();

        Create.Table("EmployeeLifecycleTasks")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EmployeeId").AsInt32().NotNullable().ForeignKey("Employees", "Id")
            .WithColumn("LifecycleType").AsString(30).NotNullable()
            .WithColumn("TaskCode").AsString(80).NotNullable()
            .WithColumn("TaskName").AsString(200).NotNullable()
            .WithColumn("IsCompleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CompletedBy").AsString(100).Nullable()
            .WithColumn("CompletedAtUtc").AsDateTime2().Nullable()
            .WithColumn("Notes").AsString(500).Nullable();

        Create.Table("WriteOffs")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("AssetId").AsInt32().NotNullable().ForeignKey("Assets", "Id")
            .WithColumn("Reason").AsString(300).NotNullable()
            .WithColumn("Condition").AsString(60).NotNullable()
            .WithColumn("CurrentValue").AsDecimal(18, 2).Nullable()
            .WithColumn("RequestedBy").AsString(100).NotNullable()
            .WithColumn("RequestedAtUtc").AsDateTime2().NotNullable()
            .WithColumn("ApprovalStatus").AsString(30).NotNullable().WithDefaultValue("Pending")
            .WithColumn("ApprovedBy").AsString(100).Nullable()
            .WithColumn("ApprovedAtUtc").AsDateTime2().Nullable()
            .WithColumn("DisposalMethod").AsString(100).Nullable()
            .WithColumn("DisposalCompany").AsString(200).Nullable()
            .WithColumn("DisposalDate").AsDate().Nullable()
            .WithColumn("CertificateReference").AsString(300).Nullable()
            .WithColumn("Notes").AsString(1000).Nullable();

        Alter.Table("AssetRequests")
            .AddColumn("Status").AsString(40).NotNullable().WithDefaultValue("Submitted")
            .AddColumn("EmployeeId").AsInt32().Nullable()
            .AddColumn("ManagerApprovalStatus").AsString(30).NotNullable().WithDefaultValue("Pending")
            .AddColumn("ITStatus").AsString(30).NotNullable().WithDefaultValue("Pending")
            .AddColumn("ApprovedAtUtc").AsDateTime2().Nullable()
            .AddColumn("CompletedAtUtc").AsDateTime2().Nullable();

        Insert.IntoTable("Branches").Row(new { Code = "PHX", Name = "Phoenix", IsActive = true });
        Insert.IntoTable("Branches").Row(new { Code = "DBN", Name = "Durban", IsActive = true });
        Insert.IntoTable("Branches").Row(new { Code = "JHB", Name = "Johannesburg", IsActive = true });
        Insert.IntoTable("Branches").Row(new { Code = "CPT", Name = "Cape Town", IsActive = true });

        foreach (var department in new[] { "IT", "Sales", "Finance", "Operations", "Human Resources", "Procurement" })
            Insert.IntoTable("Departments").Row(new { Name = department, IsActive = true });
    }

    public override void Down()
    {
        Delete.Column("CompletedAtUtc").FromTable("AssetRequests");
        Delete.Column("ApprovedAtUtc").FromTable("AssetRequests");
        Delete.Column("ITStatus").FromTable("AssetRequests");
        Delete.Column("ManagerApprovalStatus").FromTable("AssetRequests");
        Delete.Column("EmployeeId").FromTable("AssetRequests");
        Delete.Column("Status").FromTable("AssetRequests");
        Delete.Table("WriteOffs");
        Delete.Table("EmployeeLifecycleTasks");
        Delete.Table("AssetHistory");
        Delete.Table("AssetAssignments");
        Delete.Table("Assets");
        Delete.Table("Employees");
        Delete.Table("Departments");
        Delete.Table("Branches");
    }
}
