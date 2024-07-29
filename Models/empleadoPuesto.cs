using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace ControlSoft.Models
{
    
    public class EmpleadoViewModel
    {
        public int IdEmpleado { get; set; }
        public string NombreCompleto { get; set; }
        public string Puesto { get; set; }
        public DateTime FechaIngreso { get; set; }
    }

}
