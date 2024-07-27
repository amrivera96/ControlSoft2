using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace ControlSoft.Models
{

    public class TiposInconsistencias
    {
        public int idTipoInconsistencia { get; set; }
        public string nombreInconsistencia { get; set; }
        public string descInconsistencia { get; set; }
        public bool estadoTipoInconsistencia { get; set; }
        public DateTime fechaCreacion { get; set; }
    }

    public class RegistroInconsistencia
    {
        public int idInconsistencia { get; set; }
        public int idEmpleado { get; set; }
        public int idTipoInconsistencia { get; set; }
        public string nombreTipoInconsistencia { get; set; }
        public DateTime fechaInconsistencia { get; set; }
        public bool estadoInconsistencia { get; set; }
        public int? idJustificacion { get; set; }
        public bool? estadoJustificacion { get; set; }
        public GestionInconsistencia Gestion { get; set; }
        public JustificacionInconsistencia Justificacion { get; set; }
        public string nombreEmpleado { get; set; } // Nueva propiedad para el nombre del empleado
    }


    public class GestionInconsistencia
    {
        public int idGestion { get; set; }
        public int idInconsistencia { get; set; }
        public DateTime? fechaGestion { get; set; }
        public bool estadoGestion { get; set; } // 0 = No gestionada, 1 = Gestionada
        public int? idJefe { get; set; }
        public string observacionGestion { get; set; }
    }

    public class JustificacionInconsistencia
    {
        public int idJustificacion { get; set; }
        public int idInconsistencia { get; set; }
        public bool estadoJustificacion { get; set; } // 0 = No justificada, 1 = Justificada
        public DateTime? fechaJustificacion { get; set; }
        public string descripcionJustificacion { get; set; }
        public byte[] adjuntoJustificacion { get; set; }
    }


    public class TiposActividades
    {
        public int idAct { get; set; }
        public string nombreAct { get; set; }
        public string descpAct { get; set; }
        public DateTime fechaCreacion { get; set; }
        public bool estadoAct { get; set; }
    }


    public class RegistrarActividadViewModel
    {
        public List<TiposActividades> TiposActividades { get; set; }
        public List<RegistroActividades> RegistroActividades { get; set; }
        public int IdEmpleado { get; set; }
    }

    public class RegistroActividades
    {
        public int idRegAct { get; set; }
        public int idGesAct { get; set; }
        public int idAct { get; set; }
        public int idEmp { get; set; }
        public string nombreEmpleado { get; set; } // Nueva propiedad para el nombre del empleado
        public DateTime fechaAct { get; set; }
        public TimeSpan horaInicio { get; set; }
        public TimeSpan horaFinal { get; set; }
        public TimeSpan duracionAct { get; set; }
        public bool estadoReg { get; set; }
        public TiposActividades Actividad { get; set; }
    }


    public class GestionActividades
    {
        public int idGesAct { get; set; }
        public DateTime? fechaGesAct { get; set; }
        public string obserGest { get; set; }
        public bool? estadoGesAct { get; set; }
        public int idJefe { get; set; }
    }


    public class MonitoreoRendimiento
    {
        public int idEmp { get; set; }
        public string nombre { get; set; }
        public DateTime fechaAct { get; set; }
        public int TotalActividades { get; set; }
        public int ActividadesAprobadas { get; set; }
        public int ActividadesRechazadas { get; set; }
        public double TotalHoras { get; set; }
        public double TiempoPromedioPorActividad { get; set; }
    }

    public class SolicitudHoras
    {
        public int idSolicitud { get; set; }
        public DateTime fechaSolicitud { get; set; }
        public int idJefe { get; set; }
        public int idEmpleado { get; set; }
        public int idAct { get; set; }
        public decimal cantidadHoras { get; set; }
        public DateTime fechaSolicitada { get; set; }
        public string motivoSolicitud { get; set; }
    }

    public class HistorialHoras
    {
        public int idSolicitud { get; set; }
        public string Empleado { get; set; }
        public string Actividad { get; set; }
        public decimal cantidadHoras { get; set; }
        public string Estado { get; set; }
        public DateTime fechaSolicitud { get; set; }
        public DateTime fechaSolicitada { get; set; }
        public string motivoSolicitud { get; set; }
    }



    public class Empleado
    {
        public int idEmpleado { get; set; }
        public string nombre { get; set; }
        public string apellidos { get; set; }
        public string correo { get; set; }
        public string telefono { get; set; }
        public int idPuesto { get; set; }
        public bool estadoEmp { get; set; }
        public DateTime fechaIngreso { get; set; }
    }

}



