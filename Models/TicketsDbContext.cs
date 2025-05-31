using Microsoft.EntityFrameworkCore;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class TicketsDbContext : DbContext
    {
        public TicketsDbContext(DbContextOptions options) : base(options) 
        {

        }
        public DbSet<rol> rol { get; set; }
        public DbSet<usuario> usuario { get; set; }
        public DbSet<historialEstados> historialEstados { get; set; }
        public DbSet<ticket> ticket { get; set; }
        public DbSet<problema> problema { get; set; }
        public DbSet<categoria> categoria { get; set; }
        public DbSet<estado> estado { get; set; }
        public DbSet<prioridad> prioridad { get; set; }
        public DbSet<tarea> tarea { get; set; }
        public DbSet<comentario> comentario { get; set; }
        public DbSet<archivosAdjuntos> archivosAdjuntos { get; set; }

    }
}
