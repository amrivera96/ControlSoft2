using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace ControlSoft.Models
{
    public class DepartamentosViewModel
    {
        [Required(ErrorMessage = "El nombre del departamento es obligatorio.")]
        public string NombreDep { get; set; }

        [Required(ErrorMessage = "El correo del departamento es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string CorreoDep { get; set; }

        [Required(ErrorMessage = "El teléfono del departamento es obligatorio.")]
        [StringLength(15, ErrorMessage = "El teléfono no puede exceder los 15 caracteres.")]
        public string TelefonoDep { get; set; }

        [Required(ErrorMessage = "El estado del departamento es obligatorio.")]
        public bool EstadoDep { get; set; }

        [Required(ErrorMessage = "El jefe del departamento es obligatorio.")]
        public int IdJefe { get; set; }
    }


}