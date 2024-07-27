using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlSoft.Models
{
    public class DepartamentoViewModel
    {
        public string nombreDep { get; set; }
    }

    public class PuestoViewModel
    {
        public List<DepartamentoViewModel> Departamentos { get; set; }

        
        public string NombrePuesto { get; set; }

        public bool EstadoPuesto { get; set; }

        public decimal SalarioHora { get; set; }

        public string NombreDepartamento { get; set; }
    }
}
