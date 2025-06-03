using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_DAW_Grupo_10.Models;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly TicketsDbContext _ticketsDbContext;

        public AdministradorController(TicketsDbContext ticketsDbContext)
        {
            _ticketsDbContext = ticketsDbContext;
        }

        //Dashboard
        public IActionResult Dashboard()
        {
            // Consulta para tickets creados (estadoId = 1)
            var ticketsCreados = (from t in _ticketsDbContext.ticket
                                  join u in _ticketsDbContext.usuario on t.usuarioCreadorId equals u.usuarioId
                                  join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                  join p in _ticketsDbContext.prioridad on t.prioridadId equals p.prioridadId
                                  where t.estadoId == 1
                                  select new
                                  {
                                      t.ticketId,
                                      t.descripcion,
                                      UsuarioNombre = u.nombre,
                                      PrioridadNombre = p.nombre,
                                      t.fechaApertura,
                                      EstadoNombre = e.nombre
                                  }).Take(3).ToList();

            // Consulta para tickets en proceso (estadoId = 4)
            var ticketsEnProceso = (from t in _ticketsDbContext.ticket
                                    join u in _ticketsDbContext.usuario on t.usuarioCreadorId equals u.usuarioId
                                    join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                    join p in _ticketsDbContext.prioridad on t.prioridadId equals p.prioridadId
                                    where t.estadoId == 4
                                    select new
                                    {
                                        t.ticketId,
                                        t.descripcion,
                                        UsuarioNombre = u.nombre,
                                        PrioridadNombre = p.nombre,
                                        t.fechaApertura,
                                        t.fechaModificacion,
                                        EstadoNombre = e.nombre
                                    }).Take(3).ToList();

            // Consulta para tickets finalizados (estadoId = 6)
            var ticketsFinalizados = (from t in _ticketsDbContext.ticket
                                      join u in _ticketsDbContext.usuario on t.usuarioCreadorId equals u.usuarioId
                                      join e in _ticketsDbContext.estado on t.estadoId equals e.estadoId
                                      join p in _ticketsDbContext.prioridad on t.prioridadId equals p.prioridadId
                                      where t.estadoId == 6
                                      select new
                                      {
                                          t.ticketId,
                                          t.descripcion,
                                          UsuarioNombre = u.nombre,
                                          PrioridadNombre = p.nombre,
                                          t.fechaApertura,
                                          t.fechaCierre,
                                          EstadoNombre = e.nombre
                                      }).Take(3).ToList();

            // Obtener conteos totales (sin limitar a 3)
            ViewBag.CantidadCreados = _ticketsDbContext.ticket.Count(t => t.estadoId == 1);
            ViewBag.CantidadEnProceso = _ticketsDbContext.ticket.Count(t => t.estadoId == 4);
            ViewBag.CantidadFinalizados = _ticketsDbContext.ticket.Count(t => t.estadoId == 6);

            // Pasar los datos a la vista
            ViewBag.TicketsCreados = ticketsCreados;
            ViewBag.TicketsEnProceso = ticketsEnProceso;
            ViewBag.TicketsFinalizados = ticketsFinalizados;
            

            return View();
        }

        //Usuarios
        public IActionResult Usuarios()
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


            var usuarios = _ticketsDbContext.usuario.Include(u => u.rol).ToList();

            var roles = _ticketsDbContext.rol.ToList(); // Asegúrate de que haya datos en la tabla
            ViewBag.Roles = roles; // No usamos SelectList, lo dejamos más simple

            return View(usuarios);
        }


        [HttpPost]
        public IActionResult Create(usuario user)
        {
            if (ModelState.IsValid)
            {
                _ticketsDbContext.usuario.Add(user);
                _ticketsDbContext.SaveChanges();
                return RedirectToAction("Usuarios");
            }
            ViewBag.Roles = _ticketsDbContext.rol.ToList();
            return View(user);
        }
        
        /*
        public IActionResult CrearUsuario()
        {
            var roles = _ticketsDbContext.rol?.ToList() ?? new List<rol>();
            ViewBag.Roles = new SelectList(roles, "rolId", "nombre");
            return View("_CrearUsuario"); 
        }
        */


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

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _ticketsDbContext.usuario.Find(id);
            if (user != null)
            {
                _ticketsDbContext.usuario.Remove(user);
                _ticketsDbContext.SaveChanges();
            }
            return RedirectToAction("Usuarios");
        }


        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _ticketsDbContext.usuario.Find(id);
            if (user != null)
            {
                _ticketsDbContext.usuario.Remove(user);
                _ticketsDbContext.SaveChanges();
            }
            return RedirectToAction("Usuarios");
        }



    }
}
