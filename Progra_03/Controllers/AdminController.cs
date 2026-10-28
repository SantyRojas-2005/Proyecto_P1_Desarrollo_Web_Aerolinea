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
        private AerolineaVIPContext db = new AerolineaVIPContext();

        public ActionResult Index()
        {
            return View();
        }

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

            ViewBag.Frecuencias = db.Frecuencia.Where(f => f.Activa == true).ToList();
            ViewBag.Itinerarios = db.Itinerario.ToList();

            return View(vuelos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarVuelos(DateTime fechaDesde, DateTime fechaHasta)
        {
            try
            {
                db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @FechaInicio = {0}, @FechaFin = {1}", fechaDesde, fechaHasta);
                TempData["MensajeExito"] = $"Vuelos generados correctamente entre el {fechaDesde:dd/MM/yyyy} y el {fechaHasta:dd/MM/yyyy}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorValidacion"] = "Error al generar programación: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction("Vuelos", new { desde = fechaDesde.ToString("yyyy-MM-dd"), hasta = fechaHasta.ToString("yyyy-MM-dd") });
        }

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

                    db.SaveChanges();
                    TempData["MensajeExito"] = $"El vuelo VIP{VueloId.ToString("D5")} fue actualizado exitosamente.";
                }
            }
            catch (Exception ex)
            {
                string error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                if (ex.InnerException?.InnerException != null) error = ex.InnerException.InnerException.Message;

                TempData["ErrorValidacion"] = "Conflicto operativo: " + error;
            }

            return RedirectToAction("Vuelos");
        }

        public ActionResult Aeronaves()
        {
            var aeronaves = db.Aeronave
                              .Include("ModeloAeronave")
                              .Include("EstadoAeronave")
                              .ToList();

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
                    db.SaveChanges();
                    TempData["MensajeExito"] = $"El estado operativo de la aeronave {aeronave.Matricula} fue actualizado a {EstadoAeronaveCodigo}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorValidacion"] = "Error al actualizar la aeronave: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction("Aeronaves");
        }

        public ActionResult Gestion(string tab = "reservas")
        {
            ViewBag.TabActiva = tab;

            ViewBag.Pasajeros = db.Pasajero.OrderByDescending(p => p.FechaRegistro).ToList();

            ViewBag.Reservas = db.Reserva.OrderByDescending(r => r.FechaCreacion).ToList();

            ViewBag.EsSuperUsuario = User.IsInRole("SuperUsuario");

            return View();
        }

        public ActionResult Reportes(DateTime? desde, DateTime? hasta)
        {
            if (!desde.HasValue) desde = DateTime.Today.AddDays(-7);
            if (!hasta.HasValue) hasta = DateTime.Today.AddDays(7);

            ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

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
