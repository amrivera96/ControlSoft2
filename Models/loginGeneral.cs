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

        [Required(ErrorMessage = "La contrase�a es obligatoria.")]
        [StringLength(50, ErrorMessage = "La contrase�a no puede exceder los 50 caracteres.")]
        public string Contrase�a { get; set; }

       
        public string Rol { get; set; }
        public bool EstadoCre { get; set; }
        public string Mensaje { get; set; }
    }



}
