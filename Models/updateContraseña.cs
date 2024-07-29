using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace ControlSoft.Models
{

    public class ActualizarContraseñaViewModel
    {
        public int IdEmpleado { get; set; }
        public string NuevaContraseña { get; set; }
    }


}
