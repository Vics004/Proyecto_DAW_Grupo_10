using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Identity.Client;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class usuario
    {
        [Key]
        public int usuarioId { get; set; }
        public string? nombre { get; set; }
        public string? correo { get; set; }
        public string? telefono { get; set; }
        public string? contrasenia { get; set; }
        public string? empresa { get; set; }
        public string? contactoPrincipal { get; set; }
        public string? detalleContacto {  get; set; }
        public bool activo {  get; set; }
        public bool tipoUsuario { get; set; }
        public int rolId { get; set; }
        public rol? rol { get; set; }
    }
}


