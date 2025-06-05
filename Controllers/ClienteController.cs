using Microsoft.AspNetCore.Mvc;
using Proyecto_DAW_Grupo_10.Models;
using static Proyecto_DAW_Grupo_10.Servicios.AutenticationAttribute;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Proyecto_DAW_Grupo_10.Controllers.TecnicoController;
using Proyecto_DAW_Grupo_10.Servicios;
using iTextSharp.text.pdf;
using iTextSharp.text;


namespace Proyecto_DAW_Grupo_10.Controllers
{
    [Autenticacion]
    public class ClienteController : Controller
    {
        private IConfiguration _configuration; // Se agrego para los correos
        private readonly TicketsDbContext _context;
        // Agregar al inicio del controlador
        private void CargarDatosParaTicket()
        {
            // Cargar categorías para el dropdown
            var categorias = _context.categoria.ToList();
            ViewData["listaCategorias"] = categorias;

            // Cargar prioridades (ya lo tienes)
            var prioridades = _context.prioridad.ToList();
            ViewData["listaPrioridades"] = new SelectList(prioridades, "prioridadId", "nombre");
        }

        public ClienteController(TicketsDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; //Se agrego para los correos
        }

        [HttpGet]
        public IActionResult ObtenerProblemasPorCategoria(int categoriaId)
        {
            var problemas = (from p in _context.problema
                             where p.categoriaId == categoriaId
                             select new
                             {
                                 p.problemaId,
                                 p.nombre
                             }).ToList();

            return Json(problemas);
        }

        public IActionResult Dashboard()
        {
            // Obtener ID del cliente de la sesión
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            // Obtener estadísticas de tickets
            var ticketsAbiertos = (from t in _context.ticket
                                   join e in _context.estado on t.estadoId equals e.estadoId
                                   where t.usuarioCreadorId == clienteId &&
                                         e.nombre != "Finalizado" &&
                                         e.nombre != "Cancelado"
                                   select t).Count();

            var ticketsEnEspera = (from t in _context.ticket
                                    join e in _context.estado on t.estadoId equals e.estadoId
                                    where t.usuarioCreadorId == clienteId &&
                                          e.nombre == "En espera del cliente"
                                    select t).Count();

            var ticketsFinalizados = (from t in _context.ticket
                                      join e in _context.estado on t.estadoId equals e.estadoId
                                      where t.usuarioCreadorId == clienteId &&
                                            e.nombre == "Finalizado"
                                      select t).Count();

            // Obtener tickets recientes (últimos 5 días)
            var fechaLimite = DateTime.Now.AddDays(-5);
            var ticketsRecientes = (from t in _context.ticket
                                    join p in _context.problema on t.problemaId equals p.problemaId
                                    join pr in _context.prioridad on t.prioridadId equals pr.prioridadId
                                    join e in _context.estado on t.estadoId equals e.estadoId
                                    where t.usuarioCreadorId == clienteId &&
                                          t.fechaApertura >= fechaLimite
                                    orderby t.fechaApertura descending
                                    select new
                                    {
                                        t.ticketId,
                                        Problema = p.nombre,
                                        Prioridad = pr.nombre,
                                        t.fechaApertura,
                                        Estado = e.nombre,
                                        t.descripcion
                                    }).Take(10).ToList();

            ViewBag.TicketsAbiertos = ticketsAbiertos;
            ViewBag.TicketsEnEspera = ticketsEnEspera;
            ViewBag.TicketsFinalizados = ticketsFinalizados;
            ViewBag.TicketsRecientes = ticketsRecientes;

            return View();
        }

        public IActionResult CrearTicket()
        {
            // Obtener problemas disponibles para seleccionar
            var problemas = (from p in _context.problema
                             join c in _context.categoria on p.categoriaId equals c.categoriaId
                             select new
                             {
                                 p.problemaId,
                                 NombreCompleto = $"{p.nombre} ({c.nombre})"
                             }).ToList();

            var prioridades = _context.prioridad.ToList();

            ViewData["listaProblemas"] = new SelectList(problemas, "problemaId", "NombreCompleto");
            ViewData["listaPrioridades"] = new SelectList(prioridades, "prioridadId", "nombre");

            CargarDatosParaTicket();
            return View();
        }

        [HttpPost]
        [Autenticacion]
        public async Task<IActionResult> CrearTicket([FromForm] ticket nuevoTicket)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Asignar datos automáticos
                    nuevoTicket.usuarioCreadorId = HttpContext.Session.GetInt32("usuarioId") ?? 0;
                    nuevoTicket.fechaApertura = DateTime.Now;
                    nuevoTicket.estadoId = 1; // Estado "Creado"

                    // Guardar el ticket
                    _context.ticket.Add(nuevoTicket);
                    await _context.SaveChangesAsync();

                    //Se agrego para los correos
                    //Variables para el correo
                    var problema = await _context.problema
                    .Where(p => p.problemaId == nuevoTicket.problemaId)
                    .Select(p => p.nombre)
                    .FirstOrDefaultAsync();

                    int usuarioId = HttpContext.Session.GetInt32("usuarioId") ?? 0;
                    var usuario = await _context.usuario.FindAsync(usuarioId);
                    string fecha = nuevoTicket.fechaApertura.ToString("dd/MM/yyyy");
                    string hora = nuevoTicket.fechaApertura.ToString("HH:mm");
                    string asunto = $"Hemos recibido tu solicitud #{nuevoTicket.ticketId} - {problema}";
                    string cuerpo = $"Hola {usuario.nombre},\n\n" +
                    $"¡Gracias por ponerte en contacto con nosotros! Hemos creado un ticket #{nuevoTicket.ticketId} para el problema \"{problema}\" registrado el {fecha} a las {hora}. Nuestro equipo lo revisará y te notificará los próximos pasos.\n\n" +
                    "Estamos aquí para ayudarte,\n\n" +
                    "Ayudín";

                    correo enviarCorreo = new correo(_configuration);
                    enviarCorreo.enviar(
                        destinatario: usuario.correo,
                        asunto: asunto,
                        cuerpo: cuerpo
                    );

                    return Json(new { success = true, ticketId = nuevoTicket.ticketId });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error al crear el ticket" });
                }
            }

            return Json(new { success = false, message = "Datos del ticket no válidos" });
        }

        [Autenticacion]
        public IActionResult MisTickets(int? estadoFiltro, int? prioridadFiltro, DateTime? fechaInicio, DateTime? fechaFin, DateTime? fechaEspecifica)
        {
            // Obtener ID del cliente de la sesión
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            // Obtener datos para los filtros
            var estados = _context.estado.OrderBy(e => e.nombre).ToList();
            var prioridades = _context.prioridad.OrderBy(p => p.nombre).ToList();

            // Consulta base de tickets
            var ticketsQuery = from t in _context.ticket
                               join p in _context.problema on t.problemaId equals p.problemaId
                               join pr in _context.prioridad on t.prioridadId equals pr.prioridadId
                               join e in _context.estado on t.estadoId equals e.estadoId
                               where t.usuarioCreadorId == clienteId
                               select new
                               {
                                   t.ticketId,
                                   Problema = p.nombre,
                                   Prioridad = pr.nombre,
                                   t.fechaApertura,
                                   Estado = e.nombre,
                                   t.descripcion,
                                   t.estadoId,
                                   t.prioridadId
                               };

            // Aplicar filtros si están seleccionados
            if (estadoFiltro.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.estadoId == estadoFiltro.Value);
            }

            if (prioridadFiltro.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.prioridadId == prioridadFiltro.Value);
            }

            // Filtro por fecha específica
            if (fechaEspecifica.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.fechaApertura.Date == fechaEspecifica.Value.Date);
            }
            // Filtro por rango de fechas (solo si no hay fecha específica)
            else if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                if (fechaInicio.HasValue)
                {
                    ticketsQuery = ticketsQuery.Where(t => t.fechaApertura >= fechaInicio.Value.Date);
                }
                if (fechaFin.HasValue)
                {
                    ticketsQuery = ticketsQuery.Where(t => t.fechaApertura <= fechaFin.Value.Date.AddDays(1).AddTicks(-1));
                }
            }

            // Ejecutar consulta y ordenar por fecha
            var tickets = ticketsQuery.OrderByDescending(t => t.fechaApertura).ToList();

            // Pasar datos a la vista
            ViewBag.Tickets = tickets;
            ViewBag.Estados = estados;
            ViewBag.Prioridades = prioridades;
            ViewBag.EstadoSeleccionado = estadoFiltro;
            ViewBag.PrioridadSeleccionada = prioridadFiltro;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.FechaEspecifica = fechaEspecifica?.ToString("yyyy-MM-dd");

            return View();
        }

        [Autenticacion]
        public IActionResult GenerarPDFMisTickets(int? estadoFiltro, int? prioridadFiltro, DateTime? fechaInicio, DateTime? fechaFin, DateTime? fechaEspecifica)
        {
            // Obtener ID del cliente de la sesión
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            // Consulta base de tickets
            var ticketsQuery = from t in _context.ticket
                               join p in _context.problema on t.problemaId equals p.problemaId
                               join pr in _context.prioridad on t.prioridadId equals pr.prioridadId
                               join e in _context.estado on t.estadoId equals e.estadoId
                               where t.usuarioCreadorId == clienteId
                               select new
                               {
                                   t.ticketId,
                                   Problema = p.nombre,
                                   Prioridad = pr.nombre,
                                   t.fechaApertura,
                                   Estado = e.nombre,
                                   t.descripcion,
                                   t.estadoId,
                                   t.prioridadId
                               };

            // Aplicar filtros si están seleccionados
            if (estadoFiltro.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.estadoId == estadoFiltro.Value);
            }

            if (prioridadFiltro.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.prioridadId == prioridadFiltro.Value);
            }

            // Filtro por fecha específica
            if (fechaEspecifica.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.fechaApertura.Date == fechaEspecifica.Value.Date);
            }
            // Filtro por rango de fechas (solo si no hay fecha específica)
            else if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                if (fechaInicio.HasValue)
                {
                    ticketsQuery = ticketsQuery.Where(t => t.fechaApertura >= fechaInicio.Value.Date);
                }
                if (fechaFin.HasValue)
                {
                    ticketsQuery = ticketsQuery.Where(t => t.fechaApertura <= fechaFin.Value.Date.AddDays(1).AddTicks(-1));
                }
            }

            // Ejecutar consulta y ordenar por fecha
            var tickets = ticketsQuery.OrderByDescending(t => t.fechaApertura).ToList();

            // Obtener información del cliente para el reporte
            var cliente = _context.usuario.FirstOrDefault(u => u.usuarioId == clienteId);
            var clienteNombre = cliente?.nombre ?? "Cliente";

            // Crear el documento PDF
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Fuentes
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

            // Título y subtítulo
            doc.Add(new Paragraph("Ayudín", titleFont));
            doc.Add(new Paragraph($"Reporte de Tickets - {clienteNombre}", subtitleFont));
            doc.Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy HH:mm"), smallFont));
            doc.Add(new Paragraph(" "));

            // Información de filtros aplicados
            var filtros = new Paragraph("Filtros aplicados: ", subtitleFont);

            if (estadoFiltro.HasValue)
            {
                var estadoNombre = _context.estado.FirstOrDefault(e => e.estadoId == estadoFiltro.Value)?.nombre;
                filtros.Add(new Chunk($" Estado: {estadoNombre}   ", bodyFont));
            }

            if (prioridadFiltro.HasValue)
            {
                var prioridadNombre = _context.prioridad.FirstOrDefault(p => p.prioridadId == prioridadFiltro.Value)?.nombre;
                filtros.Add(new Chunk($" Prioridad: {prioridadNombre}   ", bodyFont));
            }

            if (fechaEspecifica.HasValue)
            {
                filtros.Add(new Chunk($" Fecha: {fechaEspecifica.Value.ToString("dd/MM/yyyy")}   ", bodyFont));
            }
            else
            {
                if (fechaInicio.HasValue)
                {
                    filtros.Add(new Chunk($" Desde: {fechaInicio.Value.ToString("dd/MM/yyyy")}", bodyFont));
                }
                if (fechaFin.HasValue)
                {
                    filtros.Add(new Chunk($" Hasta: {fechaFin.Value.ToString("dd/MM/yyyy")}", bodyFont));
                }
            }

            if (!estadoFiltro.HasValue && !prioridadFiltro.HasValue && !fechaEspecifica.HasValue && !fechaInicio.HasValue && !fechaFin.HasValue)
            {
                filtros.Add(new Chunk(" Ninguno", bodyFont));
            }

            doc.Add(filtros);
            doc.Add(new Paragraph(" "));

            // Crear tabla para los tickets
            PdfPTable table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1f, 3f, 2f, 2f, 3f });

            // Encabezados de la tabla
            string[] headers = { "ID", "Problema", "Prioridad", "Estado", "Fecha Apertura" };
            foreach (var h in headers)
            {
                table.AddCell(new PdfPCell(new Phrase(h, headerFont)) { BackgroundColor = new BaseColor(240, 240, 240) });
            }

            // Agregar tickets a la tabla
            foreach (var t in tickets)
            {
                table.AddCell(new Phrase(t.ticketId.ToString(), bodyFont));
                table.AddCell(new Phrase(t.Problema, bodyFont));
                table.AddCell(new Phrase(t.Prioridad, bodyFont));
                table.AddCell(new Phrase(t.Estado, bodyFont));
                table.AddCell(new Phrase(t.fechaApertura.ToString("dd/MM/yyyy HH:mm"), bodyFont));
            }

            doc.Add(table);

            // Agregar resumen al final
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Total de tickets: {tickets.Count}", subtitleFont));

            // Si no hay tickets, mostrar mensaje
            if (tickets.Count == 0)
            {
                doc.Add(new Paragraph("No se encontraron tickets con los filtros seleccionados.", bodyFont));
            }

            doc.Close();

            // Nombre del archivo con filtros aplicados
            string fileName = $"MisTickets_{clienteNombre}_{DateTime.Now:yyyyMMddHHmm}";
            if (estadoFiltro.HasValue)
            {
                fileName += $"_Estado{estadoFiltro.Value}";
            }
            if (prioridadFiltro.HasValue)
            {
                fileName += $"_Prioridad{prioridadFiltro.Value}";
            }
            if (fechaEspecifica.HasValue)
            {
                fileName += $"_Fecha{fechaEspecifica.Value:yyyyMMdd}";
            }
            else
            {
                if (fechaInicio.HasValue)
                {
                    fileName += $"_Desde{fechaInicio.Value:yyyyMMdd}";
                }
                if (fechaFin.HasValue)
                {
                    fileName += $"_Hasta{fechaFin.Value:yyyyMMdd}";
                }
            }
            fileName += ".pdf";

            return File(ms.ToArray(), "application/pdf", fileName);
        }

        public class ActividadDTO
        {
            public int UsuarioId { get; set; } 
            public string Usuario { get; set; }
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } // "Comentario" o "Cambio de Estado"
            public string Detalle { get; set; }
        }

        [Autenticacion]
        public IActionResult DetalleTicket(int id)
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            // Obtener información del ticket
            var ticket = (from t in _context.ticket
                          join p in _context.problema on t.problemaId equals p.problemaId
                          join pr in _context.prioridad on t.prioridadId equals pr.prioridadId
                          join e in _context.estado on t.estadoId equals e.estadoId
                          join c in _context.categoria on p.categoriaId equals c.categoriaId
                          where t.ticketId == id && t.usuarioCreadorId == clienteId
                          select new
                          {
                              t.ticketId,
                              t.descripcion,
                              t.fechaApertura,
                              t.fechaCierre,
                              t.fechaModificacion,
                              Problema = p.nombre,
                              Categoria = c.nombre,
                              Prioridad = pr.nombre,
                              Estado = e.nombre
                          }).FirstOrDefault();

            if (ticket == null) return NotFound();

            ViewBag.Ticket = ticket;
            ViewBag.UsuarioId = clienteId;

            // Obtener archivos adjuntos
            var archivos = (from a in _context.archivosAdjuntos
                            where a.ticketId == id
                            select new
                            {
                                a.archivoId,
                                a.nombreArchivo,
                                a.fechaCarga
                            }).ToList();
            ViewBag.Archivos = archivos;

            // Obtener comentarios incluyendo el usuarioId
            var comentariosList = (from com in _context.comentario
                                   join u in _context.usuario on com.usuarioId equals u.usuarioId
                                   where com.ticketId == id
                                   select new ActividadDTO
                                   {
                                       UsuarioId = u.usuarioId,  // Incluir el ID del usuario
                                       Usuario = u.nombre,
                                       Fecha = com.fecha,
                                       Tipo = "Comentario",
                                       Detalle = com.texto
                                   }).ToList();

            // Obtener historial de estados incluyendo el usuarioId
            var historialEstados = (from he in _context.historialEstados
                                    join u in _context.usuario on he.usuarioId equals u.usuarioId
                                    join e in _context.estado on he.estadoNuevoId equals e.estadoId
                                    where he.ticketId == id && he.tareaId == null
                                    select new ActividadDTO
                                    {
                                        UsuarioId = u.usuarioId,  // Incluir el ID del usuario
                                        Usuario = u.nombre,
                                        Fecha = he.fechaNuevo,
                                        Tipo = "Cambio de Estado",
                                        Detalle = "Estado cambiado a: " + e.nombre
                                    }).ToList();

            // Combinar y ordenar actividades
            var actividadCompleta = comentariosList
                .Concat(historialEstados)
                .OrderBy(a => a.Fecha)
                .ToList();

            ViewBag.Actividad = actividadCompleta;

            return View();
        }

        [HttpPost]
        [Autenticacion]
        public IActionResult AgregarComentario(int ticketId, string texto)
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            var comentario = new comentario
            {
                texto = texto,
                fecha = DateTime.Now,
                ticketId = ticketId,
                usuarioId = clienteId.Value
            };

            _context.comentario.Add(comentario);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Autenticacion]
        public async Task<IActionResult> SubirArchivo(int ticketId, IFormFile archivo)
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            if (archivo != null && archivo.Length > 0)
            {
                // ✅ 1. Carpeta donde se guardarán los archivos
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "ArchivosAdjuntos");
                if (!Directory.Exists(rutaCarpeta))
                {
                    Directory.CreateDirectory(rutaCarpeta);
                }

                // ✅ 2. Generar nombre único para el archivo
                var nombreUnico = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);

                // ✅ 3. Ruta completa en disco
                var rutaCompleta = Path.Combine(rutaCarpeta, nombreUnico);

                // ✅ 4. Guardar el archivo físicamente
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // ✅ 5. Guardar la referencia en la base de datos
                var nuevoArchivo = new archivosAdjuntos
                {
                    ticketId = ticketId,
                    usuarioId = clienteId.Value,
                    nombreArchivo = archivo.FileName,         // Nombre original del archivo
                    tipoArchivo = archivo.ContentType,       // Tipo MIME
                    rutaArchivo = nombreUnico,               // Guardamos solo el nombre generado
                    fechaCarga = DateTime.Now
                };

                _context.archivosAdjuntos.Add(nuevoArchivo);
                await _context.SaveChangesAsync();

                return Ok("Archivo cargado y guardado exitosamente.");
            }

            return BadRequest("No se seleccionó ningún archivo.");
        }

        public IActionResult DescargarArchivo(int id)
        {
            // 1. Buscar el archivo en la base de datos
            var archivo = _context.archivosAdjuntos.FirstOrDefault(a => a.archivoId == id);
            if (archivo == null)
            {
                return NotFound();
            }

            // 2. Construir la ruta completa del archivo en disco
            var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "ArchivosAdjuntos");
            var rutaCompleta = Path.Combine(rutaCarpeta, archivo.rutaArchivo);

            // 3. Verificar si el archivo existe físicamente
            if (!System.IO.File.Exists(rutaCompleta))
            {
                return NotFound("El archivo no existe en el servidor.");
            }

            // 4. Leer los bytes del archivo físico
            var fileBytes = System.IO.File.ReadAllBytes(rutaCompleta);

            // 5. Retornar el archivo como descarga
            return File(fileBytes, archivo.tipoArchivo, archivo.nombreArchivo);
        }


        // Añade estas acciones al final de tu ClienteController

        [Autenticacion]
        public IActionResult MiPerfil()
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            var usuario = _context.usuario
                .Where(u => u.usuarioId == clienteId)
                .Select(u => new
                {
                    u.usuarioId,
                    u.nombre,
                    u.correo,
                    u.telefono,
                    u.empresa,
                    u.contactoPrincipal,
                    u.detalleContacto
                })
                .FirstOrDefault();

            if (usuario == null) return NotFound();

            ViewBag.Usuario = usuario;
            return View();
        }

        [HttpGet]
        [Autenticacion]
        public IActionResult EditarPerfil()
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            var usuario = _context.usuario.Find(clienteId);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        [HttpPost]
        [Autenticacion]
        public IActionResult EditarPerfil(usuario model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var clienteId = HttpContext.Session.GetInt32("usuarioId");

                    var usuario = _context.usuario.Find(clienteId);

                    // Actualizar solo los campos editables
                    usuario.nombre = model.nombre;
                    usuario.correo = model.correo;
                    usuario.telefono = model.telefono;
                    usuario.empresa = model.empresa;
                    usuario.contactoPrincipal = model.contactoPrincipal;
                    usuario.detalleContacto = model.detalleContacto;

                    _context.SaveChanges();

                    // Actualizar datos en sesión
                    HttpContext.Session.SetString("nombre", usuario.nombre);
                    HttpContext.Session.SetString("correo", usuario.correo);
                    HttpContext.Session.SetString("telefono", usuario.telefono);

                    return RedirectToAction("MiPerfil");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar el perfil: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Autenticacion]
        public IActionResult CambiarContrasenia()
        {
            return View();
        }

        [HttpPost]
        [Autenticacion]
        public IActionResult CambiarContrasenia(string contraseniaActual, string nuevaContrasenia, string confirmarContrasenia)
        {
            if (string.IsNullOrEmpty(nuevaContrasenia) || nuevaContrasenia != confirmarContrasenia)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden");
                return View();
            }

            var clienteId = HttpContext.Session.GetInt32("usuarioId");

            var usuario = _context.usuario.Find(clienteId);
            if (usuario == null) return NotFound();

            // Verificar contraseña actual (deberías usar hashing en producción)
            if (usuario.contrasenia != contraseniaActual)
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta");
                return View();
            }

            // Actualizar contraseña (en producción deberías hashear la nueva contraseña)
            usuario.contrasenia = nuevaContrasenia;
            _context.SaveChanges();

            return RedirectToAction("MiPerfil");
        }
    }
}
