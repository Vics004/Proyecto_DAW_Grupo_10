using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class rol
    {
        [Key]
        public int rolId { get; set; }
        public string? nombre { get; set; }
        public int categoriaId { get; set; }
    }
}
