using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_DAW_Grupo_10.Models;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class TecnicoController : Controller
    {
        private readonly TicketsDbContext _ticketsDbContext;
        public TecnicoController(TicketsDbContext ticketsDbContext)
        {
            _ticketsDbContext = ticketsDbContext;
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
                                    where e.nombre != creado && e.nombre != Finalizado && u.usuarioId == usuario
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
                                    where (e.nombre == Finalizado || e.nombre == "Cancleado") && u.usuarioId == usuario
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
            public string Usuario { get; set; }
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } // "Comentario" o "Cambio de Estado"
            public string Detalle { get; set; }
        }
        [HttpPost]
        public IActionResult Detalles(int id)
        {
            var tareasDetalle = (from ta in _ticketsDbContext.tarea
                                 join u in _ticketsDbContext.usuario on ta.usuarioAsignadoId equals u.usuarioId
                                 join e in _ticketsDbContext.estado on ta.estadoId equals e.estadoId
                                 join ti in _ticketsDbContext.ticket on ta.ticketId equals ti.ticketId
                                 join p in _ticketsDbContext.prioridad on ti.prioridadId equals p.prioridadId
                                 join pro in _ticketsDbContext.problema on ti.problemaId equals pro.problemaId
                                 join c in _ticketsDbContext.categoria on pro.categoriaId equals c.categoriaId
                                 where ta.tareaId == id
                                 select new
                                 {
                                     Usuario = u.nombre,
                                     FechaInicio = ti.fechaApertura,
                                     FechaCierre = ti.fechaCierre,
                                     Estado = e.nombre,
                                     Prioridad = p.nombre,
                                     Categoria = c.nombre,
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

            var estados = (from e in _ticketsDbContext.estado
                           where e.nombre != creado && e.nombre != "Cancelado" && e.nombre != asignado
                           select new SelectListItem
                           {
                               Value = e.estadoId.ToString(),
                               Text = e.nombre
                           }).ToList();

            ViewData["Estados"] = estados;
            ViewBag.ID = id;


            ViewBag.tareasDetalles = tareasDetalle;
            //Obtener los nombres de archivos adjuntos y su fecha de subida
            var idticket = (from ta in _ticketsDbContext.tarea
                            where ta.tareaId == id
                            select ta.ticketId).FirstOrDefault();
            ViewBag.Ticket = idticket;

            var archivosAdjuntos = (from t in _ticketsDbContext.ticket
                                    join a in _ticketsDbContext.archivosAdjuntos on t.ticketId equals a.ticketId
                                    where t.ticketId == idticket
                                    select new
                                    {
                                        NombreArchivo = a.nombreArchivo,
                                        FechaSubida = a.fechaCarga
                                    }).ToList();

            //obtener comentarios y cambios de estado
            var comentariosList = (from com in _ticketsDbContext.comentario
                                   join u in _ticketsDbContext.usuario on com.usuarioId equals u.usuarioId
                                   where com.ticketId == idticket
                                   select new ActividadDTO
                                   {
                                       Usuario = u.nombre,
                                       Fecha = com.fecha,
                                       Tipo = "Comentario",
                                       Detalle = com.texto
                                   }).ToList();

            var historialEstados = (from he in _ticketsDbContext.historialEstados
                                    join u in _ticketsDbContext.usuario on he.usuarioId equals u.usuarioId
                                    join e in _ticketsDbContext.estado on he.estadoNuevoId equals e.estadoId
                                    where he.ticketId == idticket
                                    select new ActividadDTO
                                    {
                                        Usuario = u.nombre,
                                        Fecha = he.fechaNuevo,
                                        Tipo = "Cambio de Estado",
                                        Detalle = "Estado cambiado a: " + e.nombre
                                    }).ToList();
            var actividadCompleta = comentariosList
            .Concat(historialEstados)
            .OrderBy(a => a.Fecha)
            .ToList();

            ViewData["Actividad"] = actividadCompleta;
            ViewData["ArchivosAdjuntos"] = archivosAdjuntos;


            return View();
        }
        [HttpPost]
        public IActionResult CambiarEstado(int id, int estadoId)
        {
            var tarea = _ticketsDbContext.tarea.Find(id);
            if (tarea != null)
            {
                tarea.estadoId = estadoId;
                _ticketsDbContext.SaveChanges();
            }
            // Registrar el cambio de estado en el historial
            var historialEstado = new historialEstados
            {
                ticketId = tarea.ticketId,
                usuarioId = (int)HttpContext.Session.GetInt32("usuarioId"),
                estadoAnteriorId = tarea.estadoId,
                estadoNuevoId = estadoId,
                fechaNuevo = DateTime.Now
            };
            _ticketsDbContext.historialEstados.Add(historialEstado);
            _ticketsDbContext.SaveChanges();

            return RedirectToAction("Dashboard");
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
                          where u.usuarioId == usuarioId && e.nombre != creado && e.nombre != Finalizado && e.nombre != "Calncelado"
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
