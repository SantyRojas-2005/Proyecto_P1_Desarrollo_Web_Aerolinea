using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // ===== ViewModels de la gestión de usuarios (MOD-01 / MOD-10) =====
    public class UsuarioListaViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool Bloqueado { get; set; }
        public bool PuedeGestionar { get; set; }
        public bool EsUsuarioActual { get; set; }
    }

    public class UsuarioCrearViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Solo letras, números, punto, guion y guion bajo (sin espacios).")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Seleccione un rol.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; }
    }

    public class UsuarioEditarViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Seleccione un rol.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        [Display(Name = "Cuenta activa")]
        public bool Activo { get; set; }
    }

    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class UsuariosController : Controller
    {
        private static readonly string[] TodosLosRoles = { "SuperUsuario", "Administrador", "Counter", "Cliente" };

        private ApplicationUserManager UserManager
        {
            get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        private bool EsSuperUsuario
        {
            get { return User.IsInRole("SuperUsuario"); }
        }

        // El SuperUsuario asigna cualquier rol; el Administrador solo Counter y Cliente.
        private string[] RolesPermitidos
        {
            get { return EsSuperUsuario ? TodosLosRoles : new[] { "Counter", "Cliente" }; }
        }

        private string RolDe(string userId)
        {
            return UserManager.GetRoles(userId).FirstOrDefault() ?? "Sin rol";
        }

        private bool PuedeGestionar(string rol)
        {
            return EsSuperUsuario || rol == "Counter" || rol == "Cliente" || rol == "Sin rol";
        }

        private static bool EstaBloqueado(ApplicationUser u)
        {
            return u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > DateTime.UtcNow;
        }

        // GET: Usuarios
        public ActionResult Index(string buscar)
        {
            string actualId = User.Identity.GetUserId();
            var usuarios = UserManager.Users.OrderBy(u => u.UserName).ToList();

            var modelo = new List<UsuarioListaViewModel>();
            foreach (var u in usuarios)
            {
                string rol = RolDe(u.Id);
                modelo.Add(new UsuarioListaViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Rol = rol,
                    Bloqueado = EstaBloqueado(u),
                    PuedeGestionar = PuedeGestionar(rol),
                    EsUsuarioActual = u.Id == actualId
                });
            }

            // Búsqueda por nombre de usuario, correo o rol
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string b = buscar.Trim().ToLower();
                modelo = modelo.Where(m => (m.UserName ?? "").ToLower().Contains(b)
                                        || (m.Email ?? "").ToLower().Contains(b)
                                        || m.Rol.ToLower().Contains(b)).ToList();
            }

            ViewBag.Buscar = buscar;
            return View(modelo);
        }

        // GET: Usuarios/Create
        public ActionResult Create()
        {
            ViewBag.Roles = new SelectList(RolesPermitidos, "Cliente");
            return View(new UsuarioCrearViewModel { Rol = "Cliente" });
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UsuarioCrearViewModel model)
        {
            if (!RolesPermitidos.Contains(model.Rol))
                ModelState.AddModelError("Rol", "No tiene permiso para asignar ese rol.");

            if (ModelState.IsValid && UserManager.FindByName(model.UserName.Trim()) != null)
                ModelState.AddModelError("UserName", "Ya existe un usuario con ese nombre.");

            if (ModelState.IsValid && UserManager.FindByEmail(model.Email.Trim()) != null)
                ModelState.AddModelError("Email", "Ya existe un usuario registrado con ese correo.");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.UserName.Trim(), Email = model.Email.Trim() };
                var result = UserManager.Create(user, model.Password);
                if (result.Succeeded)
                {
                    UserManager.AddToRole(user.Id, model.Rol);
                    TempData["Mensaje"] = "Usuario " + user.UserName + " creado con el rol " + model.Rol + ".";
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
            }

            ViewBag.Roles = new SelectList(RolesPermitidos, model.Rol);
            return View(model);
        }

        // GET: Usuarios/Edit/id
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            var user = UserManager.FindById(id);
            if (user == null) return HttpNotFound();

            string rol = RolDe(id);
            if (!PuedeGestionar(rol))
            {
                TempData["Error"] = "Solo el SuperUsuario puede modificar cuentas con rol " + rol + ".";
                return RedirectToAction("Index");
            }

            var model = new UsuarioEditarViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Rol = rol,
                Activo = !EstaBloqueado(user)
            };
            CargarDatosEdicion(model, id);
            return View(model);
        }

        // POST: Usuarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UsuarioEditarViewModel model)
        {
            var user = UserManager.FindById(model.Id);
            if (user == null) return HttpNotFound();

            string rolActual = RolDe(user.Id);
            if (!PuedeGestionar(rolActual))
            {
                TempData["Error"] = "Solo el SuperUsuario puede modificar cuentas con rol " + rolActual + ".";
                return RedirectToAction("Index");
            }

            bool esMismoUsuario = user.Id == User.Identity.GetUserId();
            if (esMismoUsuario && (model.Rol != rolActual || !model.Activo))
                ModelState.AddModelError("", "No puede cambiar su propio rol ni desactivar su propia cuenta.");

            if (model.Rol != rolActual && !RolesPermitidos.Contains(model.Rol))
                ModelState.AddModelError("Rol", "No tiene permiso para asignar ese rol.");

            if (ModelState.IsValid)
            {
                var otro = UserManager.FindByEmail(model.Email.Trim());
                if (otro != null && otro.Id != user.Id)
                    ModelState.AddModelError("Email", "Ese correo ya pertenece a otro usuario.");
            }

            if (ModelState.IsValid)
            {
                user.Email = model.Email.Trim();
                var result = UserManager.Update(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) ModelState.AddModelError("", error);
                }
                else
                {
                    if (model.Rol != rolActual)
                    {
                        var rolesActuales = UserManager.GetRoles(user.Id).ToArray();
                        if (rolesActuales.Length > 0) UserManager.RemoveFromRoles(user.Id, rolesActuales);
                        UserManager.AddToRole(user.Id, model.Rol);
                    }

                    if (model.Activo)
                    {
                        UserManager.SetLockoutEndDate(user.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
                        UserManager.ResetAccessFailedCount(user.Id);
                    }
                    else
                    {
                        UserManager.SetLockoutEnabled(user.Id, true);
                        UserManager.SetLockoutEndDate(user.Id, DateTimeOffset.UtcNow.AddYears(100));
                    }

                    TempData["Mensaje"] = "Usuario " + user.UserName + " actualizado.";
                    return RedirectToAction("Index");
                }
            }

            model.UserName = user.UserName;
            CargarDatosEdicion(model, user.Id);
            return View(model);
        }

        // POST: Usuarios/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            var user = UserManager.FindById(id);
            if (user == null) return HttpNotFound();

            string rol = RolDe(id);
            if (id == User.Identity.GetUserId())
            {
                TempData["Error"] = "No puede eliminar su propia cuenta.";
            }
            else if (!PuedeGestionar(rol))
            {
                TempData["Error"] = "Solo el SuperUsuario puede eliminar cuentas con rol " + rol + ".";
            }
            else
            {
                // Regla de trazabilidad: un usuario con reservas no se elimina, se desactiva.
                bool tieneReservas;
                using (var db = new AerolineaVIPContext())
                {
                    tieneReservas = db.Reserva.Any(r => r.UsuarioId == id);
                }

                if (tieneReservas)
                {
                    TempData["Error"] = "El usuario " + user.UserName + " tiene reservas registradas. Para conservar la trazabilidad, desactive la cuenta en lugar de eliminarla.";
                }
                else
                {
                    var result = UserManager.Delete(user);
                    if (result.Succeeded)
                        TempData["Mensaje"] = "Usuario " + user.UserName + " eliminado.";
                    else
                        TempData["Error"] = string.Join(" ", result.Errors);
                }
            }
            return RedirectToAction("Index");
        }

        private void CargarDatosEdicion(UsuarioEditarViewModel model, string userId)
        {
            // Si el rol actual no está en los permitidos (p. ej. "Sin rol"), se agrega para mostrarlo
            var opciones = RolesPermitidos.ToList();
            if (!opciones.Contains(model.Rol)) opciones.Insert(0, model.Rol);
            ViewBag.Roles = new SelectList(opciones, model.Rol);
            ViewBag.EsMismoUsuario = userId == User.Identity.GetUserId();
        }
    }
}
