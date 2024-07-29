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

    public class HorarioViewModel
    {
        public int IdEmpleado { get; set; }
        public string Fecha { get; set; }
        public List<Horario> Horarios { get; set; }
    }

    public class Horario
    {
        public int? IdTurno { get; set; }
        public string NombreTurno { get; set; }
        public string Fecha { get; set; }
    }


}
