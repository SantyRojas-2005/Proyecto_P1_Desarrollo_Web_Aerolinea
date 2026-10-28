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

        // GET: Reservas/SeleccionAsiento/5
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

            // Buscamos el código físico del asiento (ej. "3D")
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

            // 1. VALIDACIONES DE TU CÓDIGO ORIGINAL (Adaptadas para iterar sobre todos los pasajeros)
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

            // Si hay error, devolvemos a la vista con los datos retenidos (usando el índice 0 para simplificar la vista actual)
            if (errorValidacion != null)
            {
                var vueloError = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == VueloId);
                ViewBag.Vuelo = vueloError;
                ViewBag.ErrorValidacion = errorValidacion;

                // Retenemos los datos del primer pasajero para no vaciarle el formulario
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

            // 2. PROCESO DE GUARDADO EN BASE DE DATOS (Múltiples pasajeros)
            try
            {
                var detalleVuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == VueloId);
                string userId = User.Identity.GetUserId();

                // Crear Cabecera de Reserva (PUNTO 2: FechaExpiracion en 30 minutos)
                var reserva = new Reserva
                {
                    UsuarioId = userId,
                    EstadoReservaCodigo = "PENDIENTE",
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddMinutes(30)
                };
                db.Reserva.Add(reserva);
                db.SaveChanges(); // Guardamos para obtener el ReservaId

                // PUNTO 3: Iterar sobre el arreglo para registrar N pasajeros y N asientos
                for (int i = 0; i < NumeroAsiento.Length; i++)
                {
                    string docActual = NumeroDocumento[i];

                    // Verificar si el pasajero ya existe en la BD
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

                    // Crear el asiento para este pasajero
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

                // Confirmar todos los asientos en la BD
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

                // Si la BD detecta un conflicto de asientos o pasajeros repetidos
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
        // GET: /Reservas/Resumen/5
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

        // GET: /Reservas/Pago/5
        public ActionResult Pago(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            // Calcular el total sumando el importe de los asientos de esta reserva
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

                // Evitar doble pago
                if (reserva.EstadoReservaCodigo != "PENDIENTE")
                    return Content("Esta reserva ya ha sido procesada o cancelada.");

                // Usamos una transacción para asegurar que todas las tablas se actualicen juntas
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Crear registro de Compra
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

                        // 2. Crear registro de Pago Simulado
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

                        // 3. Generar Comprobante (Factura Académica)
                        var comprobante = new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "FACTURA",
                            NumeroComprobante = "ACAD-" + compra.CompraId.ToString("D6"),
                            FechaEmision = DateTime.Now,
                            EstadoComprobanteCodigo = "EMITIDO"
                        };
                        db.Comprobante.Add(comprobante);

                        // 4. Actualizar Estado de la Reserva
                        reserva.EstadoReservaCodigo = "CONFIRMADA";

                        // 5. Actualizar Estado de los Asientos
                        var asignaciones = db.ReservaPasajeroVuelo.Where(r => r.ReservaId == ReservaId).ToList();
                        foreach (var asig in asignaciones)
                        {
                            asig.EstadoAsignacionCodigo = "CONFIRMADA";
                        }

                        // Confirmar todos los cambios en la BD
                        db.SaveChanges();
                        transaction.Commit();

                        // Redirigir a la pantalla de éxito final
                        return RedirectToAction("Boleto", new { id = reserva.ReservaId });
                    }
                    catch (Exception)
                    {
                        // Si algo falla, revertimos todo para no dejar datos huérfanos
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

        // GET: /Reservas/Boleto/5
        public ActionResult Boleto(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            // Obtenemos los datos para mostrar el boleto final
            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.Find(detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);

            // Obtenemos el comprobante generado
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
        // GET: /Reservas/MisReservas
        public ActionResult MisReservas()
        {
            ExpirarReservasVencidas();
            string userId = User.Identity.GetUserId();

            // 1. Obtenemos las reservas directamente del modelo original
            var reservas = db.Reserva
                             .Where(r => r.UsuarioId == userId)
                             .OrderByDescending(r => r.FechaCreacion)
                             .ToList();

            // 2. Creamos diccionarios para guardar los datos extra sin usar clases adicionales
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

            // 3. Pasamos los diccionarios a la vista a través del ViewBag
            ViewBag.InfoVuelos = infoVuelos;
            ViewBag.InfoPasajeros = infoPasajeros;
            ViewBag.InfoTotales = infoTotales;

            // Enviamos la lista original de EF
            return View(reservas);
        }

        // GET: /Reservas/DescargarPDF/5
        public ActionResult DescargarPDF(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            // Obtenemos los mismos datos del boleto
            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.FirstOrDefault(p => p.PasajeroId == detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);

            var compra = db.Compra.FirstOrDefault(c => c.ReservaId == id);
            ViewBag.Comprobante = db.Comprobante.FirstOrDefault(c => c.CompraId == compra.CompraId);

            // Convertimos la vista "Boleto" en PDF
            return new ViewAsPdf("Boleto", reserva)
            {
                FileName = "Boleto_Altevia_" + reserva.CodigoReserva + ".pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Portrait,
                // Este switch obliga al PDF a imprimir los fondos de color de Bootstrap
                CustomSwitches = "--print-media-type"
            };
        }

        // GET: /Reservas/Gestionar/5 (Pantalla 14)
        public ActionResult Gestionar(int id)
        {
            var reserva = db.Reserva.Find(id);
            if (reserva == null || reserva.UsuarioId != User.Identity.GetUserId())
                return HttpNotFound();

            var detalle = db.ReservaPasajeroVuelo.FirstOrDefault(d => d.ReservaId == id);
            ViewBag.Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == detalle.VueloId);
            ViewBag.Pasajero = db.Pasajero.Find(detalle.PasajeroId);
            ViewBag.Asiento = db.Asiento.FirstOrDefault(a => a.NumeroAsiento == detalle.NumeroAsiento);

            // Consultamos si ya existe una compra asociada
            ViewBag.Compra = db.Compra.FirstOrDefault(c => c.ReservaId == id);

            return View(reserva);
        }

        // POST: /Reservas/SolicitarCancelacion
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

                    // Validar la política DP-08: Cancelaciones hasta 24 horas antes del vuelo
                    if (vueloDetalle.SalidaProgramada <= DateTime.Now.AddHours(24))
                    {
                        TempData["Error"] = "Por políticas operativas, solo se permiten cancelaciones hasta 24 horas antes del despegue.";
                        return RedirectToAction("Gestionar", new { id = ReservaId });
                    }

                    // 1. Cambiar estado de la Reserva
                    reserva.EstadoReservaCodigo = "CANCELADA";

                    // 2. Liberar los asientos (CANCELADA los vuelve verdes en el mapa)
                    foreach (var asig in asignaciones)
                    {
                        asig.EstadoAsignacionCodigo = "CANCELADA";
                    }

                    // 3. Manejo de la Compra si estaba pagada (Generar Nota de Crédito - DP-08)
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
                        // Si estaba pendiente, anulamos la compra pendiente para que no pida pago futuro
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