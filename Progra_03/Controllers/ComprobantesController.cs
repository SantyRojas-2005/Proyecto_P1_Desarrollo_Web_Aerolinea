using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    [Authorize] // Solo clientes autenticados ven sus comprobantes
    public class ComprobantesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: /Comprobantes/Index (Pantalla 16)
        public ActionResult Index(DateTime? desde, DateTime? hasta, string tipo, int? idPreview)
        {
            string userId = User.Identity.GetUserId();

            // 1. Consulta base a la vista de comprobantes filtrando por el cliente actual
            var query = db.vw_ComprobanteDetalle.Where(c => c.UsuarioId == userId);

            // 2. Aplicar filtros si el cliente los llena
            if (desde.HasValue) query = query.Where(c => c.FechaEmision >= desde.Value);
            if (hasta.HasValue) query = query.Where(c => c.FechaEmision <= hasta.Value);
            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos") query = query.Where(c => c.TipoComprobanteCodigo == tipo);

            var comprobantes = query.OrderByDescending(c => c.FechaEmision).ToList();

            // 3. Devolver datos al ViewBag para mantener los filtros llenos
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.Tipo = tipo;
            ViewBag.TiposComprobante = db.TipoComprobante.ToList();

            // 4. Lógica de "Vista Previa" si el usuario dio clic en "Visualizar"
            if (idPreview.HasValue)
            {
                var comprobante = db.vw_ComprobanteDetalle.FirstOrDefault(c => c.ComprobanteId == idPreview.Value && c.UsuarioId == userId);
                if (comprobante != null)
                {
                    ViewBag.Preview = comprobante;

                    // Extraemos todos los detalles (asientos/pasajeros) de la reserva asociada
                    var detalles = db.ReservaPasajeroVuelo.Where(r => r.ReservaId == comprobante.ReservaId).ToList();

                    var listaPasajeros = new List<string>();
                    var listaVuelos = new List<vw_VueloDetalle>();
                    var listaAsientos = new List<string>();

                    foreach (var d in detalles)
                    {
                        var p = db.Pasajero.Find(d.PasajeroId);
                        var v = db.vw_VueloDetalle.FirstOrDefault(x => x.VueloId == d.VueloId);
                        var a = db.Asiento.FirstOrDefault(x => x.NumeroAsiento == d.NumeroAsiento);

                        listaPasajeros.Add(p.Nombres + " " + p.Apellidos);
                        listaVuelos.Add(v);
                        listaAsientos.Add(a != null ? a.Codigo : d.NumeroAsiento.ToString());
                    }

                    ViewBag.PreviewPasajeros = listaPasajeros;
                    ViewBag.PreviewVuelos = listaVuelos;
                    ViewBag.PreviewAsientos = listaAsientos;
                    ViewBag.PreviewDetalles = detalles;
                }
            }

            return View(comprobantes);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}