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
    }
}
