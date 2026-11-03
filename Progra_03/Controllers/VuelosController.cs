using System;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models; // Asegúrate de usar el namespace de tus modelos

namespace Progra_03.Controllers
{
    public class VuelosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        [HttpGet]
        public ActionResult Buscar(string origen, string destino, DateTime? fecha, int pasajeros = 1)
        {
            if (string.IsNullOrEmpty(origen) || string.IsNullOrEmpty(destino) || !fecha.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Origen = origen;
            ViewBag.Destino = destino;
            ViewBag.Fecha = fecha.Value.ToString("dd/MM/yyyy");
            ViewBag.Pasajeros = pasajeros;
            ViewBag.EsEscala = false;

            var resultados = db.vw_DisponibilidadVuelo
                               .Where(v => v.Origen == origen
                                        && v.Destino == destino
                                        && v.FechaOperacion == fecha.Value)
                               .OrderBy(v => v.HoraSalida)
                               .ToList();

            if (origen == "UIO" && destino == "GPS")
            {
                var tramo1 = db.vw_DisponibilidadVuelo.FirstOrDefault(v => v.Origen == "UIO" && v.Destino == "GYE" && v.FechaOperacion == fecha.Value && v.HoraSalida == new TimeSpan(9, 0, 0));
                var tramo2 = db.vw_DisponibilidadVuelo.FirstOrDefault(v => v.Origen == "GYE" && v.Destino == "GPS" && v.FechaOperacion == fecha.Value);

                if (tramo1 != null && tramo2 != null)
                {
                    ViewBag.EsEscala = true;
                    ViewBag.Tramo1 = tramo1;
                    ViewBag.Tramo2 = tramo2;
                }
            }

            return View(resultados);
        }
        public ActionResult Disponibilidad(int id)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == id);
            if (vuelo == null)
            {
                return HttpNotFound("El vuelo seleccionado no está disponible.");
            }
            var asientos = db.vw_EstadoAsientoVuelo
                             .Where(a => a.VueloId == id)
                             .ToList();
            ViewBag.Vuelo = vuelo;
            ViewBag.Disponibles = asientos.Count(a => a.EstadoVisual == "DISPONIBLE");
            ViewBag.Ocupados = asientos.Count(a => a.EstadoVisual == "OCUPADO");
            ViewBag.Reservados = asientos.Count(a => a.EstadoVisual == "RESERVADO");

            return View(asientos);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}