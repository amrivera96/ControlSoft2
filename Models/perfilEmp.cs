using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{

    public class PerfilViewModel
    {
        public string IdEmpleado { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string NombrePuesto { get; set; }
        public string Contraseña { get; set; }
    }



}
