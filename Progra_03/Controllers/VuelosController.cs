using System;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models; // Asegúrate de usar el namespace de tus modelos

namespace Progra_03.Controllers
{
    public class VuelosController : Controller
    {
        // Reemplaza "AerolineaVIPEntities" por el nombre exacto de tu DbContext generado por EF
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Vuelos/Buscar
        public ActionResult Buscar(string origen, string destino, DateTime? fecha, int pasajeros = 1)
        {
            // 1. Validar que vengan los parámetros obligatorios
            if (string.IsNullOrEmpty(origen) || string.IsNullOrEmpty(destino) || !fecha.HasValue)
            {
                // Si faltan datos, regresamos al inicio
                return RedirectToAction("Index", "Home");
            }

            // 2. Pasar los parámetros de búsqueda a la vista para el resumen
            ViewBag.Origen = origen;
            ViewBag.Destino = destino;
            ViewBag.Fecha = fecha.Value.ToString("dd/MM/yyyy");
            ViewBag.Pasajeros = pasajeros;

            // 3. Consultar la vista vw_VueloDetalle de la base de datos
            // Esta vista fue creada en el script SQL específicamente para esta pantalla
            var resultados = db.vw_VueloDetalle
                               .Where(v => v.Origen == origen
                                        && v.Destino == destino
                                        && v.FechaOperacion == fecha.Value)
                               .OrderBy(v => v.SalidaProgramada)
                               .ToList();

            // Retornamos la lista de vuelos a la vista
            return View(resultados);
        }
        // GET: Vuelos/Disponibilidad/5
        public ActionResult Disponibilidad(int id)
        {
            // 1. Obtener los detalles del vuelo
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == id);
            if (vuelo == null)
            {
                return HttpNotFound("El vuelo seleccionado no está disponible.");
            }

            // 2. Obtener el estado de los asientos para este vuelo específico
            var asientos = db.vw_EstadoAsientoVuelo
                             .Where(a => a.VueloId == id)
                             .ToList();

            // Pasamos el detalle del vuelo por ViewBag y la lista de asientos como Modelo
            ViewBag.Vuelo = vuelo;

            // Calculamos estadísticas para el resumen
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