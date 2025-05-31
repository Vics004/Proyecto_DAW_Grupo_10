using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class comentario
    {
        [Key]
        public int comentarioId { get; set; }
        public string texto { get; set; }
        public DateTime fecha { get; set; }
        public int? tareaId { get; set; }
        public int usuarioId { get; set; }
        public int ticketId { get; set; }

    }
}
