﻿using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace Proyecto_DAW_Grupo_10.Models
{
    public class prioridad
    {
        [Key]
        public int prioridadId { get; set; }
        public string nombre { get; set; }

        public ICollection<ticket>? tickets { get; set; }

    }
}
