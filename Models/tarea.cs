using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class tarea
    {
        [Key]
        public int tareaId { get; set; }
        public int ticketId { get; set; }
        public int usuarioAsignadoId { get; set; }
        public int estadoId { get; set; }
        public string descripcion { get; set; }
        public DateTime fecha { get; set; }
    }
}
