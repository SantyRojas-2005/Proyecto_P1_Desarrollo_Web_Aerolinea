using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class PasajerosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        public ActionResult Index()
        {
            var pasajero = db.Pasajero.Include(p => p.TipoDocumentoIdentidad);
            return View(pasajero.ToList());
        }
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }
        public ActionResult Create()
        {
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Pasajero.Add(pasajero);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pasajero).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Pasajero pasajero = db.Pasajero.Find(id);
            db.Pasajero.Remove(pasajero);
            db.SaveChanges();
            return RedirectToAction("Index");
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
