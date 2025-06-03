using System.ComponentModel.DataAnnotations;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class archivosAdjuntos
    {
        [Key]
        public int archivoId { get; set; }
        public int? tareaId { get; set; }
        public int usuarioId { get; set; }
        public string nombreArchivo { get; set; }
        public string tipoArchivo { get; set; }
        public string rutaArchivo { get; set; }
        public DateTime fechaCarga { get; set; }
        public int ticketId { get; set; }
        public tarea? tarea { get; set; }
        public usuario? usuario { get; set; }
        public ticket? ticket { get; set; }

    }
}
