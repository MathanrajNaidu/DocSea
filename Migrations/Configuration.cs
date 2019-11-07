namespace DocSea.Migrations
{
    using DocSea.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<DocSea.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(DocSea.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.

            var roleStore = new RoleStore<IdentityRole>(context);
            var roleManager = new RoleManager<IdentityRole>(roleStore);
            // RoleTypes is a class containing constant string values for different roles
            List<IdentityRole> identityRoles = new List<IdentityRole>();
            identityRoles.Add(new IdentityRole() { Name = RoleTypes.Admin });

            foreach (IdentityRole role in identityRoles)
            {
                if(!roleManager.RoleExists(role.Name))
                    roleManager.Create(role);
            }

            // Initialize default user
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);
            ApplicationUser admin = new ApplicationUser();
            admin.Email = "admin@admin.com";
            admin.UserName = "admin";

            if (userManager.FindByName(admin.UserName) == null)
            {
                userManager.Create(admin, "Petronas@1");
                userManager.AddToRole(admin.Id, RoleTypes.Admin);
            }
            // Add code to initialize context tables

            base.Seed(context);
        }
    }
}
