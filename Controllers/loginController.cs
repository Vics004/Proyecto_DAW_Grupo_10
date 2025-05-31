using Microsoft.AspNetCore.Mvc;
using Proyecto_DAW_Grupo_10.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static Proyecto_DAW_Grupo_10.Servicios.AutenticationAttribute;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class loginController : Controller
    {
        private readonly ILogger<loginController> _logger;
        private readonly TicketsDbContext _TicketsDbContexto;
        public loginController(ILogger<loginController> logger, TicketsDbContext TicketsDbContexto)
        {
            _TicketsDbContexto = TicketsDbContexto;
            _logger = logger;
        }

        [Autenticacion]
        public IActionResult Index()
        {
            // Obtener datos de sesión
            var usuarioId = HttpContext.Session.GetInt32("usuarioId");
            var rolId = HttpContext.Session.GetInt32("rolId");
            var nombreUsuario = HttpContext.Session.GetString("nombre");

            // Si no está autenticado, redirigir a login
            if (usuarioId == null)
            {
                return RedirectToAction("Autenticar", "Login");
            }

            // Obtener información actualizada del usuario con join explícito
            var usuarioInfo = (from u in _TicketsDbContexto.usuario
                               join r in _TicketsDbContexto.rol on u.rolId equals r.rolId
                               where u.usuarioId == usuarioId
                               select new
                               {
                                   rolNombre = r.nombre,
                                   rolId = r.rolId
                               }).FirstOrDefault();

            // Establecer ViewBag con el layout correspondiente
            switch (usuarioInfo?.rolId ?? rolId)
            {
                case 1:
                    ViewBag.Layout = "_Layout_Cliente";
                    ViewData["tipoUsuario"] = "Cliente";
                    break;
                case 2:
                    ViewBag.Layout = "_Layout_Tecnico";
                    ViewData["tipoUsuario"] = "Tecnico";
                    break;
                case 3:
                    ViewBag.Layout = "_Layout";
                    ViewData["tipoUsuario"] = "Administrador";
                    break;
            }

            ViewBag.nombre = nombreUsuario;
            return View();
        }

        public IActionResult Autenticar()
        {
            ViewData["ErrorMessage"] = "";
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Autenticar(string txtUsuario, string txtClave)
        {
            var usuario = await (from u in _TicketsDbContexto.usuario
                                 join r in _TicketsDbContexto.rol on u.rolId equals r.rolId
                                 where u.nombre == txtUsuario
                                 && u.contrasenia == txtClave
                                 && (u.rolId == 1 || u.rolId == 2 || u.rolId == 3)
                                 && u.activo == true
                                 select new
                                 {
                                     usuario = u,
                                     rolNombre = r.nombre,
                                     rolId = r.rolId
                                 }).FirstOrDefaultAsync();

            if (usuario != null)
            {
                HttpContext.Session.SetInt32("usuarioId", usuario.usuario.usuarioId);
                HttpContext.Session.SetString("TipoUsuario", usuario.rolNombre);
                HttpContext.Session.SetString("Nombre", usuario.usuario.nombre);
                HttpContext.Session.SetInt32("RolId", usuario.rolId);

                switch (usuario.rolId)
                {
                    case 1:
                        return RedirectToAction("Index", "login");
                    case 2:
                        return RedirectToAction("Index", "login");
                    case 3:
                        return RedirectToAction("Index", "login");
                }
            }

            ViewData["ErrorMessage"] = "Error, usuario inválido";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
