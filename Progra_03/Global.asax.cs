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

            // 1. Crear los 4 roles obligatorios
            string[] roles = { "SuperUsuario", "Administrador", "Counter", "Cliente" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                {
                    roleManager.Create(new IdentityRole(role));
                }
            }

            // 2. Crear cuentas base (Contraseñas seguras requeridas por Identity)
            if (userManager.FindByEmail("superadmin@altevia.com") == null)
            {
                var superAdmin = new ApplicationUser { UserName = "SuperAdmin", Email = "superadmin@altevia.com" };
                if (userManager.Create(superAdmin, "Superadmin123*").Succeeded)
                    userManager.AddToRole(superAdmin.Id, "SuperUsuario");
            }

            if (userManager.FindByEmail("administrador@altevia.com") == null)
            {
                var admin = new ApplicationUser { UserName = "Administrador", Email = "administrador@altevia.com" };
                if (userManager.Create(admin, "Administrador123*").Succeeded)
                    userManager.AddToRole(admin.Id, "Administrador");
            }

            if (userManager.FindByEmail("counter@altevia.com") == null)
            {
                var counter = new ApplicationUser { UserName = "Counter", Email = "counter@altevia.com" };
                if (userManager.Create(counter, "Counter123*").Succeeded)
                    userManager.AddToRole(counter.Id, "Counter");
            }

            if (userManager.FindByEmail("cliente@altevia.com") == null)
            {
                var cliente = new ApplicationUser { UserName = "Cliente", Email = "cliente@altevia.com" };
                if (userManager.Create(cliente, "Cliente123*").Succeeded)
                    userManager.AddToRole(cliente.Id, "Cliente");
            }
        }
    }
}
