using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ControlSoft.Models
{
    public class marcas
    {
        public int idMarca { get; set; }
        public int idEmpleado { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
    }
}