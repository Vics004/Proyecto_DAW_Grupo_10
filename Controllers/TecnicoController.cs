﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_DAW_Grupo_10.Models;
using Proyecto_DAW_Grupo_10.Servicios;
using iTextSharp.text.pdf;
using iTextSharp.text;
using static Proyecto_DAW_Grupo_10.Servicios.AutenticationAttribute;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class TecnicoController : Controller
    {

        private IConfiguration _configuration; // Se agrego para los correos
        private readonly TicketsDbContext _ticketsDbContext;
        public TecnicoController(TicketsDbContext ticketsDbContext, IConfiguration configuration)
        {
            _ticketsDbContext = ticketsDbContext;
            _configuration = configuration; //Se agrego para los correos
        }
        //Variables cambiantes por el moomento (previo a los insert)
        string creado = "Creado", Finalizado = "Finalizado", Espera = "En espera del cliente", asignado = "Asignado";
        public IActionResult Dashboard()
        {
            var usuario = HttpContext.Session.GetInt32("usuarioId");
            var rolId = HttpContext.Session.GetInt32("rolId");
            var nombreUsuario = HttpContext.Session.GetString("nombre");
            var rolNombre = (from r in _ticketsDbContext.rol
                             where r.rolId == rolId
                             select r.nombre).FirstOrDefault();
            var tareasPendientes = (from t in _ticketsDbContext.tarea
                                    join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                                    join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                    join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                                    join p in _ticketsDbContext.prioridad on ti.prioridadId equals p.prioridadId
                                    where e.nombre != creado && e.nombre != Finalizado && u.usuarioId == usuario && e.nombre != "Cancelado"
                                    select new
                                    {
                                        ID = t.tareaId,
                                        Des = t.descripcion,
                                        Prioridad = p.nombre,
                                        Estado = e.nombre

                                    }).ToList();

            var tareasRealizadas = (from t in _ticketsDbContext.tarea
                                    join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                                    join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                    join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                                    where (e.nombre == Finalizado || e.nombre == "Cancelado") && u.usuarioId == usuario
                                    select new
                                    {
                                        ID = t.tareaId,
                                        Des = t.descripcion,
                                        Estado = e.nombre

                                    }).ToList();
            //Viewdatas
            ViewData["usuarioId"] = usuario;
            ViewData["tareasPendientes"] = tareasPendientes;
            ViewData["tareasRealizadas"] = tareasRealizadas;
            return View();
        }
        //crear una clase temporal para facilitar to xd
        public class ActividadDTO
        {
            public int UsuarioId { get; set; }
            public string Usuario { get; set; }
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } // "Comentario" o "Cambio de Estado"
            public string Detalle { get; set; }
            public int? TareaId { get; set; }
        }
        [HttpPost]
        public IActionResult Detalles(int id)
        {
            var clienteId = HttpContext.Session.GetInt32("usuarioId");
            var tareasDetalle = (from ta in _ticketsDbContext.tarea
                                 join u in _ticketsDbContext.usuario on ta.usuarioAsignadoId equals u.usuarioId
                                 join e in _ticketsDbContext.estado on ta.estadoId equals e.estadoId
                                 join ti in _ticketsDbContext.ticket on ta.ticketId equals ti.ticketId
                                 join u2 in _ticketsDbContext.usuario on ti.usuarioCreadorId equals u2.usuarioId
                                 join p in _ticketsDbContext.prioridad on ti.prioridadId equals p.prioridadId
                                 join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                                 join c in _ticketsDbContext.categoria on pro.categoriaId equals c.categoriaId
                                 where ta.tareaId == id
                                 select new
                                 {
                                     Usuariocreador = u2.nombre,
                                     Usuario = u.nombre,
                                     FechaInicio = ti.fechaApertura,
                                     FechaCierre = ti.fechaCierre,
                                     Estado = e.nombre,
                                     Prioridad = p.nombre,
                                     Categoria = c.nombre,
                                     Problema = ta.descripcion,
                                     ProblemaTi = ti.descripcion,
                                 }).FirstOrDefault();
            //Controlador para cambiar de vista a la no editable y reutilizar vista
            if (tareasDetalle.Estado == "Cancelado" || tareasDetalle.Estado == "Finalizado")
            {
                ViewBag.EstadoTerminado = true;
            }
            else
            {
                ViewBag.EstadoTerminado = false;
            }
            var estadoActual = (from ta in _ticketsDbContext.tarea
                                join e in _ticketsDbContext.estado on ta.estadoId equals e.estadoId
                                where ta.tareaId == id
                                select e.nombre).FirstOrDefault();
            var estadosPermitidos = new[] { "En proceso", "En espera del cliente" };

            var estados = (from e in _ticketsDbContext.estado
                           where e.nombre == "En proceso"
                           select new SelectListItem
                           {
                               Value = e.estadoId.ToString(),
                               Text = e.nombre
                           }).ToList();

            //Si esta en proceso puede añadir el estado "En espera del cliente"
            if (estadoActual == "En proceso")
            {
                var estadoEnProceso = _ticketsDbContext.estado
                    .Where(e => e.nombre == "En espera del cliente")
                    .Select(e => new SelectListItem
                    {
                        Value = e.estadoId.ToString(),
                        Text = e.nombre
                    })
                    .FirstOrDefault();
                if (estadoEnProceso != null)
                {
                    estados.Add(estadoEnProceso);
                }

            }
            // Agregar "Finalizado" solo si el estado actual está permitido
            if (estadoActual == "En proceso" || estadoActual == "En espera del cliente")
            {
                var estadoFinalizado = _ticketsDbContext.estado
                    .Where(e => e.nombre == "Finalizado")
                    .Select(e => new SelectListItem
                    {
                        Value = e.estadoId.ToString(),
                        Text = e.nombre
                    })
                    .FirstOrDefault();

                if (estadoFinalizado != null)
                {
                    estados.Add(estadoFinalizado);
                }
            }


            ViewData["Estados"] = estados;
            ViewBag.ID = id;



            ViewBag.tareasDetalles = tareasDetalle;
            //Obtener los nombres de archivos adjuntos y su fecha de subida
            var idticket = (from ta in _ticketsDbContext.tarea
                            where ta.tareaId == id
                            select ta.ticketId).FirstOrDefault();
            ViewBag.Ticket = idticket;
            ViewBag.UsuarioId = clienteId;

            var archivosAdjuntos = (from t in _ticketsDbContext.ticket
                                    join a in _ticketsDbContext.archivosAdjuntos on t.ticketId equals a.ticketId
                                    where t.ticketId == idticket
                                    select new
                                    {
                                        a.archivoId,
                                        a.nombreArchivo,
                                        a.fechaCarga
                                    }).ToList();

            //obtener comentarios y cambios de estado
            var comentariosList = (from com in _ticketsDbContext.comentario
                                   join u in _ticketsDbContext.usuario on com.usuarioId equals u.usuarioId
                                   where com.ticketId == idticket
                                   select new ActividadDTO
                                   {
                                       UsuarioId = u.usuarioId,
                                       Usuario = u.nombre,
                                       Fecha = com.fecha,
                                       Tipo = "Comentario",
                                       Detalle = com.texto,
                                       TareaId = com.tareaId
                                   }).ToList();

            var historialEstados = (from he in _ticketsDbContext.historialEstados
                                    join u in _ticketsDbContext.usuario on he.usuarioId equals u.usuarioId
                                    join e in _ticketsDbContext.estado on he.estadoNuevoId equals e.estadoId
                                    where he.ticketId == idticket
                                    select new ActividadDTO
                                    {
                                        UsuarioId = u.usuarioId,
                                        Usuario = u.nombre,
                                        Fecha = he.fechaNuevo,
                                        Tipo = "Cambio de Estado",
                                        Detalle = "Estado cambiado a: " + e.nombre,
                                        TareaId = he.tareaId
                                    }).ToList();
            var actividadCompleta = comentariosList
            .Concat(historialEstados)
            .OrderBy(a => a.Fecha)
            .ToList();
            ViewBag.ticketID = idticket;
            ViewBag.Actividad = actividadCompleta;
            ViewBag.Archivos = archivosAdjuntos;


            return View();
        }


        [HttpPost]
        public IActionResult CambiarEstado(int id, int estadoId)
        {
            var tarea = _ticketsDbContext.tarea.Find(id);
            var estadoNombre = (from e in _ticketsDbContext.estado
                                where e.estadoId == estadoId
                                select e.nombre).FirstOrDefault();

            var estadoTicket = (from t in _ticketsDbContext.ticket
                                join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                where t.ticketId == tarea.ticketId
                                select e.nombre).FirstOrDefault();
            var ticket = _ticketsDbContext.ticket.Find(tarea.ticketId);
            

            if (estadoTicket == asignado)
            {
                //Aqui busco y coloco el id del estado en proceso
                var idEstado = (from e in _ticketsDbContext.estado
                                where e.nombre == "En proceso"
                                select e.estadoId).FirstOrDefault();
                //Aqui actualizo el estado del Ticket a "En proceso" solo si el estado actual es "Asignado"
                


                /*
                    Zona para logica de Email para mandar en proceso del Ticket (probablemente)
                 */


                    EnviarCorreoEstado(ticket.ticketId, "En proceso");


                // Registrar el cambio de estado en el historial
                var historialEstado2 = new historialEstados
                {
                    ticketId = ticket.ticketId,
                    usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"),
                    estadoAnteriorId = ticket.estadoId,
                    estadoNuevoId = idEstado,
                    fechaNuevo = DateTime.Now

                };
                ticket.estadoId = idEstado;
                _ticketsDbContext.SaveChanges();
                _ticketsDbContext.historialEstados.Add(historialEstado2);
                _ticketsDbContext.SaveChanges();
            }

            var momentaneo = tarea.estadoId;
            if (tarea != null)
            {

                tarea.estadoId = estadoId;
                _ticketsDbContext.SaveChanges();
                if (estadoNombre == Espera)
                {
                    //Aqui 
                    /*
                        Zona para logica de Email para mandar correo al cliente para que revise los comentarios (probablemente)
                     *
                     */
                    EnviarCorreoEstado(tarea.ticketId, "En espera de revisión por parte de cliente"); // Se agrego por lo de correo

                }
            }

            //Se colocó en este punto porque si se colocaba arriba (en la zona marcada) se mandaba 3 veces
            // En proceso, en espera y en finalizado, ya acá solo se manda en proceso

            //Pero quiza se mandaba 3 veces por que se usaba una variable que no era, no se, es de ver

            /*Se agregó para el envío de correo a todos los Administradores, cuando una tarea se marque como finalizada*/
            // Se puso una plantilla en específico para los Administradores en vez de usar el método que contiene la plantilla para clientes
            // Para indicarles que revisen la tarea
            if (estadoNombre == "Finalizado")
            {
                var administradores = (
                    from u in _ticketsDbContext.usuario
                    join r in _ticketsDbContext.rol on u.rolId equals r.rolId
                    where r.nombre == "Administrador" && u.activo == true
                    select u
                ).ToList();

                var ticketRelacionado = _ticketsDbContext.ticket.Find(tarea.ticketId);

                foreach (var admin in administradores)
                {
                    string asunto = $"Tarea #{tarea.tareaId} finalizada";
                    string cuerpo = $"Es un gusto saludarte Administrador/a {admin.nombre},\n\n" +
                                    $"La tarea #{tarea.tareaId} asociada al ticket #{ticketRelacionado.ticketId} ha sido marcada como Finalizada.Puede ingresar al sistema para revisarla.\n\n" +
                                    $"Descripción del ticket: {ticketRelacionado.descripcion}\n\n" +
                                    $"Atentamente,\nEquipo de Ayudín";

                    correo enviarCorreo = new correo(_configuration);
                    enviarCorreo.enviar(
                        destinatario: admin.correo,
                        asunto: asunto,
                        cuerpo: cuerpo
                    );
                }
                /*Fin plantilla*/


                
            }
            // Registrar el cambio de estado en el historial
            var historialEstado = new historialEstados
            {
                ticketId = tarea.ticketId,
                usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"),
                estadoAnteriorId = momentaneo,
                estadoNuevoId = estadoId,
                fechaNuevo = DateTime.Now,
                tareaId = id

            };
            _ticketsDbContext.historialEstados.Add(historialEstado);
            _ticketsDbContext.SaveChanges();
            if (estadoNombre == Finalizado)
            {
                var VerificarTareasTerminadas = (from t in _ticketsDbContext.tarea
                                                 join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                                 where t.ticketId == tarea.ticketId
                                                 select new
                                                 {
                                                     Estado = e.nombre
                                                 }).ToList();
                bool todasFinalizadas = false;
                int contadorNOFinalizadas = 0;
                foreach (var tareaVerificar in VerificarTareasTerminadas)
                {
                    if (tareaVerificar.Estado != Finalizado && tareaVerificar.Estado != "Cancelado")
                    {
                        contadorNOFinalizadas += 1;
                    }
                }
                if (contadorNOFinalizadas == 0)
                {
                    //Como todas las tareas estan finalizdas entonces cambiaremos el estado de Ticket a "En espera del Cliente" y guardamos en historial
                    //Se obtiene el id de estado de "En espera del Cliente" 
                    var idEstado1 = (from e in _ticketsDbContext.estado
                                     where e.nombre == Espera
                                     select e.estadoId).FirstOrDefault();
                    //Se actualiza el estado del Ticket a "En espera del Cliente" solo si todas las tareas estan finalizadas
                    //(no poner otro Return RedirectToAction por que sino corta el codigo)
                    //Espacio para email
                    /*
                     * 
                     * 
                     * 
                     *Espacio para logica email de Tecnico a admin
                     * 
                     * 
                     * Tambien notificacion de cambio de estado del ticket ( se modifcara para que el usuario entienda que tiene que revisar y confirmar)
                     * 
                     */
                    //Email de Técnico a Administrador
                    int usuarioId = HttpContext.Session.GetInt32("usuarioId") ?? 0;
                    var usuarioTecnico = _ticketsDbContext.usuario.Find(usuarioId);

             
                    var admins = (from u in _ticketsDbContext.usuario
                                  join r in _ticketsDbContext.rol on u.rolId equals r.rolId
                                  where r.nombre == "Administrador" && u.activo == true
                                  select u).ToList();

               
                    string fecha = DateTime.Now.ToString("dd/MM/yyyy");
                    string hora = DateTime.Now.ToString("HH:mm");

                   
                    string descripcionProblema = ticket.descripcion;
                    string asuntoCorreo = $"Ticket #{ticket.ticketId} en espera del cliente";

                   
                    foreach (var admin in admins)
                    {
                        string cuerpoCorreo = $"Estimado/a Administrador/a {admin.nombre},\n\n" +
                                              $"Le informamos que el técnico {usuarioTecnico.nombre} ha marcado el ticket #{ticket.ticketId} como: En espera del cliente.\n\n" +
                                              $"Esto indica que ya se ha brindado una resolución y se espera que el cliente la revise.\n\n" +
                                              $"Descripción del ticket:{descripcionProblema}\n" +
                                              $"Fecha: {fecha} Hora: {hora}\n\n" +
                                              $"Le invitamos a ingresar al sistema para dar seguimiento.\n\n" +
                                              $"Saludos,\nEquipo de Ayudín";

                        correo correoAdmin = new correo(_configuration);
                        correoAdmin.enviar(
                            destinatario: admin.correo,
                            asunto: asuntoCorreo,
                            cuerpo: cuerpoCorreo
                        );
                    }

                    //Email al cliente sobre el cambio de estado y espera de su respuesta
                    var cliente = _ticketsDbContext.usuario.Find(ticket.usuarioCreadorId);
                    string asuntoCliente = $"¿Nos ayudas con tu ticket #{ticket.ticketId}?";
                    string cuerpoCliente = $"Hola {cliente.nombre},\n\n" +
                        $"Hemos finalizado las tareas relacionadas con tu ticket #{ticket.ticketId}. Un administrador se comunicará contigo por medio de una llamada para confirmar si la solución fue satisfactoria.\n\n" +
                        $"Descripción del ticket: {ticket.descripcion}\n\n" +
                        "Gracias por utilizar Ayudín.\n\n" +
                        "Saludos,\nEquipo de Ayudín";

                    correo enviarCorreoCliente = new correo(_configuration);
                    enviarCorreoCliente.enviar(
                        destinatario: cliente.correo,
                        asunto: asuntoCliente,
                        cuerpo: cuerpoCliente
                    );




                    // Registrar el cambio de estado en el historial
                    var momentaneo2 = ticket.estadoId;
                    ticket.estadoId = idEstado1;
                    _ticketsDbContext.SaveChanges();
                    var historialEstado1 = new historialEstados
                    {
                        ticketId = ticket.ticketId,
                        usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"),
                        estadoAnteriorId = momentaneo2,
                        estadoNuevoId = idEstado1,
                        fechaNuevo = DateTime.Now
                    };
                    _ticketsDbContext.historialEstados.Add(historialEstado1);
                    _ticketsDbContext.SaveChanges();


                }

            }
            return RedirectToAction("Dashboard");
        }


        //Método para el envío de correos para En proceso y En espera de la respuesta (Cliente)
        private void EnviarCorreoEstado(int ticketId, string estadoNombre)
        {
            var ticket = _ticketsDbContext.ticket.Find(ticketId);
            if (ticket == null) return;

            var usuarioCliente = _ticketsDbContext.usuario.Find(ticket.usuarioCreadorId);
            if (usuarioCliente == null) return;

            string asunto = $"Ticket #{ticket.ticketId} está en {estadoNombre.ToLower()}";
            string cuerpo = $"Hola {usuarioCliente.nombre},\n\n" +
                            $"Estamos trabajando en tu ticket #{ticket.ticketId} sobre {ticket.descripcion}, tu ticket se encuentra: {estadoNombre}. Nuestro equipo está trabajando con dedicación para ofrecerte la mejor solución a tu caso.\n\n" +
                            "Próximamente te informaremos sobre avances.\n\n" +
                            "Atentamente,\n" +
                            "Equipo de Ayudín";

            correo enviarCorreo = new correo(_configuration);
            enviarCorreo.enviar(
                destinatario: usuarioCliente.correo,
                asunto: asunto,
                cuerpo: cuerpo
            );
        }


        [HttpPost]
        public IActionResult SubirComentario(int id, string comentario, int id2)
        {
            var nuevo = new comentario
            {
                usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"), // Obtener el ID del usuario de la sesión
                ticketId = id2, // Asignar el ID del ticket
                tareaId = id,
                texto = comentario,
                fecha = DateTime.Now
            };

            _ticketsDbContext.comentario.Add(nuevo);
            _ticketsDbContext.SaveChanges();

            return Ok(); // <- para que AJAX reciba 200 OK
        }

        //Nuevo modulo para ver Mis tareas asignadas 
        public IActionResult MisTareas(int? estadoFiltro, int? prioridadFiltro)
        {
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");

            var tareasQuery = from t in _ticketsDbContext.tarea
                              join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                              join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                              join p in _ticketsDbContext.prioridad on ti.prioridadId equals p.prioridadId
                              join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                              join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                              where u.usuarioId == usuarioId && e.nombre != "Finalizado" && e.nombre != "Cancelado"
                              select new
                              {
                                  ID = t.tareaId,
                                  ticketID = ti.ticketId,
                                  Des = pro.nombre,
                                  Estado = e.nombre,
                                  EstadoId = e.estadoId,
                                  Fecha = t.fecha,
                                  Prioridad = p.nombre,
                                  PrioridadId = p.prioridadId
                              };

            // Aplicar filtros
            if (estadoFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.EstadoId == estadoFiltro.Value);
            }

            if (prioridadFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.PrioridadId == prioridadFiltro.Value);
            }

            var tareas = tareasQuery.OrderByDescending(t => t.Fecha).ToList();

            // Datos para los dropdowns de filtros
            var estados = _ticketsDbContext.estado
                .Where(e => e.nombre != "Finalizado" && e.nombre != "Cancelado")
                .ToList();

            var prioridades = _ticketsDbContext.prioridad.ToList();

            ViewData["MisTareas"] = tareas;
            ViewBag.Estados = new SelectList(estados, "estadoId", "nombre", estadoFiltro);
            ViewBag.Prioridades = new SelectList(prioridades, "prioridadId", "nombre", prioridadFiltro);
            ViewBag.EstadoSeleccionado = estadoFiltro;
            ViewBag.PrioridadSeleccionada = prioridadFiltro;

            return View();
        }
        //Vista para ver Mis tareas finalizadas "HistorialTareas"
        public IActionResult HistorialTareas(DateTime? fechaInicio, DateTime? fechaFin, int? estadoFiltro, int? categoriaFiltro)
        {
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");

            var tareasQuery = from t in _ticketsDbContext.tarea
                              join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                              join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                              join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                              join c in _ticketsDbContext.categoria on pro.categoriaId equals c.categoriaId
                              join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                              where u.usuarioId == usuarioId && (e.nombre == Finalizado || e.nombre == "Cancelado")
                              select new
                              {
                                  ID = t.tareaId,
                                  ticketID = ti.ticketId,
                                  Des = pro.nombre,
                                  Estado = e.nombre,
                                  EstadoId = e.estadoId,
                                  Fecha = t.fecha,
                                  Categoria = c.nombre,
                                  CategoriaId = c.categoriaId
                              };

            // Aplicar filtros
            if (fechaInicio.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.Fecha <= fechaFin.Value.AddDays(1));
            }

            if (estadoFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.EstadoId == estadoFiltro.Value);
            }

            if (categoriaFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.CategoriaId == categoriaFiltro.Value);
            }

            var tareas = tareasQuery.ToList();

            // Datos para los dropdowns de filtros
            var estados = _ticketsDbContext.estado
                .Where(e => e.nombre == Finalizado || e.nombre == "Cancelado")
                .ToList();

            var categorias = _ticketsDbContext.categoria.ToList();

            ViewData["MisTareas"] = tareas;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            ViewBag.Estados = new SelectList(estados, "estadoId", "nombre", estadoFiltro);
            ViewBag.Categorias = new SelectList(categorias, "categoriaId", "nombre", categoriaFiltro);
            ViewBag.EstadoSeleccionado = estadoFiltro;
            ViewBag.CategoriaSeleccionada = categoriaFiltro;

            return View();
        }

        [Autenticacion]
        public IActionResult GenerarPDFHistorialTareas(DateTime? fechaInicio, DateTime? fechaFin, int? estadoFiltro, int? categoriaFiltro)
        {
            // Obtener ID del técnico de la sesión
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");

            // Consulta base de tareas
            var tareasQuery = from t in _ticketsDbContext.tarea
                              join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                              join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                              join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                              join c in _ticketsDbContext.categoria on pro.categoriaId equals c.categoriaId
                              join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                              where u.usuarioId == usuarioId && (e.nombre == Finalizado || e.nombre == "Cancelado")
                              select new
                              {
                                  t.tareaId,
                                  ti.ticketId,
                                  Problema = pro.nombre,
                                  Estado = e.nombre,
                                  EstadoId = e.estadoId,
                                  t.fecha,
                                  Categoria = c.nombre,
                                  CategoriaId = c.categoriaId,
                                  t.descripcion
                              };

            // Aplicar filtros si están seleccionados
            if (fechaInicio.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.fecha <= fechaFin.Value.AddDays(1)); // Incluye todo el día final
            }

            if (estadoFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.EstadoId == estadoFiltro.Value);
            }

            if (categoriaFiltro.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.CategoriaId == categoriaFiltro.Value);
            }

            // Ejecutar consulta y ordenar por fecha
            var tareas = tareasQuery.OrderByDescending(t => t.fecha).ToList();

            // Obtener información del técnico para el reporte
            var tecnico = _ticketsDbContext.usuario.FirstOrDefault(u => u.usuarioId == usuarioId);
            var tecnicoNombre = tecnico?.nombre ?? "Técnico";

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
            doc.Add(new Paragraph("Ayudín - Historial de Tareas", titleFont));
            doc.Add(new Paragraph($"Técnico: {tecnicoNombre}", subtitleFont));
            doc.Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy HH:mm"), smallFont));
            doc.Add(new Paragraph(" "));

            // Información de filtros aplicados
            var filtros = new Paragraph("Filtros aplicados: ", subtitleFont);

            if (fechaInicio.HasValue)
            {
                filtros.Add(new Chunk($" Desde: {fechaInicio.Value.ToString("dd/MM/yyyy")}", bodyFont));
            }

            if (fechaFin.HasValue)
            {
                filtros.Add(new Chunk($" Hasta: {fechaFin.Value.ToString("dd/MM/yyyy")}", bodyFont));
            }

            if (estadoFiltro.HasValue)
            {
                var estadoNombre = _ticketsDbContext.estado.FirstOrDefault(e => e.estadoId == estadoFiltro.Value)?.nombre;
                filtros.Add(new Chunk($" Estado: {estadoNombre}", bodyFont));
            }

            if (categoriaFiltro.HasValue)
            {
                var categoriaNombre = _ticketsDbContext.categoria.FirstOrDefault(c => c.categoriaId == categoriaFiltro.Value)?.nombre;
                filtros.Add(new Chunk($" Categoría: {categoriaNombre}", bodyFont));
            }

            if (!fechaInicio.HasValue && !fechaFin.HasValue && !estadoFiltro.HasValue && !categoriaFiltro.HasValue)
            {
                filtros.Add(new Chunk(" Ninguno", bodyFont));
            }

            doc.Add(filtros);
            doc.Add(new Paragraph(" "));

            // Crear tabla para las tareas
            PdfPTable table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1f, 1f, 3f, 2f, 2f, 3f });

            // Encabezados de la tabla
            string[] headers = { "ID Tarea", "Ticket", "Problema", "Categoría", "Estado", "Fecha" };
            foreach (var h in headers)
            {
                table.AddCell(new PdfPCell(new Phrase(h, headerFont)) { BackgroundColor = new BaseColor(240, 240, 240) });
            }

            // Agregar tareas a la tabla
            foreach (var t in tareas)
            {
                table.AddCell(new Phrase(t.tareaId.ToString(), bodyFont));
                table.AddCell(new Phrase($"#{t.ticketId}", bodyFont));
                table.AddCell(new Phrase(t.Problema, bodyFont));
                table.AddCell(new Phrase(t.Categoria, bodyFont));
                table.AddCell(new Phrase(t.Estado, bodyFont));
                table.AddCell(new Phrase(t.fecha.ToString("dd/MM/yyyy HH:mm"), bodyFont));
            }

            doc.Add(table);

            // Agregar resumen al final
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Total de tareas: {tareas.Count}", subtitleFont));

            // Si no hay tareas, mostrar mensaje
            if (tareas.Count == 0)
            {
                doc.Add(new Paragraph("No se encontraron tareas con los filtros seleccionados.", bodyFont));
            }

            doc.Close();

            // Nombre del archivo con filtros aplicados
            string fileName = $"HistorialTareas_{tecnicoNombre}_{DateTime.Now:yyyyMMddHHmm}";
            if (fechaInicio.HasValue) fileName += $"_Desde{fechaInicio.Value:yyyyMMdd}";
            if (fechaFin.HasValue) fileName += $"_Hasta{fechaFin.Value:yyyyMMdd}";
            if (estadoFiltro.HasValue) fileName += $"_Estado{estadoFiltro.Value}";
            if (categoriaFiltro.HasValue) fileName += $"_Categoria{categoriaFiltro.Value}";
            fileName += ".pdf";

            return File(ms.ToArray(), "application/pdf", fileName);
        }

    }
}
