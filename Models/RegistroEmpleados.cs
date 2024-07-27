using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{
    public class Puesto
    {
        public int idPuesto { get; set; }
        public string nombrePuesto { get; set; }
    }

    public class Rol
    {
        public int idRol { get; set; }
        public string nombreRol { get; set; }
    }
    public class RegistroEmpleadoViewModel
    {
        public List<Puesto> Puestos { get; set; }
        public List<Rol> Roles { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        public int idEmpleado { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre completo no puede exceder los 100 caracteres.")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder los 100 caracteres.")]
        public string ApellidosCompletos { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string CorreoElectronico { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(15, ErrorMessage = "El teléfono no puede exceder los 15 caracteres.")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El puesto es obligatorio.")]
        public string nombrePuesto { get; set; } // Este debe coincidir con el procedimiento almacenado

        [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FechaIngreso { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string nombreRol { get; set; } // Este debe coincidir con el procedimiento almacenado
    }

}
