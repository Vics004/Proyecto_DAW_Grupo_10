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
