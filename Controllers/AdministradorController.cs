using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Proyecto_DAW_Grupo_10.Models;
using Proyecto_DAW_Grupo_10.Servicios;
using Proyecto_DAW_Grupo_10.ViewModels;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class AdministradorController : Controller
    {
        private IConfiguration _configuration; // Se agrego para los correos
        private readonly TicketsDbContext _ticketsDbContext;

        public AdministradorController(TicketsDbContext ticketsDbContext, IConfiguration configuration)
        {
            _ticketsDbContext = ticketsDbContext;
            _configuration = configuration; //Se agrego para los correos
        }

        //Dashboard
        public IActionResult Dashboard()
        {
            // Obtener conteos totales
            ViewBag.CantidadCreados = _ticketsDbContext.ticket.Count(t => t.estadoId == 1);
            ViewBag.CantidadEnProceso = _ticketsDbContext.ticket.Count(t => t.estadoId == 4);
            ViewBag.CantidadFinalizados = _ticketsDbContext.ticket.Count(t => t.estadoId == 6);

            // Obtener total general para calcular porcentajes
            int totalTickets = ViewBag.CantidadCreados + ViewBag.CantidadEnProceso + ViewBag.CantidadFinalizados;

            // Calcular porcentajes (evitando división por cero)
            ViewBag.PorcentajeCreados = totalTickets > 0 ? (ViewBag.CantidadCreados * 100 / totalTickets) : 0;
            ViewBag.PorcentajeEnProceso = totalTickets > 0 ? (ViewBag.CantidadEnProceso * 100 / totalTickets) : 0;
            ViewBag.PorcentajeFinalizados = totalTickets > 0 ? (ViewBag.CantidadFinalizados * 100 / totalTickets) : 0;

            return View();
        }

        //Usuarios
        public IActionResult Usuarios(bool? tipoUsuario = true)
        {
            ViewBag.Roles = _ticketsDbContext.rol.ToList();
            ViewBag.TiposUsuario = new List<SelectListItem>
            {
                 new SelectListItem { Text = "Interno", Value = "true" },
                 new SelectListItem { Text = "Externo", Value = "false" }
             };
            ViewBag.Estados = new List<SelectListItem>
            {
                 new SelectListItem { Text = "Activo", Value = "true" },
                 new SelectListItem { Text = "Inactivo", Value = "false" }
            };

            IQueryable<usuario> query = _ticketsDbContext.usuario.Include(u => u.rol);

            // Aplicar filtro si se especificó
            if (tipoUsuario.HasValue)
            {
                query = query.Where(u => u.tipoUsuario == tipoUsuario.Value);
            }

            var usuarios = query.ToList();
            return View(usuarios);
        }

        //Agregando el correo 
        [HttpPost]
        public IActionResult Create(usuario user)
        {
            if (ModelState.IsValid)
            {
                _ticketsDbContext.usuario.Add(user);
                _ticketsDbContext.SaveChanges();

                // Agregando el apartado de correo
                string fechaActivacion = DateTime.Now.ToString("dd/MM/yyyy");
                string asunto = "¡Bienvenido/a a Ayudín – Tu cuenta ha sido creada";
                string cuerpo = $"Hola {user.nombre},\n\n" +
                    "¡Es un placer darte la bienvenida a Ayudín! Tu cuenta ha sido creada exitosamente.\n\n" +
                    "Tus credenciales de acceso:\n" +
                    $"▸ Usuario: {user.nombre}\n" +
                    $"▸ Contraseña: {user.contrasenia}\n" +
                    $"▸ Fecha de activación: {fechaActivacion}\n\n" +
                    "Primeros pasos: Dirígete a nuestro sitio, ingresa las credenciales y no olvides cambiar tu contraseña en Mi Perfil, Configuración cuenta.\n\n" +
                    "Equipo de Ayudín";

            
                correo enviarCorreo = new correo(_configuration);
                enviarCorreo.enviar(
                    destinatario: user.correo,
                    asunto: asunto,
                    cuerpo: cuerpo
                );
                return RedirectToAction("Usuarios");
            }
            ViewBag.Roles = _ticketsDbContext.rol.ToList();

            return View(user);
        }
        
        public IActionResult Edit(int id)
        {
            var user = _ticketsDbContext.usuario.Find(id);
            return View("_EditUsuario", user); // si se usa modal
        }

        [HttpPost]
        public IActionResult Edit(usuario user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el rol existe
                    var rolExists = _ticketsDbContext.rol.Any(r => r.rolId == user.rolId);
                    if (!rolExists)
                    {
                        ModelState.AddModelError("rolId", "El rol seleccionado no existe");
                        ViewBag.Roles = _ticketsDbContext.rol.ToList();
                        return View("_EditUsuario", user);
                    }

                    // Actualizar solo los campos necesarios (approach más seguro)
                    var existingUser = _ticketsDbContext.usuario.Find(user.usuarioId);
                    if (existingUser != null)
                    {
                        existingUser.nombre = user.nombre;
                        existingUser.correo = user.correo;
                        existingUser.telefono = user.telefono;
                        existingUser.contrasenia = user.contrasenia;
                        existingUser.rolId = user.rolId;
                        existingUser.tipoUsuario = user.tipoUsuario;
                        existingUser.activo = user.activo;

                        _ticketsDbContext.SaveChanges();
                        return RedirectToAction("Usuarios");
                    }
                }
                catch (DbUpdateException ex)
                {
                    // Log the error
                }
            }

            // Si llegamos aquí, algo salió mal
            ViewBag.Roles = _ticketsDbContext.rol.ToList();
            return View(user);
        }

        


        //Otros CRUDS
        //Tipos de Problemas
        public IActionResult ListadoProblemas(string busqueda, int? editarId = null)
        {
            var problemas = from p in _ticketsDbContext.problema
                            join c in _ticketsDbContext.categoria on p.categoriaId equals c.categoriaId
                            select new
                            {
                                p.problemaId,
                                p.nombre,
                                Categoria = c.nombre,
                                c.categoriaId
                            };

            if (!string.IsNullOrEmpty(busqueda))
            {
                problemas = problemas.Where(p => p.nombre.Contains(busqueda));
            }

            var listaProblemas = problemas.ToList();
            var categorias = _ticketsDbContext.categoria.OrderBy(c => c.nombre).ToList();

            ViewBag.Problemas = listaProblemas;
            ViewBag.Categorias = categorias;
            ViewBag.Busqueda = busqueda;

         
            if (editarId.HasValue)
            {
                var problemaAEditar = _ticketsDbContext.problema.FirstOrDefault(p => p.problemaId == editarId);
                ViewBag.ProblemaAEditar = problemaAEditar;
                ViewBag.MostrarModal = true; 
            }
            else
            {
                ViewBag.MostrarModal = false;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarProblema(int problemaId, string nombre, int categoriaId, string busqueda = "")
        {
            if (ModelState.IsValid)
            {
                var problema = _ticketsDbContext.problema.FirstOrDefault(p => p.problemaId == problemaId);
                if (problema == null)
                    return NotFound();

                problema.nombre = nombre;
                problema.categoriaId = categoriaId;
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Problema actualizado correctamente";
            }
            else
            {
                TempData["Error"] = "Error al actualizar el problema";
            }

            return RedirectToAction("ListadoProblemas", new { busqueda = busqueda });


        }

        public IActionResult CrearProblema()
        {
            var categorias = _ticketsDbContext.categoria.OrderBy(c => c.nombre).ToList();
            ViewBag.Categorias = categorias;
            ViewBag.Busqueda = "";

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearProblema(string nombre, int categoriaId, string busqueda = "")
        {
            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(nombre))
            {
                var nuevoProblema = new problema
                {
                    nombre = nombre,
                    categoriaId = categoriaId
                };

                _ticketsDbContext.problema.Add(nuevoProblema);
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Problema creado correctamente";
            }
            else
            {
                TempData["Error"] = "Error al crear el problema";
            }

            return RedirectToAction("ListadoProblemas", new { busqueda = busqueda });
        }

       

        /*Categorias*/
        public IActionResult ListadoCategorias(string busqueda, int? editarId = null)
        {
            var categorias = from c in _ticketsDbContext.categoria
                             select new
                             {
                                 c.categoriaId,
                                 Categoria = c.nombre

                             };

            if (!string.IsNullOrEmpty(busqueda))
            {
                categorias = categorias.Where(c => c.Categoria.Contains(busqueda));
            }

            var listaCategorias = categorias.ToList();
            ViewBag.Categorias = listaCategorias;
            ViewBag.Busqueda = busqueda;


            if (editarId.HasValue)
            {
                var categoriaAEditar = _ticketsDbContext.categoria.FirstOrDefault(c => c.categoriaId == editarId);
                ViewBag.CategoriaAEditar = categoriaAEditar;
                ViewBag.MostrarModal = true;
            }
            else
            {
                ViewBag.MostrarModal = false;
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCategoria(int categoriaId, string nombre, string busqueda = "")
        {
            if (ModelState.IsValid)
            {
                var categoria = _ticketsDbContext.categoria.FirstOrDefault(c => c.categoriaId == categoriaId);
                if (categoria == null)
                    return NotFound();

                categoria.nombre = nombre;
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Categoría actualizada correctamente";
            }
            else
            {
                TempData["Error"] = "Error al actualizar la categoría";
            }

            return RedirectToAction("ListadoCategorias", new { busqueda = busqueda });


        }


        public IActionResult CrearCategoria()
        {
            var categorias = _ticketsDbContext.categoria.OrderBy(c => c.nombre).ToList();
            ViewBag.Categorias = categorias;
            ViewBag.Busqueda = "";

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearCategoria(string nombre, string busqueda = "")
        {
            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(nombre))
            {
                var nuevoCategoria = new categoria
                {
                    nombre = nombre
                };

                _ticketsDbContext.categoria.Add(nuevoCategoria);
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Categoría creada correctamente";
            }
            else
            {
                TempData["Error"] = "Error al crear la categoría";
            }

            return RedirectToAction("ListadoCategorias", new { busqueda = busqueda });
        }

        /*Roles*/
        public IActionResult ListadoRoles(string busqueda, int? editarId = null)
        {
            var roles = from r in _ticketsDbContext.rol
                        join c in _ticketsDbContext.categoria on r.categoriaId equals c.categoriaId
                        select new
                        {
                            r.rolId,
                            r.nombre,
                            Categoria = c.nombre,
                            c.categoriaId
                        };

            if (!string.IsNullOrEmpty(busqueda))
            {
                roles = roles.Where(p => p.nombre.Contains(busqueda));
            }

            var listaRoles = roles.ToList();
            var categorias = _ticketsDbContext.categoria.OrderBy(c => c.nombre).ToList();

            ViewBag.Roles = listaRoles;
            ViewBag.Categorias = categorias;
            ViewBag.Busqueda = busqueda;

            if (editarId.HasValue)
            {
                var rolAEditar = _ticketsDbContext.rol.FirstOrDefault(r => r.rolId == editarId);
                ViewBag.RolAEditar = rolAEditar;
                ViewBag.MostrarModal = true;
            }
            else
            {
                ViewBag.MostrarModal = false;
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarRol(int rolId, string nombre, int categoriaId, string busqueda = "")
        {
            if (ModelState.IsValid)
            {
                var rol = _ticketsDbContext.rol.FirstOrDefault(r => r.rolId == rolId);
                if (rol == null)
                    return NotFound();

                rol.nombre = nombre;
                rol.categoriaId = categoriaId;
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Rol actualizado correctamente";
            }
            else
            {
                TempData["Error"] = "Error al actualizar el rol";
            }

            return RedirectToAction("ListadoRoles", new { busqueda = busqueda });


        }

        public IActionResult CrearRol()
        {
            var categorias = _ticketsDbContext.categoria.OrderBy(c => c.nombre).ToList();
            ViewBag.Categorias = categorias;
            ViewBag.Busqueda = "";

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearRol(string nombre, int categoriaId, string busqueda = "")
        {
            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(nombre))
            {
                var nuevoRol = new rol
                {
                    nombre = nombre,
                    categoriaId = categoriaId
                };

                _ticketsDbContext.rol.Add(nuevoRol);
                _ticketsDbContext.SaveChanges();

                TempData["Mensaje"] = "Rol creado correctamente";
            }
            else
            {
                TempData["Error"] = "Error al crear el rol";
            }

            return RedirectToAction("ListadoRoles", new { busqueda = busqueda });
        }



        #region Emmer

        // Gestión de Tickets
        public IActionResult GestionTickets()
        {
            var ticketsAsignados = _ticketsDbContext.ticket
                .Where(t => _ticketsDbContext.tarea.Any(tar => tar.ticketId == t.ticketId))
                .Include(t => t.problema)
                .ThenInclude(p => p.categoria)
                .Include(t => t.estado)
                .Include(t => t.prioridad)
                .Include(t => t.tarea)
                .ToList();

            var ticketsSinAsignar = _ticketsDbContext.ticket
                .Where(t => !_ticketsDbContext.tarea.Any(tar => tar.ticketId == t.ticketId))
                .Include(t => t.problema)
                .ThenInclude(p => p.categoria)
                .Include(t => t.prioridad)
                .ToList();

            ViewBag.TicketsAsignados = ticketsAsignados;
            ViewBag.TicketsSinAsignar = ticketsSinAsignar;

            return View();
        }

        public IActionResult EditarTicket(int id)
        {
            var ticket = _ticketsDbContext.ticket
                .Include(t => t.problema).ThenInclude(p => p.categoria)
                .Include(t => t.estado)
                .Include(t => t.prioridad)
                .Include(t => t.usuarioCreador)
                .Include(t => t.tarea).ThenInclude(t => t.usuarioAsignado)
                .Include(t => t.tarea).ThenInclude(t => t.estado)
                .Include(t => t.archivosAdjuntos)

                .FirstOrDefault(t => t.ticketId == id);

            ViewBag.Estados = _ticketsDbContext.estado.ToList();
            ViewBag.Prioridades = _ticketsDbContext.prioridad.ToList();
            ViewBag.Categorias = _ticketsDbContext.categoria.ToList();
            ViewBag.Tecnicos = _ticketsDbContext.usuario
                .Where(u => u.rol.nombre == "Tecnico" && u.activo)
                .Select(u => new TecnicoViewModel
                {
                    usuarioId = u.usuarioId,
                    nombre = u.nombre,
                    TicketsActivos = _ticketsDbContext.tarea
                        .Count(t => t.usuarioAsignadoId == u.usuarioId && t.estadoId != 6),
                    categoriaId = u.rol.categoriaId,
                    categoriaNombre = u.rol.categoria.nombre
                })
                .OrderBy(t => t.TicketsActivos)
                .ToList();

            return View("EditarTicket", ticket);
        }

        [HttpPost]
        public IActionResult ActualizarTicket(ticket model, int tecnicoId)
        {
            var ticket = _ticketsDbContext.ticket
                .Include(t => t.tarea)
                .FirstOrDefault(t => t.ticketId == model.ticketId);

            if (ticket != null)
            {
                var usuarioId = HttpContext.Session.GetInt32("usuarioId") ?? 0;
                var estadoAnterior = ticket.estadoId;

                ticket.estadoId = model.estadoId;
                ticket.prioridadId = model.prioridadId;
                ticket.fechaCierre = model.fechaCierre;
                ticket.problemaId = model.problemaId;

                var tarea = ticket.tarea.OrderByDescending(t => t.fecha).FirstOrDefault();
                if (tarea != null)
                {
                    tarea.usuarioAsignadoId = tecnicoId;
                }

                _ticketsDbContext.SaveChanges();

                // Registrar historial de cambio de estado del ticket
                var historial = new historialEstados
                {
                    ticketId = ticket.ticketId,
                    tareaId = null,
                    usuarioId = usuarioId,
                    estadoAnteriorId = estadoAnterior,
                    estadoNuevoId = model.estadoId,
                    fechaNuevo = DateTime.Now
                };
                _ticketsDbContext.historialEstados.Add(historial);
                _ticketsDbContext.SaveChanges();
            }

            return RedirectToAction("GestionTickets");
        }




        public IActionResult EditarTarea(int tareaId)
        {
            var tarea = _ticketsDbContext.tarea
                .Include(t => t.usuarioAsignado)
                .Include(t => t.estado)
                .Include(t => t.ticket)
                .Include(t => t.ticket)
                    .ThenInclude(ti => ti.problema)
                        .ThenInclude(p => p.categoria)

                .FirstOrDefault(t => t.tareaId == tareaId);

            if (tarea == null)
                return NotFound();

            ViewBag.Estados = _ticketsDbContext.estado.ToList();
            ViewBag.Categorias = _ticketsDbContext.categoria.ToList();
            ViewBag.Tecnicos = _ticketsDbContext.usuario
                .Where(u => u.rol.nombre == "Tecnico" && u.activo)
                .Select(u => new TecnicoViewModel
                {
                    usuarioId = u.usuarioId,
                    nombre = u.nombre,
                    TicketsActivos = _ticketsDbContext.tarea
                        .Count(t => t.usuarioAsignadoId == u.usuarioId && t.estadoId != 6),
                    categoriaId = u.rol.categoriaId,
                    categoriaNombre = u.rol.categoria.nombre
                })
                .OrderBy(t => t.TicketsActivos)
                .ToList();

            return View("EditarTarea", tarea);
        }

        [HttpPost]
        public IActionResult EditarTarea(int tareaId, string descripcion, int estadoId, int tecnicoId)
        {
            var tarea = _ticketsDbContext.tarea
                .Include(t => t.ticket)
                .FirstOrDefault(t => t.tareaId == tareaId);

            if (tarea == null)
                return NotFound();

            var usuarioId = HttpContext.Session.GetInt32("usuarioId") ?? 0;
            var estadoAnterior = tarea.estadoId;

            tarea.descripcion = descripcion;
            tarea.estadoId = estadoId;
            tarea.usuarioAsignadoId = tecnicoId;
            _ticketsDbContext.SaveChanges();

            // Registrar historial de cambio de estado de la tarea
            var historial = new historialEstados
            {
                ticketId = tarea.ticketId,
                tareaId = tarea.tareaId,
                usuarioId = usuarioId,
                estadoAnteriorId = estadoAnterior,
                estadoNuevoId = estadoId,
                fechaNuevo = DateTime.Now
            };
            _ticketsDbContext.historialEstados.Add(historial);
            _ticketsDbContext.SaveChanges();

            return RedirectToAction("EditarTicket", new { id = tarea.ticketId });
        }



        public IActionResult AsignarTicket(int id)
        {
            var ticket = _ticketsDbContext.ticket
                .Include(t => t.problema).ThenInclude(p => p.categoria)
                .Include(t => t.usuarioCreador)
                .Include(t => t.prioridad)
                .Include(t => t.estado)
                .Include(t => t.archivosAdjuntos)
                .FirstOrDefault(t => t.ticketId == id);

            var categorias = _ticketsDbContext.categoria.ToList();

            var tecnicos = _ticketsDbContext.usuario
                .Where(u => u.rol.nombre == "Tecnico" && u.activo)
                .Select(u => new TecnicoViewModel
                {
                    usuarioId = u.usuarioId,
                    nombre = u.nombre,
                    TicketsActivos = _ticketsDbContext.tarea
                        .Count(t => t.usuarioAsignadoId == u.usuarioId && t.estadoId != 6),
                    categoriaId = u.rol.categoriaId,
                    categoriaNombre = u.rol.categoria.nombre
                })
                .OrderBy(t => t.TicketsActivos)
                .ToList();

            ViewBag.Categorias = categorias;
            ViewBag.Tecnicos = tecnicos;

            return View("AsignarTicket", ticket);
        }




        [HttpPost]
        public IActionResult CrearTarea(int ticketId, int tecnicoId, string descripcion)
        {

            var nuevaTarea = new tarea
            {
                ticketId = ticketId,
                usuarioAsignadoId = tecnicoId,
                estadoId = 3, // Asignado
                descripcion = descripcion,
                fecha = DateTime.Now
            };

            _ticketsDbContext.tarea.Add(nuevaTarea);
            _ticketsDbContext.SaveChanges();

            var ticket = _ticketsDbContext.ticket.Find(ticketId);
            if (ticket != null && ticket.estadoId == 1)
            {
                ticket.estadoId = 3;
                _ticketsDbContext.SaveChanges();
            }

            return RedirectToAction("GestionTickets");
        }


        [HttpGet]
        public JsonResult ObtenerTecnicosPorCategoria(int categoriaId)
        {
            var tecnicos = _ticketsDbContext.usuario
                .Where(u => u.rol.categoriaId == categoriaId && u.rol.nombre == "Tecnico" && u.activo)
                .Select(u => new
                {
                    u.usuarioId,
                    u.nombre,
                    TicketsActivos = _ticketsDbContext.tarea
                        .Count(t => t.usuarioAsignadoId == u.usuarioId && t.estadoId != 6)
                })
                .OrderBy(t => t.TicketsActivos)
                .ToList();

            return Json(tecnicos);
        }

        [HttpGet]
        public JsonResult ObtenerProblemasPorCategoria(int categoriaId)
        {
            var problemas = _ticketsDbContext.problema
                .Where(p => p.categoriaId == categoriaId)
                .Select(p => new
                {
                    p.problemaId,
                    p.nombre
                })
                .ToList();

            return Json(problemas);
        }


        [HttpPost]
        public IActionResult CancelarTarea(int tareaId)
        {
            var tarea = _ticketsDbContext.tarea.FirstOrDefault(t => t.tareaId == tareaId);

            if (tarea != null && tarea.estadoId != 2 && tarea.estadoId != 6) // No Cancelado ni Finalizado
            {
                tarea.estadoId = 2; // Estado Cancelado
                _ticketsDbContext.SaveChanges();
                return Ok();
            }

            return BadRequest("La tarea no puede ser cancelada.");
        }

        [HttpPost]
        public IActionResult CambiarEstadoTarea(int tareaId, int nuevoEstadoId)
        {
            var tarea = _ticketsDbContext.tarea
                .Include(t => t.ticket)
                .FirstOrDefault(t => t.tareaId == tareaId);

            if (tarea == null)
                return NotFound();

            var usuarioId = HttpContext.Session.GetInt32("usuarioId") ?? 0;

            // Guardar estado anterior de la tarea
            var estadoAnteriorTarea = tarea.estadoId;

            // Actualizar estado de la tarea
            tarea.estadoId = nuevoEstadoId;
            _ticketsDbContext.SaveChanges();

            // Registrar cambio de estado de la tarea
            var historialTarea = new historialEstados
            {
                ticketId = tarea.ticketId,
                tareaId = tarea.tareaId,
                usuarioId = usuarioId,
                estadoAnteriorId = estadoAnteriorTarea,
                estadoNuevoId = nuevoEstadoId,
                fechaNuevo = DateTime.Now
            };
            _ticketsDbContext.historialEstados.Add(historialTarea);
            _ticketsDbContext.SaveChanges();

            // Si el ticket está en estado "Asignado", lo pasamos a "En proceso"
            var estadoAsignado = _ticketsDbContext.estado.FirstOrDefault(e => e.nombre == "Asignado");
            var estadoEnProceso = _ticketsDbContext.estado.FirstOrDefault(e => e.nombre == "En proceso");

            if (tarea.ticket.estadoId == estadoAsignado?.estadoId)
            {
                var estadoAnteriorTicket = tarea.ticket.estadoId;
                tarea.ticket.estadoId = estadoEnProceso.estadoId;
                _ticketsDbContext.SaveChanges();

                // Registrar cambio de estado del ticket
                var historialTicket = new historialEstados
                {
                    ticketId = tarea.ticketId,
                    tareaId = null, // 👈 null para cambios de ticket
                    usuarioId = usuarioId,
                    estadoAnteriorId = estadoAnteriorTicket,
                    estadoNuevoId = estadoEnProceso.estadoId,
                    fechaNuevo = DateTime.Now
                };
                _ticketsDbContext.historialEstados.Add(historialTicket);
                _ticketsDbContext.SaveChanges();
            }

            return RedirectToAction("EditarTicket", new { id = tarea.ticketId });
        }



        #endregion

    }
}
