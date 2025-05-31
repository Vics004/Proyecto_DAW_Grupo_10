using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class historialEstados
    {
        [Key]
        public int historialId { get; set; }
        public int ticketId { get; set; }
        public int? estadoAnteriorId { get; set; }
        public int estadoNuevoId { get; set; }
        public int usuarioId { get; set; }
        public string descripcion { get; set; }
        public DateTime fechaNuevo { get; set; }
        public int? tareaId { get; set; }
    }
}
