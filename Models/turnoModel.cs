using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{

    public class TurnoViewModel
    {
        public string NombreTurno { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool EstadoTurno { get; set; }
    }
}
