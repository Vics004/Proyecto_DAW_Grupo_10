using Microsoft.AspNetCore.Mvc;
using Proyecto_DAW_Grupo_10.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using static Proyecto_DAW_Grupo_10.Servicios.AutenticationAttribute;

namespace Proyecto_DAW_Grupo_10.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;


        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [Autenticacion]
        public IActionResult Index()
        {
            return View();
        }
        [Autenticacion]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
