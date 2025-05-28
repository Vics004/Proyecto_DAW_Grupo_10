using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Proyecto_DAW_Grupo_10.Servicios
{
    public class AutenticationAttribute
    {
        public class AutenticacionAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(ActionExecutingContext context)
            {
                var usuarioId = context.HttpContext.Session.GetInt32("usuarioId");

                if (usuarioId == null)
                {
                    context.Result = new RedirectToActionResult("Autenticar", "login", null);
                }
                base.OnActionExecuting(context);
            }
        }
    }
}
