using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class estado
    {
        [Key]
        public int estadoId { get; set; }
        public string nombre { get; set; }

        public ICollection<ticket>? tickets { get; set; }
        public ICollection<tarea>? tareas { get; set; }

    }
}
