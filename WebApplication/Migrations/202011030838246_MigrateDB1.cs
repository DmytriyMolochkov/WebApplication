namespace WebApplication.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MigrateDB1 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.UserEmail", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.UserEmail", new[] { "User_Id" });
            RenameColumn(table: "dbo.UserEmail", name: "User_Id", newName: "UserID");
            AlterColumn("dbo.UserEmail", "UserID", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.UserEmail", "UserID");
            AddForeignKey("dbo.UserEmail", "UserID", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserEmail", "UserID", "dbo.AspNetUsers");
            DropIndex("dbo.UserEmail", new[] { "UserID" });
            AlterColumn("dbo.UserEmail", "UserID", c => c.String(maxLength: 128));
            RenameColumn(table: "dbo.UserEmail", name: "UserID", newName: "User_Id");
            CreateIndex("dbo.UserEmail", "User_Id");
            AddForeignKey("dbo.UserEmail", "User_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
