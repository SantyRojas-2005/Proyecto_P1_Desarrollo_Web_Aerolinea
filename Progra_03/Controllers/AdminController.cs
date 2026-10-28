using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class AdminController : Controller
    {
        // Instancia de base de datos
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: /Admin/Index (Pantalla 17 - El menú)
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Vuelos (Pantalla 18 - Vuelos e Itinerarios)
        public ActionResult Vuelos(DateTime? desde, DateTime? hasta)
        {
            if (!desde.HasValue) desde = DateTime.Today;
            if (!hasta.HasValue) hasta = DateTime.Today.AddDays(7);

            var vuelos = db.vw_VueloDetalle
                           .Where(v => v.FechaOperacion >= desde.Value && v.FechaOperacion <= hasta.Value)
                           .OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida)
                           .ToList();

            ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");
            ViewBag.Aeronaves = db.Aeronave.Where(a => a.EstadoAeronaveCodigo == "DISPONIBLE").ToList();
            ViewBag.Estados = db.EstadoVuelo.ToList();

            // NUEVO: Consultamos las frecuencias e itinerarios para que funcionen las pestañas
            ViewBag.Frecuencias = db.Frecuencia.Where(f => f.Activa == true).ToList();
            ViewBag.Itinerarios = db.Itinerario.ToList();

            return View(vuelos);
        }

        // POST: /Admin/GenerarVuelos (Llama al SP de SQL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarVuelos(DateTime fechaDesde, DateTime fechaHasta)
        {
            try
            {
                // Llamamos al Procedimiento Almacenado nativo de SQL Server
                db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @FechaInicio = {0}, @FechaFin = {1}", fechaDesde, fechaHasta);
                TempData["MensajeExito"] = $"Vuelos generados correctamente entre el {fechaDesde:dd/MM/yyyy} y el {fechaHasta:dd/MM/yyyy}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorValidacion"] = "Error al generar programación: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction("Vuelos", new { desde = fechaDesde.ToString("yyyy-MM-dd"), hasta = fechaHasta.ToString("yyyy-MM-dd") });
        }

        // POST: /Admin/EditarVueloRapido (Para asignar aviones y cambiar estados)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarVueloRapido(int VueloId, int? AeronaveId, string EstadoVueloCodigo)
        {
            try
            {
                var vuelo = db.Vuelo.Find(VueloId);
                if (vuelo != null)
                {
                    vuelo.AeronaveId = AeronaveId;
                    vuelo.EstadoVueloCodigo = EstadoVueloCodigo;

                    // Al hacer SaveChanges, SQL Server validará que la aeronave no se cruce en horarios
                    db.SaveChanges();
                    TempData["MensajeExito"] = $"El vuelo VIP{VueloId.ToString("D5")} fue actualizado exitosamente.";
                }
            }
            catch (Exception ex)
            {
                // Si el trigger de SQL detecta un conflicto, lo capturamos aquí
                string error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                if (ex.InnerException?.InnerException != null) error = ex.InnerException.InnerException.Message;

                TempData["ErrorValidacion"] = "Conflicto operativo: " + error;
            }

            // Refrescamos la vista
            return RedirectToAction("Vuelos");
        }

        // GET: /Admin/Aeronaves (Pantalla 19)
        public ActionResult Aeronaves()
        {
            var aeronaves = db.Aeronave
                              .Include("ModeloAeronave")
                              .Include("EstadoAeronave")
                              .ToList();

            // Calculamos cuántos vuelos próximos tiene cada avión (desde hoy en adelante)
            var vuelosProximos = new Dictionary<int, int>();
            DateTime hoy = DateTime.Today;

            foreach (var a in aeronaves)
            {
                int count = db.Vuelo.Count(v => v.AeronaveId == a.AeronaveId
                                             && v.FechaOperacion >= hoy
                                             && v.EstadoVueloCodigo != "CANCELADO");
                vuelosProximos.Add(a.AeronaveId, count);
            }

            ViewBag.VuelosProximos = vuelosProximos;
            ViewBag.Estados = db.EstadoAeronave.ToList();

            return View(aeronaves);
        }

        // POST: /Admin/EditarAeronave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAeronave(int AeronaveId, string EstadoAeronaveCodigo)
        {
            try
            {
                var aeronave = db.Aeronave.Find(AeronaveId);
                if (aeronave != null)
                {
                    aeronave.EstadoAeronaveCodigo = EstadoAeronaveCodigo;
                    db.SaveChanges(); // El trigger de BD protegerá si hay reglas que lo impidan
                    TempData["MensajeExito"] = $"El estado operativo de la aeronave {aeronave.Matricula} fue actualizado a {EstadoAeronaveCodigo}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorValidacion"] = "Error al actualizar la aeronave: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction("Aeronaves");
        }

        // GET: /Admin/Gestion (Pantalla 20)
        public ActionResult Gestion(string tab = "reservas")
        {
            // Pasamos la pestaña activa a la vista (por defecto 'reservas')
            ViewBag.TabActiva = tab;

            // Consultamos todos los pasajeros
            ViewBag.Pasajeros = db.Pasajero.OrderByDescending(p => p.FechaRegistro).ToList();

            // Consultamos todas las reservas globales del sistema
            ViewBag.Reservas = db.Reserva.OrderByDescending(r => r.FechaCreacion).ToList();

            // Verificamos si es SuperUsuario para mostrar u ocultar opciones de roles
            ViewBag.EsSuperUsuario = User.IsInRole("SuperUsuario");

            return View();
        }

        // GET: /Admin/Reportes (Pantalla 21)
        public ActionResult Reportes(DateTime? desde, DateTime? hasta)
        {
            // Rango predeterminado: 7 días hacia atrás y 7 hacia adelante
            if (!desde.HasValue) desde = DateTime.Today.AddDays(-7);
            if (!hasta.HasValue) hasta = DateTime.Today.AddDays(7);

            ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

            // Consultamos la vista de disponibilidad de SQL Server
            var ocupacion = db.vw_DisponibilidadVuelo
                              .Where(v => v.FechaOperacion >= desde.Value && v.FechaOperacion <= hasta.Value)
                              .OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida)
                              .ToList();

            return View(ocupacion);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}