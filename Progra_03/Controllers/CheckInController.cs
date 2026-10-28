using Progra_03.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Progra_03.Controllers
{
    public class CheckInController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Buscar(string CodigoReserva, string NumeroDocumento)
        {
            var asignacion = db.ReservaPasajeroVuelo
                               .FirstOrDefault(a => a.Reserva.CodigoReserva == CodigoReserva
                                                 && a.Pasajero.NumeroDocumento == NumeroDocumento);

            if (asignacion == null)
            {
                ViewBag.Error = "No se encontró un boleto con ese Código de Reserva y Documento.";
                return View("Index");
            }
            if (asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                ViewBag.Error = "Esta reserva no se encuentra confirmada o pagada.";
                return View("Index");
            }
            bool yaHizoCheckIn = db.CheckIn.Any(c => c.AsignacionId == asignacion.AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO");
            ViewBag.YaHizoCheckIn = yaHizoCheckIn;
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == asignacion.VueloId);
            ViewBag.Pasajero = db.Pasajero.FirstOrDefault(p => p.PasajeroId == asignacion.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(s => s.NumeroAsiento == asignacion.NumeroAsiento);
            ViewBag.Reserva = db.Reserva.FirstOrDefault(r => r.ReservaId == asignacion.ReservaId);

            return View("Resultado", asignacion);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(int AsignacionId)
        {
            try
            {
                bool existe = db.CheckIn.Any(c => c.AsignacionId == AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO");
                if (existe) return Content("El Check-In ya fue realizado anteriormente.");

                var checkin = new CheckIn
                {
                    AsignacionId = AsignacionId,
                    FechaRegistro = DateTime.Now,
                    EstadoCheckInCodigo = "COMPLETADO"
                };

                db.CheckIn.Add(checkin);
                db.SaveChanges();
                TempData["MensajeExito"] = "¡Check-in completado con éxito! Su pase de abordar está listo.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error de base de datos al realizar Check-in: " + ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
