namespace Proyecto_DAW_Grupo_10.Models
{
    public class ticket
    {
        public int ticketId { get; set; }
        public string descripcion { get; set; }
        public DateTime fechaApertura { get; set; }
        public DateTime? fechaCierre { get; set; }
        public DateTime? fechaModificacion { get; set; }
        public int problemaId { get; set; }
        public int estadoId { get; set; }
        public int prioridadId { get; set; }
        public int usuarioCreadorId { get; set; }
    }
}
