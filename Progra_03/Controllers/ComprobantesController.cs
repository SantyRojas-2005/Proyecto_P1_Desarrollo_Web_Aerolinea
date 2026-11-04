using Microsoft.AspNet.Identity;
using Progra_03.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Progra_03.Controllers
{
    [Authorize] // OBLIGATORIO: Solo el titular puede ver sus documentos
    public class ComprobantesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Comprobantes
        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();

            // Obtenemos los comprobantes vinculados a las compras de las reservas del usuario logueado
            var comprobantes = db.Comprobante
                                 .Include(c => c.Compra)
                                 .Include(c => c.Compra.Reserva)
                                 .Where(c => c.Compra.Reserva.UsuarioId == userId)
                                 .OrderByDescending(c => c.FechaEmision)
                                 .ToList();

            return View(comprobantes);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}