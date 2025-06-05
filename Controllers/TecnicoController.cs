using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_DAW_Grupo_10.Models;
using Proyecto_DAW_Grupo_10.Servicios;

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
                ticket.estadoId = idEstado;
                _ticketsDbContext.SaveChanges();


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
                _ticketsDbContext.historialEstados.Add(historialEstado2);
                _ticketsDbContext.SaveChanges();
            }


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


                

                return RedirectToAction("Dashboard");
            }
            // Registrar el cambio de estado en el historial
            var historialEstado = new historialEstados
            {
                ticketId = tarea.ticketId,
                usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"),
                estadoAnteriorId = tarea.estadoId,
                estadoNuevoId = estadoId,
                fechaNuevo = DateTime.Now,
                tareaId = id

            };
            _ticketsDbContext.historialEstados.Add(historialEstado);
            _ticketsDbContext.SaveChanges();
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
        public IActionResult MisTareas()
        {
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");
            var tareas = (from t in _ticketsDbContext.tarea
                          join u in _ticketsDbContext.usuario on t.usuarioAsignadoId equals u.usuarioId
                          join ti in _ticketsDbContext.ticket on t.ticketId equals ti.ticketId
                          join p in _ticketsDbContext.prioridad on ti.prioridadId equals p.prioridadId
                          join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                          join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                          where u.usuarioId == usuarioId && e.nombre != creado && e.nombre != Finalizado && e.nombre != "Cancelado"
                          select new
                          {
                              ID = t.tareaId,
                              ticketID = ti.ticketId,
                              Des = pro.nombre,
                              Estado = e.nombre,
                              Fecha = t.fecha,
                              Prioridad = p.nombre,
                          }).ToList();

            ViewData["MisTareas"] = tareas;
            return View();
        }
        //Vista para ver Mis tareas finalizadas "HistorialTareas"
        public IActionResult HistorialTareas()
        {
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");
            var tareas = (from t in _ticketsDbContext.tarea
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
                              Fecha = t.fecha,
                              Categoria = c.nombre,
                          }).ToList();
            ViewData["MisTareas"] = tareas;
            return View();
        }

    }
}
