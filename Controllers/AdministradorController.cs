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

        /*[HttpPost]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var usuario = await _ticketsDbContext.usuario.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            usuario.activo = !usuario.activo;
            await _ticketsDbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Usuarios));
        }*/


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

       

    }
}
