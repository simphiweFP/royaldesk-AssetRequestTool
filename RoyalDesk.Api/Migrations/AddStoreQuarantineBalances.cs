using FluentMigrator;

namespace RoyalDesk.Api.Migrations;

[Migration(202611050003)]
public sealed class AddStoreQuarantineBalances : Migration
{
    public override void Up()
    {
        if (!Schema.Table("StoreItems").Column("QuarantinedQuantity").Exists())
            Alter.Table("StoreItems").AddColumn("QuarantinedQuantity").AsInt32()
                .NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        if (Schema.Table("StoreItems").Column("QuarantinedQuantity").Exists())
            Delete.Column("QuarantinedQuantity").FromTable("StoreItems");
    }
}
