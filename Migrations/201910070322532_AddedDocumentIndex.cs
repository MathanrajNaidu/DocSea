namespace DocSea.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedDocumentIndex : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DocumentIndexes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(),
                        Password = c.String(),
                        DirectoryPath = c.String(),
                        Status = c.String(),
                        JobId = c.String(),
                        ForceStop = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.DocumentIndexes");
        }
    }
}
