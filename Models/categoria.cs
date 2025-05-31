namespace Proyecto_DAW_Grupo_10.Models
{
    using System.ComponentModel.DataAnnotations;

    public class categoria
    {
        [Key]
        public int categoriaId { get; set; }
        public string nombre { get; set; }

    }
}
