using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class problema
    {
        [Key]
        public int problemaId { get; set; }
        public string nombre { get; set; }
        public int categoriaId { get; set; }
        public categoria? categoria { get; set; }
    }
}
