using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Progra_03.Models;

namespace Progra_03
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            CrearRolesYUsuariosBase();
        }

        private void CrearRolesYUsuariosBase()
        {
            ApplicationDbContext context = new ApplicationDbContext();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // 1. Crear los roles obligatorios
            string[] roles = { "SuperUsuario", "Administrador", "Cliente" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                {
                    roleManager.Create(new IdentityRole(role));
                }
            }

            // 2. Crear cuenta SuperAdmin
            if (userManager.FindByEmail("superadmin@hotmail.com") == null)
            {
                var superAdmin = new ApplicationUser { UserName = "SuperAdmin", Email = "superadmin@hotmail.com" };
                var result = userManager.Create(superAdmin, "Superadmin123*");
                if (result.Succeeded) userManager.AddToRole(superAdmin.Id, "SuperUsuario");
            }

            // 3. Crear cuenta Administrador
            if (userManager.FindByEmail("administrador@hotmail.com") == null)
            {
                var admin = new ApplicationUser { UserName = "Administrador", Email = "administrador@hotmail.com" };
                var result = userManager.Create(admin, "Administrador123*");
                if (result.Succeeded) userManager.AddToRole(admin.Id, "Administrador");
            }

            // 4. Crear cuenta Cliente
            if (userManager.FindByEmail("cliente@hotmail.com") == null)
            {
                var cliente = new ApplicationUser { UserName = "Cliente", Email = "cliente@hotmail.com" };
                var result = userManager.Create(cliente, "Cliente123*");
                if (result.Succeeded) userManager.AddToRole(cliente.Id, "Cliente");
            }
        }
    }
}
