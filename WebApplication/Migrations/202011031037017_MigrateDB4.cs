namespace WebApplication.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MigrateDB4 : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.UserAdress", newName: "UserAddress");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.UserAddress", newName: "UserAdress");
        }
    }
}
