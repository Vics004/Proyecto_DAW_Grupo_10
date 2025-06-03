using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class ticket
    {
        [Key]
        public int ticketId { get; set; }
        public string descripcion { get; set; }
        public DateTime fechaApertura { get; set; }
        public DateTime? fechaCierre { get; set; }
        public DateTime? fechaModificacion { get; set; }

        public int problemaId { get; set; }
        public int estadoId { get; set; }
        public int prioridadId { get; set; }
        public int usuarioCreadorId { get; set; }
        public problema? problema { get; set; }
        public estado? estado { get; set; }
        public prioridad? prioridad { get; set; }
        public usuario? usuarioCreador { get; set; }
        public ICollection<tarea>? tarea { get; set; }
        public ICollection<archivosAdjuntos>? archivosAdjuntos { get; set; }


    }
}

