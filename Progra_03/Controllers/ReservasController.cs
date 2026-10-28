using Microsoft.AspNet.Identity;
using Progra_03.Models;
using System;
using Rotativa;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Progra_03.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        public ActionResult SeleccionAsiento(int vueloId)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == vueloId);
            if (vuelo == null)
            {
                return HttpNotFound("El vuelo no existe.");
            }

            var asientos = db.vw_EstadoAsientoVuelo
                             .Where(a => a.VueloId == vueloId)
                             .ToList();

            ViewBag.Vuelo = vuelo;
            return View(asientos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pasajeros(int vueloId, int numeroAsiento)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == vueloId);
            if (vuelo == null) return HttpNotFound("Vuelo no encontrado.");
            var asientoFisico = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == numeroAsiento);

            ViewBag.Vuelo = vuelo;
            ViewBag.NumeroAsiento = numeroAsiento;
            ViewBag.CodigoAsiento = asientoFisico != null ? asientoFisico.Codigo : numeroAsiento.ToString();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(int VueloId, int[] NumeroAsiento, string[] CodigoAsiento, string[] TipoDocumentoCodigo, string[] NumeroDocumento, string[] Nombres, string[] Apellidos, string[] FechaNacimiento, string[] Correo, string[] Telefono)
        {
            if (NumeroAsiento == null || NumeroAsiento.Length == 0)
            {
                return Content("Error: No se han recibido pasajeros ni asientos.");
            }

            string errorValidacion = null;
            for (int i = 0; i < NumeroAsiento.Length; i++)
            {
                if (!Regex.IsMatch(Nombres[i], @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$") || !Regex.IsMatch(Apellidos[i], @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
                    errorValidacion = $"Pasajero {i + 1}: Los nombres y apellidos solo pueden contener letras.";

                else if (TipoDocumentoCodigo[i] == "CEDULA")
                {
                    if (!ValidadorIdentificacion.ValidarCedulaEcuatoriana(NumeroDocumento[i]))
                        errorValidacion = $"Pasajero {i + 1}: La cédula ecuatoriana ingresada no es válida.";
                }
                else if (TipoDocumentoCodigo[i] == "PASAPORTE")
                {
                    if (!Regex.IsMatch(NumeroDocumento[i], @"^[a-zA-Z0-9]{6,15}$"))
                        errorValidacion = $"Pasajero {i + 1}: El pasaporte debe ser alfanumérico y tener entre 6 y 15 caracteres.";
                }

                if (errorValidacion == null && !string.IsNullOrWhiteSpace(Correo[i]))
                {
                    if (!Regex.IsMatch(Correo[i], @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        errorValidacion = $"Pasajero {i + 1}: El formato del correo electrónico no es válido.";
                }

                if (errorValidacion == null && !string.IsNullOrWhiteSpace(Telefono[i]))
                {
                    if (!Regex.IsMatch(Telefono[i], @"^[0-9]{10}$"))
                        errorValidacion = $"Pasajero {i + 1}: El teléfono debe contener exactamente 10 números.";
                }

                if (errorValidacion != null) break; // Detenemos la validación al primer error
            }
            if (errorValidacion != null)
            {
                var vueloError = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == VueloId);
                ViewBag.Vuelo = vueloError;
                ViewBag.ErrorValidacion = errorValidacion;
                ViewBag.NumeroAsiento = NumeroAsiento[0];
                ViewBag.CodigoAsiento = CodigoAsiento[0];
                ViewBag.NumeroDocumento = NumeroDocumento[0];
                ViewBag.Nombres = Nombres[0];
                ViewBag.Apellidos = Apellidos[0];
                ViewBag.Correo = Correo[0];
                ViewBag.Telefono = Telefono[0];
                ViewBag.FechaNacimiento = FechaNacimiento[0];

                return View("Pasajeros");
            }
            try
            {
                var detalleVuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == VueloId);
                string userId = User.Identity.GetUserId();
                var reserva = new Reserva
                {
                    UsuarioId = userId,
                    EstadoReservaCodigo = "PENDIENTE",
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddMinutes(30)
                };
                db.Reserva.Add(reserva);
                db.SaveChanges(); // Guardamos para obtener el ReservaId
                for (int i = 0; i < NumeroAsiento.Length; i++)
                {
                    string docActual = NumeroDocumento[i];
                    var pasajero = db.Pasajero.FirstOrDefault(p => p.NumeroDocumento == docActual);
                    if (pasajero == null)
                    {
                        pasajero = new Pasajero
                        {
                            TipoDocumentoCodigo = TipoDocumentoCodigo[i],
                            NumeroDocumento = NumeroDocumento[i],
                            Nombres = Nombres[i].ToUpper(),
                            Apellidos = Apellidos[i].ToUpper(),
                            FechaNacimiento = string.IsNullOrEmpty(FechaNacimiento[i]) ? (DateTime?)null : DateTime.Parse(FechaNacimiento[i]),
                            Correo = Correo[i],
                            Telefono = Telefono[i],
                            FechaRegistro = DateTime.Now
                        };
                        db.Pasajero.Add(pasajero);
                        db.SaveChanges(); // Guardamos para obtener el PasajeroId
                    }
                    var detalleReserva = new ReservaPasajeroVuelo
                    {
                        ReservaId = reserva.ReservaId,
                        PasajeroId = pasajero.PasajeroId,
                        VueloId = VueloId,
                        NumeroAsiento = (byte)NumeroAsiento[i],
                        ImporteTramo = detalleVuelo.TarifaReferencial,
                        EstadoAsignacionCodigo = "PENDIENTE",
                        FechaAsignacion = DateTime.Now
                    };
                    db.ReservaPasajeroVuelo.Add(detalleReserva);
                }
                db.SaveChanges();

                return RedirectToAction("Resumen", new { id = reserva.ReservaId });
            }
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                {
                    innerMessage += " -> " + ex.InnerException.InnerException.Message;
                }
                if (innerMessage.Contains("UX_RPV_PasajeroActivo") || innerMessage.Contains("UX_RPV_AsientoActivo"))
                {
                    var vueloError = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == VueloId);
                    ViewBag.Vuelo = vueloError;
                    ViewBag.ErrorValidacion = "Uno de los pasajeros o asientos ya está activo en este vuelo. Por favor, vuelva a intentarlo.";
                    ViewBag.NumeroAsiento = NumeroAsiento[0];
                    ViewBag.CodigoAsiento = CodigoAsiento[0];

                    return View("Pasajeros");
                }

                return Content("Error al guardar en BD: " + ex.Message + "<br/><br/><strong>DETALLE EXACTO DE SQL:</strong><br/> " + innerMessage);
            }
        }
        public ActionResult Resumen(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.Find(detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);

            return View(reserva);
        }
        public ActionResult Pago(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();
            decimal total = db.ReservaPasajeroVuelo
                              .Where(r => r.ReservaId == id)
                              .Sum(r => r.ImporteTramo);

            ViewBag.Total = total;
            ViewBag.Metodos = db.MetodoPago.Where(m => m.Activo == true).ToList();
            ViewBag.Marcas = db.MarcaTarjeta.Where(m => m.Activa == true).ToList();

            return View(reserva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarPago(int ReservaId, decimal Total, string MetodoPagoCodigo, string MarcaTarjetaCodigo, string NumeroTarjeta, string NombreTitular)
        {
            try
            {
                var reserva = db.Reserva.Find(ReservaId);
                if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                    return HttpNotFound();
                if (reserva.EstadoReservaCodigo != "PENDIENTE")
                    return Content("Esta reserva ya ha sido procesada o cancelada.");
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var compra = new Compra
                        {
                            ReservaId = ReservaId,
                            FechaCompra = DateTime.Now,
                            Importe = Total,
                            MonedaCodigo = "USD",
                            EstadoCompraCodigo = "PAGADA",
                            ClaveIdempotencia = Guid.NewGuid() // Clave única requerida por la BD
                        };
                        db.Compra.Add(compra);
                        db.SaveChanges(); // Guardamos para obtener el CompraId
                        string ultimos4 = NumeroTarjeta.Substring(NumeroTarjeta.Length - 4);
                        var pago = new Pago
                        {
                            CompraId = compra.CompraId,
                            FechaPago = DateTime.Now,
                            Importe = Total,
                            EstadoPagoCodigo = "APROBADO",
                            MetodoPagoCodigo = MetodoPagoCodigo,
                            MarcaTarjetaCodigo = MarcaTarjetaCodigo,
                            UltimosCuatroDigitos = ultimos4,
                            NombreTitular = NombreTitular.ToUpper(),
                            CodigoOperacion = "SIM-" + reserva.ReservaId + "-" + DateTime.Now.ToString("HHmmss"),
                            EsSimulado = true,
                            MensajeResultado = "Pago simulado aprobado (dato de demostración)."
                        };
                        db.Pago.Add(pago);
                        var comprobante = new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "FACTURA",
                            NumeroComprobante = "ACAD-" + compra.CompraId.ToString("D6"),
                            FechaEmision = DateTime.Now,
                            EstadoComprobanteCodigo = "EMITIDO"
                        };
                        db.Comprobante.Add(comprobante);
                        reserva.EstadoReservaCodigo = "CONFIRMADA";
                        var asignaciones = db.ReservaPasajeroVuelo.Where(r => r.ReservaId == ReservaId).ToList();
                        foreach (var asig in asignaciones)
                        {
                            asig.EstadoAsignacionCodigo = "CONFIRMADA";
                        }
                        db.SaveChanges();
                        transaction.Commit();
                        return RedirectToAction("Boleto", new { id = reserva.ReservaId });
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    innerMessage += " -> " + ex.InnerException.InnerException.Message;

                return Content("Error al procesar el pago: " + ex.Message + "<br/><br/><strong>SQL:</strong> " + innerMessage);
            }
        }
        public ActionResult Boleto(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();
            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.Find(detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);
            var compra = db.Compra.FirstOrDefault(c => c.ReservaId == id);
            ViewBag.Comprobante = db.Comprobante.FirstOrDefault(c => c.CompraId == compra.CompraId);

            return View(reserva);
        }



        private void ExpirarReservasVencidas()
        {
            var expiradas = db.Reserva.Where(r => r.EstadoReservaCodigo == "PENDIENTE" && r.FechaExpiracion < DateTime.Now).ToList();
            foreach (var r in expiradas)
            {
                r.EstadoReservaCodigo = "EXPIRADA";
                var asignaciones = db.ReservaPasajeroVuelo.Where(a => a.ReservaId == r.ReservaId).ToList();
                foreach (var a in asignaciones)
                {
                    a.EstadoAsignacionCodigo = "LIBERADA";
                }
            }
            db.SaveChanges();
        }
        public ActionResult MisReservas()
        {
            ExpirarReservasVencidas();
            string userId = User.Identity.GetUserId();
            var reservas = db.Reserva
                             .Where(r => r.UsuarioId == userId)
                             .OrderByDescending(r => r.FechaCreacion)
                             .ToList();
            var infoVuelos = new Dictionary<int, vw_VueloDetalle>();
            var infoPasajeros = new Dictionary<int, Pasajero>();
            var infoTotales = new Dictionary<int, decimal>();

            foreach (var r in reservas)
            {
                var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == r.ReservaId);
                if (detalle != null)
                {
                    infoVuelos.Add(r.ReservaId, db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId));
                    infoPasajeros.Add(r.ReservaId, db.Pasajero.FirstOrDefault(p => p.PasajeroId == detalle.PasajeroId));
                    infoTotales.Add(r.ReservaId, detalle.ImporteTramo);
                }
            }
            ViewBag.InfoVuelos = infoVuelos;
            ViewBag.InfoPasajeros = infoPasajeros;
            ViewBag.InfoTotales = infoTotales;
            return View(reservas);
        }
        public ActionResult DescargarPDF(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();
            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.FirstOrDefault(p => p.PasajeroId == detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);

            var compra = db.Compra.FirstOrDefault(c => c.ReservaId == id);
            ViewBag.Comprobante = db.Comprobante.FirstOrDefault(c => c.CompraId == compra.CompraId);
            return new ViewAsPdf("Boleto", reserva)
            {
                FileName = "Boleto_Altevia_" + reserva.CodigoReserva + ".pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Portrait,
                CustomSwitches = "--print-media-type"
            };
        }
        public ActionResult Gestionar(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.Find(detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);
            ViewBag.Compra = db.Compra.FirstOrDefault(c => c.ReservaId == id);

            return View(reserva);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SolicitarCancelacion(int ReservaId, string MotivoCancelacion)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var reserva = db.Reserva.Find(ReservaId);
                    if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                        return HttpNotFound();

                    if (reserva.EstadoReservaCodigo == "CANCELADA" || reserva.EstadoReservaCodigo == "EXPIRADA")
                    {
                        TempData["Error"] = "La reserva ya se encuentra cancelada o expirada.";
                        return RedirectToAction("Gestionar", new { id = ReservaId });
                    }

                    var asignaciones = db.ReservaPasajeroVuelo.Where(a => a.ReservaId == ReservaId).ToList();
                    var primerDetalle = asignaciones.FirstOrDefault();
                    var vueloDetalle = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == primerDetalle.VueloId);
                    if (vueloDetalle.SalidaProgramada <= DateTime.Now.AddHours(24))
                    {
                        TempData["Error"] = "Por políticas operativas, solo se permiten cancelaciones hasta 24 horas antes del despegue.";
                        return RedirectToAction("Gestionar", new { id = ReservaId });
                    }
                    reserva.EstadoReservaCodigo = "CANCELADA";
                    foreach (var asig in asignaciones)
                    {
                        asig.EstadoAsignacionCodigo = "CANCELADA";
                    }
                    var compra = db.Compra.FirstOrDefault(c => c.ReservaId == ReservaId && c.EstadoCompraCodigo == "PAGADA");
                    if (compra != null)
                    {
                        compra.EstadoCompraCodigo = "ANULADA";

                        var notaCredito = new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "NOTA_CREDITO",
                            NumeroComprobante = "NC-ACAD-" + compra.CompraId.ToString("D6"),
                            FechaEmision = DateTime.Now,
                            EstadoComprobanteCodigo = "EMITIDO"
                        };
                        db.Comprobante.Add(notaCredito);
                    }
                    else
                    {
                        var compraPendiente = db.Compra.FirstOrDefault(c => c.ReservaId == ReservaId && c.EstadoCompraCodigo == "PENDIENTE");
                        if (compraPendiente != null) compraPendiente.EstadoCompraCodigo = "ANULADA";
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["MensajeExito"] = "Su reserva fue cancelada exitosamente según la política vigente.";
                    return RedirectToAction("Gestionar", new { id = ReservaId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Ocurrió un error al procesar la cancelación: " + ex.Message;
                    return RedirectToAction("Gestionar", new { id = ReservaId });
                }
            }
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
