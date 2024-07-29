using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace ControlSoft.Models
{

    public class GEmpleadoViewModel
    {
        public int idEmpleado { get; set; }
        public string NombreCompleto { get; set; }
        public string ApellidosCompletos { get; set; }
        public string CorreoElectronico { get; set; }
        public string Telefono { get; set; }
        public string Puesto { get; set; }
        public string Rol { get; set; }
        public DateTime FechaIngreso { get; set; }

        public string Mensaje { get; set; }
    }


}
