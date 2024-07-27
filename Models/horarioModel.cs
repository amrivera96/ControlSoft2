using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{

    public class UsuarioViewModel
    {
        public string idEmpleado { get; set; }
        public string NombreCompleto { get; set; }
        public string Puesto { get; set; }
        public DateTime FechaIngreso { get; set; }
    }

    public class TurnoTrabajo
    {
        public int IdTurno { get; set; }
        public string TurnoDescripcion { get; set; }
    }



}
