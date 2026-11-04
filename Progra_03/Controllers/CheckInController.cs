using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    [Authorize] // OBLIGATORIO: Solo usuarios autenticados pueden hacer check-in
    public class CheckInController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: CheckIn
        public ActionResult Index()
        {
            return View();
        }

        // POST: CheckIn/Buscar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Buscar(string CodigoReserva, string NumeroDocumento)
        {
            // Buscamos la relación exacta entre la reserva y el pasajero
            var asignacion = db.ReservaPasajeroVuelo
                               .FirstOrDefault(a => a.Reserva.CodigoReserva == CodigoReserva
                                                 && a.Pasajero.NumeroDocumento == NumeroDocumento);

            if (asignacion == null)
            {
                ViewBag.Error = "No se encontró un boleto con ese Código de Reserva y Documento.";
                return View("Index");
            }

            // Validar regla de negocio: Solo se permite check-in si está CONFIRMADA (Pagada)
            if (asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                ViewBag.Error = "Esta reserva no se encuentra confirmada o pagada.";
                return View("Index");
            }

            // Validar regla de negocio (DP-12): Ventana de Check-in (24h a 1h antes del vuelo)
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == asignacion.VueloId);
            if (vuelo != null && vuelo.SalidaProgramada.HasValue)
            {
                // Agregamos .Value a SalidaProgramada para extraer la fecha real y calcular las horas
                var horasFaltantes = (vuelo.SalidaProgramada.Value - DateTime.Now).TotalHours;

                if (horasFaltantes > 24 || horasFaltantes < 1)
                {
                    ViewBag.Error = "El check-in en línea solo está disponible entre 24 horas y 60 minutos antes de la salida programada de su vuelo.";
                    return View("Index");
                }
            }

            // Verificamos si el usuario ya hizo el check-in previamente para no duplicarlo
            bool yaHizoCheckIn = db.CheckIn.Any(c => c.AsignacionId == asignacion.AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO");
            ViewBag.YaHizoCheckIn = yaHizoCheckIn;

            // Cargamos datos para mostrar el pase de abordar
            ViewBag.Vuelo = vuelo;
            ViewBag.Pasajero = db.Pasajero.FirstOrDefault(p => p.PasajeroId == asignacion.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(s => s.NumeroAsiento == asignacion.NumeroAsiento);
            ViewBag.Reserva = db.Reserva.FirstOrDefault(r => r.ReservaId == asignacion.ReservaId);

            return View("Resultado", asignacion);
        }

        // POST: CheckIn/Confirmar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(int AsignacionId)
        {
            try
            {
                // Protegemos contra check-ins duplicados en el servidor
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

                // Redirigir a pantalla de éxito
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