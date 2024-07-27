using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{

    public class LoginViewModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El usuario no puede exceder los 50 caracteres.")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, ErrorMessage = "La contraseña no puede exceder los 50 caracteres.")]
        public string Contraseña { get; set; }

       
        public string Rol { get; set; }
        public bool EstadoCre { get; set; }
        public string Mensaje { get; set; }
    }



}
