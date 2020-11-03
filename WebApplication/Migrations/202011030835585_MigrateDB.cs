namespace WebApplication.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MigrateDB : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.UserEmail", "UserID", "dbo.AspNetUsers");
            DropIndex("dbo.UserEmail", new[] { "UserID" });
            RenameColumn(table: "dbo.UserEmail", name: "UserID", newName: "User_Id");
            AlterColumn("dbo.UserEmail", "User_Id", c => c.String(maxLength: 128));
            CreateIndex("dbo.UserEmail", "User_Id");
            AddForeignKey("dbo.UserEmail", "User_Id", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserEmail", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.UserEmail", new[] { "User_Id" });
            AlterColumn("dbo.UserEmail", "User_Id", c => c.String(nullable: false, maxLength: 128));
            RenameColumn(table: "dbo.UserEmail", name: "User_Id", newName: "UserID");
            CreateIndex("dbo.UserEmail", "UserID");
            AddForeignKey("dbo.UserEmail", "UserID", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
    }
}
