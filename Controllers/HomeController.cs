using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ControlSoft.Models;

namespace ControlSoft.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ControlSoftCrDbContext"].ConnectionString;


        public ActionResult TestDatabaseConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    return Content("Conexión a la base de datos exitosa.");
                }
            }
            catch (Exception ex)
            {
                // Registra el error y muestra un mensaje amigable
                System.Diagnostics.Debug.WriteLine("Error de conexión a la base de datos: " + ex.Message);
                return Content("Error de conexión a la base de datos: " + ex.Message);
            }
        }

        // Acción para simular el inicio de sesión
        public ActionResult Login2()
        {
            // Simular el inicio de sesión y establecer el ID del empleado en la sesión
            int idEmpleado = 301230123; // Supongamos que es 1
            Session["idEmpleado"] = idEmpleado;

            // Redirigir a la página de DashboardInicioEmp
            return RedirectToAction("DashboardInicioEmp");
        }


        // Acción para mostrar la vista de tipos de inconsistencias
        public ActionResult TiposInconsistencia()
        {
            List<TiposInconsistencias> tiposInconsistencias = new List<TiposInconsistencias>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosTiposInconsistencias", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TiposInconsistencias tipoInconsistencia = new TiposInconsistencias
                            {
                                idTipoInconsistencia = Convert.ToInt32(reader["idTipoInconsistencia"]),
                                nombreInconsistencia = reader["nombreInconsistencia"].ToString(),
                                descInconsistencia = reader["descInconsistencia"].ToString(),
                                estadoTipoInconsistencia = Convert.ToBoolean(reader["estadoTipoInconsistencia"]), // Corrección aquí
                                fechaCreacion = Convert.ToDateTime(reader["fechaCreacion"])
                            };

                            tiposInconsistencias.Add(tipoInconsistencia);
                        }
                    }
                }
            }

            return View(tiposInconsistencias);
        }

        // Acción para crear un nuevo tipo de inconsistencia
        [HttpPost]
        public ActionResult CrearTipoInconsistencia(TiposInconsistencias tipoInconsistencia)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CrearTipoInconsistencia", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@nombreInconsistencia", tipoInconsistencia.nombreInconsistencia);
                        cmd.Parameters.AddWithValue("@descInconsistencia", tipoInconsistencia.descInconsistencia);
                        cmd.Parameters.AddWithValue("@estadoTipoInconsistencia", tipoInconsistencia.estadoTipoInconsistencia);
                        cmd.Parameters.AddWithValue("@fechaCreacion", tipoInconsistencia.fechaCreacion);

                        SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(mensajeParam);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        TempData["Mensaje"] = mensajeParam.Value.ToString();
                    }
                }
            }

            return RedirectToAction("TiposInconsistencia");
        }

        // Acción para eliminar un tipo de inconsistencia
        [HttpPost]
        public ActionResult EliminarTipoInconsistencia(int idTipoInconsistencia)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DeleteTipoInconsistencia", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idTipoInconsistencia", idTipoInconsistencia);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                }
            }

            return RedirectToAction("TiposInconsistencia");
        }

        // Acción para activar/desactivar un tipo de inconsistencia
        [HttpPost]
        public ActionResult ActivarDesactivarTipoInconsistencia(int idTipoInconsistencia, bool nuevoEstado)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ActivarDesactivarTipoInconsistencia", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idTipoInconsistencia", idTipoInconsistencia);
                    cmd.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                }
            }

            return RedirectToAction("TiposInconsistencia");
        }


        // Acción para mostrar el historial de inconsistencias del empleado
        public ActionResult HistorialInconsistenciasEmp()
        {
            // Asumimos que el idEmpleado es 1 si no está en la sesión
            if (Session["idEmpleado"] == null)
            {
                Session["idEmpleado"] = 301230123;
            }

            int idEmpleado = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión
            List<RegistroInconsistencia> inconsistencias = new List<RegistroInconsistencia>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosLosRegistrosInconsistencias", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado); // Pasar el idEmpleado como parámetro
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RegistroInconsistencia inconsistencia = new RegistroInconsistencia
                            {
                                idInconsistencia = Convert.ToInt32(reader["idInconsistencia"]),
                                idEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                                idTipoInconsistencia = Convert.ToInt32(reader["idTipoInconsistencia"]),
                                nombreTipoInconsistencia = reader["nombreInconsistencia"].ToString(), // Asumimos que la consulta devuelve este campo
                                fechaInconsistencia = Convert.ToDateTime(reader["fechaInconsistencia"]),
                                estadoInconsistencia = Convert.ToBoolean(reader["estadoInconsistencia"]),
                                idJustificacion = reader["idJustificacion"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["idJustificacion"]),
                                estadoJustificacion = reader["estadoJustificacion"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(reader["estadoJustificacion"]),
                                Justificacion = reader["idJustificacion"] == DBNull.Value ? null : new JustificacionInconsistencia
                                {
                                    idJustificacion = Convert.ToInt32(reader["idJustificacion"]),
                                    idInconsistencia = Convert.ToInt32(reader["idInconsistencia"]),
                                    estadoJustificacion = Convert.ToBoolean(reader["estadoJustificacion"]),
                                    fechaJustificacion = reader["fechaJustificacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["fechaJustificacion"]),
                                    descripcionJustificacion = reader["descripcionJustificacion"].ToString(),
                                    adjuntoJustificacion = reader["adjuntoJustificacion"] == DBNull.Value ? null : (byte[])reader["adjuntoJustificacion"]
                                }
                            };

                            inconsistencias.Add(inconsistencia);
                        }
                    }
                }
            }

            return View(inconsistencias);
        }

        [HttpPost]
        public ActionResult JustificarInconsistenciaEmp(int idInconsistencia, string descripcionJustificacion, HttpPostedFileBase adjuntoJustificacion)
        {
            byte[] fileBytes = null;

            if (adjuntoJustificacion != null && adjuntoJustificacion.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var extension = Path.GetExtension(adjuntoJustificacion.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Mensaje"] = "Tipo de archivo no permitido. Por favor, suba un archivo PDF, JPG, JPEG, PNG, DOC o DOCX.";
                    TempData["TipoMensaje"] = "danger";
                    return RedirectToAction("HistorialInconsistenciasEmp");
                }

                using (var binaryReader = new BinaryReader(adjuntoJustificacion.InputStream))
                {
                    fileBytes = binaryReader.ReadBytes(adjuntoJustificacion.ContentLength);
                }
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_JustificarInconsistencia", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idInconsistencia", idInconsistencia);
                    cmd.Parameters.AddWithValue("@estadoJustificacion", true);
                    cmd.Parameters.AddWithValue("@fechaJustificacion", DateTime.Now);
                    cmd.Parameters.AddWithValue("@descripcionJustificacion", descripcionJustificacion ?? (object)DBNull.Value);

                    SqlParameter adjuntoParam = new SqlParameter("@adjuntoJustificacion", SqlDbType.VarBinary);
                    adjuntoParam.Value = fileBytes ?? (object)DBNull.Value;
                    cmd.Parameters.Add(adjuntoParam);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                    TempData["TipoMensaje"] = "success"; // Añadimos un indicador de tipo de mensaje
                }
            }

            return RedirectToAction("HistorialInconsistenciasEmp");
        }


        public ActionResult BandejaInconsistenciasJefe()
        {
            List<RegistroInconsistencia> inconsistencias = new List<RegistroInconsistencia>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosLosRegistrosGestiones", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RegistroInconsistencia inconsistencia = new RegistroInconsistencia
                            {
                                idInconsistencia = Convert.ToInt32(reader["idInconsistencia"]),
                                idEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                                idTipoInconsistencia = Convert.ToInt32(reader["idTipoInconsistencia"]),
                                nombreTipoInconsistencia = reader["nombreInconsistencia"].ToString(),
                                fechaInconsistencia = Convert.ToDateTime(reader["fechaInconsistencia"]),
                                estadoInconsistencia = Convert.ToBoolean(reader["estadoInconsistencia"]),
                                idJustificacion = reader["idJustificacion"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["idJustificacion"]),
                                estadoJustificacion = reader["estadoJustificacion"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(reader["estadoJustificacion"]),
                                nombreEmpleado = reader["nombreEmpleado"].ToString(), // Aquí asignamos el nombre del empleado
                                Gestion = new GestionInconsistencia
                                {
                                    idGestion = Convert.ToInt32(reader["idGestion"]),
                                    idInconsistencia = Convert.ToInt32(reader["idInconsistencia"]),
                                    fechaGestion = reader["fechaGestion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["fechaGestion"]),
                                    estadoGestion = Convert.ToBoolean(reader["estadoGestion"]),
                                    idJefe = reader["idJefe"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["idJefe"]),
                                    observacionGestion = reader["observacionGestion"].ToString()
                                },
                                Justificacion = reader["idJustificacion"] == DBNull.Value ? null : new JustificacionInconsistencia
                                {
                                    idJustificacion = Convert.ToInt32(reader["idJustificacion"]),
                                    idInconsistencia = Convert.ToInt32(reader["idInconsistencia"]),
                                    estadoJustificacion = Convert.ToBoolean(reader["estadoJustificacion"]),
                                    fechaJustificacion = reader["fechaJustificacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["fechaJustificacion"]),
                                    descripcionJustificacion = reader["descripcionJustificacion"].ToString(),
                                    adjuntoJustificacion = reader["adjuntoJustificacion"] == DBNull.Value ? null : (byte[])reader["adjuntoJustificacion"]
                                }
                            };

                            inconsistencias.Add(inconsistencia);
                        }
                    }
                }
            }

            return View(inconsistencias);
        }

        // Acción para gestionar las inconsistencias de los empleados
        [HttpPost]
        public ActionResult GestionInconsistenciasJefe(int idInconsistencia, bool estadoInconsistencia, string observacionGestion)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GestionarInconsistencia", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idInconsistencia", idInconsistencia);
                    cmd.Parameters.AddWithValue("@fechaGestion", DateTime.Now);
                    cmd.Parameters.AddWithValue("@estadoGestion", true); // Siempre será true para indicar que está gestionada
                    cmd.Parameters.AddWithValue("@idJefe", 1); // Asume que el ID del jefe se obtiene de la identidad del usuario actual
                    cmd.Parameters.AddWithValue("@observacionGestion", observacionGestion);
                    cmd.Parameters.AddWithValue("@estadoInconsistencia", estadoInconsistencia); // Agregamos el estado de inconsistencia

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                    TempData["TipoMensaje"] = estadoInconsistencia ? "success" : "danger"; // Mensaje verde si es aprobado y rojo si es rechazado
                }
            }

            return RedirectToAction("BandejaInconsistenciasJefe");
        }

        //Descargar el adjunto 
        public ActionResult DownloadAdjunto(int idJustificacion)
        {
            byte[] fileContent;
            string fileName;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT adjuntoJustificacion FROM JustificacionInconsistencia WHERE idJustificacion = @idJustificacion";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idJustificacion", idJustificacion);
                    conn.Open();

                    fileContent = cmd.ExecuteScalar() as byte[];
                }

                if (fileContent == null)
                {
                    return HttpNotFound();
                }
            }

            fileName = $"adjunto_{idJustificacion}.pdf"; // Cambia la extensión según el tipo de archivo que esperas

            return File(fileContent, "application/octet-stream", fileName);
        }

        // Acción para mostrar la vista de registro de actividades
        public ActionResult TiposActividades()
        {
            List<TiposActividades> actividades = new List<TiposActividades>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosTipoActividades", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TiposActividades actividad = new TiposActividades
                            {
                                idAct = Convert.ToInt32(reader["idAct"]),
                                nombreAct = reader["nombreAct"].ToString(),
                                descpAct = reader["descpAct"].ToString(),
                                fechaCreacion = Convert.ToDateTime(reader["fechaCreacion"]),
                                estadoAct = Convert.ToBoolean(reader["estadoAct"])
                            };

                            actividades.Add(actividad);
                        }
                    }
                }
            }

            return View(actividades);
        }

        // Acción para crear una nueva actividad
        [HttpPost]
        public ActionResult CrearActividad(TiposActividades actividad)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CrearTipoActividad", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@nombreAct", actividad.nombreAct);
                        cmd.Parameters.AddWithValue("@descpAct", actividad.descpAct);
                        cmd.Parameters.AddWithValue("@fechaCreacion", DateTime.Now);  // Fecha de creación automática
                        cmd.Parameters.AddWithValue("@estadoAct", actividad.estadoAct);

                        SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(mensajeParam);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        TempData["Mensaje"] = mensajeParam.Value.ToString();
                        TempData["MensajeTipo"] = "exito"; // Mensaje de éxito
                    }
                }
            }
            else
            {
                TempData["Mensaje"] = "Error: Datos inválidos.";
                TempData["MensajeTipo"] = "error"; // Mensaje de error
            }

            return RedirectToAction("TiposActividades");
        }

        // Acción para eliminar una actividad
        [HttpPost]
        public ActionResult EliminarActividad(int idAct)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DeleteTipoActividad", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idAct", idAct);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                    TempData["MensajeTipo"] = "exito"; // Mensaje de éxito
                }
            }

            return RedirectToAction("TiposActividades");
        }

        // Acción para activar/desactivar una actividad
        [HttpPost]
        public ActionResult ActivarDesactivarActividad(int idAct, bool nuevoEstado)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ActivarDesactivarTipoActividad", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idAct", idAct);
                    cmd.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    TempData["Mensaje"] = mensajeParam.Value.ToString();
                    TempData["MensajeTipo"] = "exito"; // Mensaje de éxito
                }
            }

            return RedirectToAction("TiposActividades");
        }


        // Acción para mostrar la vista de registro de actividades
        public ActionResult RegistrarActividadDiariaEmp()
        {
            int idEmpleado = Session["idEmpleado"] != null ? (int)Session["idEmpleado"] : 301230123;

            var viewModel = new RegistrarActividadViewModel
            {
                TiposActividades = ObtenerTiposActividades(),
                RegistroActividades = ObtenerRegistroActividades(),
                IdEmpleado = idEmpleado
            };

            return View(viewModel);
        }

        //Metodo para obtener lista de actividades por hacer
        private List<TiposActividades> ObtenerTiposActividades()
        {
            List<TiposActividades> actividades = new List<TiposActividades>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosTipoActividades", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TiposActividades actividad = new TiposActividades
                            {
                                idAct = Convert.ToInt32(reader["idAct"]),
                                nombreAct = reader["nombreAct"].ToString(),
                                descpAct = reader["descpAct"].ToString(),
                                fechaCreacion = Convert.ToDateTime(reader["fechaCreacion"]),
                                estadoAct = Convert.ToBoolean(reader["estadoAct"])
                            };

                            actividades.Add(actividad);
                        }
                    }
                }
            }

            return actividades;
        }

        // Acción para crear un nuevo registro de actividad
        [HttpPost]
        public ActionResult CrearRegistroActividad(int idAct, TimeSpan horaInicio, TimeSpan horaFinal)
        {
            int idEmp = Session["idEmpleado"] != null ? (int)Session["idEmpleado"] : 301230123;
            string mensaje = string.Empty;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CrearRegistroActividad", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idAct", idAct);
                    cmd.Parameters.AddWithValue("@idEmp", idEmp);
                    cmd.Parameters.AddWithValue("@horaInicio", horaInicio);
                    cmd.Parameters.AddWithValue("@horaFinal", horaFinal);
                    cmd.Parameters.AddWithValue("@estadoReg", 0);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    mensaje = mensajeParam.Value.ToString();
                }
            }

            TempData["Mensaje"] = mensaje; // Usar TempData para pasar el mensaje a la vista
            return RedirectToAction("RegistrarActividadDiariaEmp");
        }





        //Metodo para obtener el registro de las actividades
        private List<RegistroActividades> ObtenerRegistroActividades()
        {
            List<RegistroActividades> registroActividades = new List<RegistroActividades>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosRegistroActividades", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RegistroActividades registro = new RegistroActividades
                            {
                                idRegAct = Convert.ToInt32(reader["idRegAct"]),
                                idAct = Convert.ToInt32(reader["idAct"]),
                                idEmp = Convert.ToInt32(reader["idEmp"]),
                                fechaAct = Convert.ToDateTime(reader["fechaAct"]),
                                horaInicio = (TimeSpan)reader["horaInicio"],
                                horaFinal = (TimeSpan)reader["horaFinal"],
                                duracionAct = (TimeSpan)reader["duracionAct"],
                                estadoReg = Convert.ToBoolean(reader["estadoReg"]),
                                Actividad = new TiposActividades
                                {
                                    idAct = Convert.ToInt32(reader["idAct"]),
                                    nombreAct = reader["nombreAct"].ToString() // Asegúrate de que este campo existe en la consulta SQL
                                }
                            };

                            registroActividades.Add(registro);
                        }
                    }
                }
            }

            return registroActividades;
        }


        //Mostrar bandeja de actividades del jefe
        public ActionResult BandejaActividadesJefe()
        {
            int idJefe = Session["idEmpleado"] != null ? (int)Session["idEmpleado"] : 301240124;
            List<RegistroActividades> actividades = new List<RegistroActividades>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerRegistroActividadesJefe", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idJefe", idJefe);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RegistroActividades actividad = new RegistroActividades
                            {
                                idRegAct = Convert.ToInt32(reader["idRegAct"]),
                                idGesAct = Convert.ToInt32(reader["idGesAct"]),
                                idAct = Convert.ToInt32(reader["idAct"]),
                                idEmp = Convert.ToInt32(reader["idEmp"]),
                                nombreEmpleado = reader["nombreEmpleado"].ToString(), // Capturar el nombre del empleado
                                fechaAct = Convert.ToDateTime(reader["fechaAct"]),
                                horaInicio = (TimeSpan)reader["horaInicio"],
                                horaFinal = (TimeSpan)reader["horaFinal"],
                                duracionAct = (TimeSpan)reader["duracionAct"],
                                estadoReg = Convert.ToBoolean(reader["estadoReg"]),
                                Actividad = new TiposActividades
                                {
                                    nombreAct = reader["nombreAct"].ToString()
                                }
                            };

                            actividades.Add(actividad);
                        }
                    }
                }
            }

            return View(actividades);
        }

        //Pantalla para gestionar las actividades
        [HttpPost]
        public ActionResult GestionarActividad(int idGesAct, string obserGest, bool estadoGesAct)
        {
            int idJefe = Session["idEmpleado"] != null ? (int)Session["idEmpleado"] : 301240124; // Asegurando que el idJefe se toma de la sesión o asigna un valor por defecto
            string mensaje = string.Empty;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GestionarActividad", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idGesAct", idGesAct);
                    cmd.Parameters.AddWithValue("@fechaGesAct", DateTime.Now);
                    cmd.Parameters.AddWithValue("@obserGest", obserGest);
                    cmd.Parameters.AddWithValue("@estadoGesAct", estadoGesAct);
                    cmd.Parameters.AddWithValue("@idJefe", idJefe);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    mensaje = mensajeParam.Value.ToString();
                }
            }

            TempData["Mensaje"] = mensaje; // Usar TempData para pasar el mensaje a la vista
            return RedirectToAction("BandejaActividadesJefe");
        }




        //Pantalla para monitorear el rendimiento del empleado en vista jefe
        public ActionResult MonitoreoRendimientoJefe()
        {
            List<MonitoreoRendimiento> monitoreo = new List<MonitoreoRendimiento>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerMonitoreoRendimientoJefe", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MonitoreoRendimiento rendimiento = new MonitoreoRendimiento
                            {
                                idEmp = Convert.ToInt32(reader["idEmp"]),
                                nombre = reader["nombre"].ToString(),
                                fechaAct = Convert.ToDateTime(reader["fechaAct"]),
                                TotalActividades = Convert.ToInt32(reader["TotalActividades"]),
                                ActividadesAprobadas = Convert.ToInt32(reader["ActividadesAprobadas"]),
                                ActividadesRechazadas = Convert.ToInt32(reader["ActividadesRechazadas"]),
                                TotalHoras = Convert.ToDouble(reader["TotalHoras"]),
                                TiempoPromedioPorActividad = Convert.ToDouble(reader["TiempoPromedioPorActividad"])
                            };

                            monitoreo.Add(rendimiento);
                        }
                    }
                }
            }

            return View(monitoreo);
        }

        // Acción para mostrar el historial de horas extras
        public ActionResult HistorialHorasJefe()
        {
            List<HistorialHoras> solicitudes = ObtenerHistorialHorasEmp();
            ViewBag.Empleados = ObtenerEmpleados();
            ViewBag.Actividades = ObtenerActividades();
            return View(solicitudes);
        }

        // Acción para crear una nueva solicitud de horas extra
        [HttpPost]
        public ActionResult CrearSolicitudHoras(int idEmpleado, int idAct, decimal cantidadHoras, DateTime fechaSolicitada, string motivoSolicitud)
        {
            int idJefe = 1; // Asume que el ID del jefe es 1 para simplificar

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CrearSolicitudHoras", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@fechaSolicitud", DateTime.Now);
                    cmd.Parameters.AddWithValue("@idJefe", idJefe);
                    cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);
                    cmd.Parameters.AddWithValue("@idAct", idAct);
                    cmd.Parameters.AddWithValue("@cantidadHoras", cantidadHoras);
                    cmd.Parameters.AddWithValue("@fechaSolicitada", fechaSolicitada);
                    cmd.Parameters.AddWithValue("@motivoSolicitud", motivoSolicitud);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("HistorialHorasJefe");
        }

        // Acción para mostrar el historial de horas extras
        public ActionResult HistorialHorasEmp()
        {
            List<HistorialHoras> solicitudes = ObtenerHistorialHorasEmp();
            ViewBag.Empleados = ObtenerEmpleados();
            ViewBag.Actividades = ObtenerActividades();
            return View(solicitudes);
        }

        // Acción para aprobar una solicitud de horas extra
        [HttpPost]
        public ActionResult AprobarSolicitudHoras(int idSolicitud)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE AprobacionHoras SET estadoAproHoras = 1, fechaAproHoras = @fechaAproHoras WHERE idSolicitud = @idSolicitud", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                    cmd.Parameters.AddWithValue("@fechaAproHoras", DateTime.Now);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE SolicitudHoras SET estadoSolicitudHoras = 1 WHERE idSolicitud = @idSolicitud", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("HistorialHorasEmp");
        }

        // Acción para rechazar una solicitud de horas extra
        [HttpPost]
        public ActionResult RechazarSolicitudHoras(int idSolicitud)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE AprobacionHoras SET estadoAproHoras = 0, fechaAproHoras = @fechaAproHoras WHERE idSolicitud = @idSolicitud", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                    cmd.Parameters.AddWithValue("@fechaAproHoras", DateTime.Now);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE SolicitudHoras SET estadoSolicitudHoras = 0 WHERE idSolicitud = @idSolicitud", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("HistorialHorasEmp");
        }

        private List<HistorialHoras> ObtenerHistorialHorasEmp()
        {
            List<HistorialHoras> solicitudes = new List<HistorialHoras>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerHistorialHorasEmp", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            HistorialHoras solicitud = new HistorialHoras
                            {
                                idSolicitud = Convert.ToInt32(reader["idSolicitud"]),
                                Empleado = reader["Empleado"].ToString(),
                                Actividad = reader["Actividad"].ToString(),
                                cantidadHoras = Convert.ToDecimal(reader["cantidadHoras"]),
                                Estado = reader["Estado"].ToString(),
                                fechaSolicitada = Convert.ToDateTime(reader["fechaSolicitada"]),
                                fechaSolicitud = Convert.ToDateTime(reader["fechaSolicitud"]),
                                motivoSolicitud = reader["motivoSolicitud"].ToString()
                            };

                            solicitudes.Add(solicitud);
                        }
                    }
                }
            }

            return solicitudes;
        }

        //Metodo para obtener empleados
        private List<Empleado> ObtenerEmpleados()
        {
            List<Empleado> empleados = new List<Empleado>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT idEmpleado, nombre, apellidos FROM Empleado", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Empleado empleado = new Empleado
                            {
                                idEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                                nombre = reader["nombre"].ToString(),
                                apellidos = reader["apellidos"].ToString()
                            };

                            empleados.Add(empleado);
                        }
                    }
                }
            }

            return empleados;
        }

        //Metodo para obtener actividades
        private List<TiposActividades> ObtenerActividades()
        {
            List<TiposActividades> actividades = new List<TiposActividades>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT idAct, nombreAct FROM TiposActividades", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TiposActividades actividad = new TiposActividades
                            {
                                idAct = Convert.ToInt32(reader["idAct"]),
                                nombreAct = reader["nombreAct"].ToString()
                            };

                            actividades.Add(actividad);
                        }
                    }
                }
            }

            return actividades;
        }

        public ActionResult shared()
        {
            return View();
        }

        public ActionResult DashboardInicioAdmin()
        {
            return View();
        }

        public ActionResult DashboardInicioEmp()
        {
            return View();
        }

        public ActionResult DashboardInicioJef()
        {
            return View();
        }

        public ActionResult DashboardInicioSup()
        {
            return View();
        }


        public ActionResult SolicPermisos()
        {
            return View();
        }

        public ActionResult SolicHorasExtra()
        {
            return View();
        }

        public ActionResult ladingpage()
        {
            return View();
        }


        public ActionResult registroEmpleado()
        {
            RegistroEmpleadoViewModel viewModel = new RegistroEmpleadoViewModel
            {
                Puestos = new List<Puesto>(),
                Roles = new List<Rol>()
            };

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Cargar puestos
                    using (SqlCommand cmd = new SqlCommand("sp_LeerTodosPuestos", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Puesto puesto = new Puesto
                                {
                                    idPuesto = Convert.ToInt32(reader["idPuesto"]),
                                    nombrePuesto = reader["nombrePuesto"].ToString()
                                };

                                viewModel.Puestos.Add(puesto);
                            }
                        }
                    }

                    // Cargar roles
                    using (SqlCommand cmd = new SqlCommand("sp_LeerTodosRoles", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Rol rol = new Rol
                                {
                                    idRol = Convert.ToInt32(reader["idRol"]),
                                    nombreRol = reader["nombreRol"].ToString()
                                };

                                viewModel.Roles.Add(rol);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cargar datos: " + ex.Message);
                // Considerar un manejo de errores más robusto, como registrar en un archivo o base de datos
            }

            return View(viewModel); // Pasar el ViewModel a la vista
        }
        [HttpPost]
        public ActionResult registroEmpleado(RegistroEmpleadoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Registrar los datos que se enviarán
                    Debug.WriteLine("idEmpleado: " + model.idEmpleado);
                    Debug.WriteLine("NombreCompleto: " + model.NombreCompleto);
                    Debug.WriteLine("ApellidosCompletos: " + model.ApellidosCompletos);
                    Debug.WriteLine("CorreoElectronico: " + model.CorreoElectronico);
                    Debug.WriteLine("Telefono: " + model.Telefono);
                    Debug.WriteLine("nombrePuesto: " + model.nombrePuesto);
                    Debug.WriteLine("FechaIngreso: " + model.FechaIngreso);
                    Debug.WriteLine("nombreRol: " + model.nombreRol);

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertarEmpleado", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@idEmpleado", model.idEmpleado);
                            cmd.Parameters.AddWithValue("@nombre", model.NombreCompleto);
                            cmd.Parameters.AddWithValue("@apellidos", model.ApellidosCompletos);
                            cmd.Parameters.AddWithValue("@correo", model.CorreoElectronico);
                            cmd.Parameters.AddWithValue("@telefono", model.Telefono);
                            cmd.Parameters.AddWithValue("@nombrePuesto", model.nombrePuesto);
                            cmd.Parameters.AddWithValue("@fechaIngreso", model.FechaIngreso);
                            cmd.Parameters.AddWithValue("@nombreRol", model.nombreRol);

                            SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(mensajeParam);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                            conn.Close();

                            string mensaje = mensajeParam.Value.ToString();
                            if (mensaje.Contains("exitosamente"))
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "success"; // Tipo de alerta para mensajes de éxito
                            }
                            else
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                            }
                        }
                    }

                    return View("registroEmpleado", model);
                }
                catch (Exception ex)
                {
                    // Registrar el mensaje de la excepción y la traza de pila
                    Debug.WriteLine("Error al registrar empleado: " + ex.Message);
                    Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                    ViewBag.Mensaje = "Error al registrar empleado: " + ex.Message;
                    ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                }
            }

            return View("registroEmpleado", model);
        }


        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VerificarLogin", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetros
                        cmd.Parameters.AddWithValue("@usuario", model.Usuario);
                        cmd.Parameters.AddWithValue("@contraseña", model.Contraseña);

                        // Parámetros de salida
                        SqlParameter rolParam = new SqlParameter("@rol", SqlDbType.NVarChar, 50)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(rolParam);

                        SqlParameter estadoParam = new SqlParameter("@estadoCre", SqlDbType.Bit)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(estadoParam);

                        SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.NVarChar, 100)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(mensajeParam);

                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();

                            // Obtener los valores de salida
                            string rol = rolParam.Value.ToString();
                            bool estadoCre = (bool)estadoParam.Value;
                            string mensaje = mensajeParam.Value.ToString();

                            // Mostrar valores de salida para depuración
                            Debug.WriteLine($"Valor devuelto para @rol: {rol}");
                            Debug.WriteLine($"Valor devuelto para @estadoCre: {estadoCre}");
                            Debug.WriteLine($"Valor devuelto para @mensaje: {mensaje}");

                            if (estadoCre)
                            {
                                // Redirigir a la vista correspondiente basado en el rol
                                if (rol == "EMPLEADO")
                                {
                                    return RedirectToAction("DashboardInicioEmp");
                                }
                                else if (rol == "SUPERVISOR")
                                {
                                    return RedirectToAction("DashboardInicioSup");
                                }
                                else if (rol == "JEFE")
                                {
                                    return RedirectToAction("DashboardInicioJef");
                                }
                                // Añadir más roles si es necesario
                            }
                            else
                            {
                                // Mostrar mensaje de error
                                ViewBag.Message = mensaje;
                                ViewBag.AlertType = "danger";
                                return View(model);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Manejo de excepciones y errores
                            ViewBag.Message = "Ocurrió un error al procesar su solicitud.";
                            ViewBag.AlertType = "danger";
                            Debug.WriteLine($"Error: {ex.Message}");
                            return View(model);
                        }
                    }
                }
            }

            // Si el modelo no es válido, regresa a la vista con el modelo actual
            return View(model);
        }


        public ActionResult horarios()
        {
            List<UsuarioViewModel> usuarios = new List<UsuarioViewModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ObtenerUsuarios", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new UsuarioViewModel
                            {
                                idEmpleado = reader["idEmpleado"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Puesto = reader["Puesto"].ToString(),
                                FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"])
                            });
                        }
                    }
                }
            }

            return View(usuarios);
        }
        public JsonResult ObtenerTurnosTrabajo()
        {
            List<TurnoTrabajo> turnos = new List<TurnoTrabajo>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_ObtenerTurnosTrabajo", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TurnoTrabajo turno = new TurnoTrabajo
                    {
                        IdTurno = reader.GetInt32(reader.GetOrdinal("idTurno")),
                        TurnoDescripcion = reader.GetString(reader.GetOrdinal("TurnoDescripcion"))
                    };
                    turnos.Add(turno);
                }
            }

            return Json(turnos, JsonRequestBehavior.AllowGet);
        }

        public ActionResult registrarDepartamento()
        {
            return View();
        }




        public ActionResult turnos()
        {
            return View();
        }
        public JsonResult ObtenerDepartamentos()
        {
            List<DepartamentoViewModel> departamentos = new List<DepartamentoViewModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT nombreDep FROM Departamentos"; // Asegúrate de que el nombre de columna es correcto

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var departamento = new DepartamentoViewModel
                            {
                                nombreDep = reader["nombreDep"].ToString() // Asegúrate de que esto coincide con el nombre de columna
                            };
                            departamentos.Add(departamento);

                            // Agregar Debug.WriteLine para depuración
                            Debug.WriteLine($"Departamento: {departamento.nombreDep}");
                        }
                    }
                }
            }

            // Verificar el contenido completo de la lista para depuración
            foreach (var dep in departamentos)
            {
                Debug.WriteLine($"Departamento en la lista: {dep.nombreDep}");
            }

            return Json(departamentos, JsonRequestBehavior.AllowGet);
        }


        public ActionResult registrarPuesto()
        {
            return View();
        }

        [HttpPost]
        public ActionResult registrarPuesto(PuestoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Registrar los datos que se enviarán
                    Debug.WriteLine("NombrePuesto: " + model.NombrePuesto);
                    Debug.WriteLine("EstadoPuesto: " + model.EstadoPuesto);
                    Debug.WriteLine("SalarioHora: " + model.SalarioHora);
                    Debug.WriteLine("NombreDepartamento: " + model.NombreDepartamento);

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertarPuesto", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@nombrePuesto", model.NombrePuesto);
                            cmd.Parameters.AddWithValue("@estadoPuesto", model.EstadoPuesto);
                            cmd.Parameters.AddWithValue("@SalHora", model.SalarioHora);
                            cmd.Parameters.AddWithValue("@nombreDepartamento", model.NombreDepartamento);

                            SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 100)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(mensajeParam);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                            conn.Close();

                            string mensaje = mensajeParam.Value.ToString();
                            if (mensaje.Contains("correctamente"))
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "success"; // Tipo de alerta para mensajes de éxito
                            }
                            else
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                            }
                        }
                    }

                    // Volver a cargar los departamentos
                    //model.Departamentos = ObtenerDepartamentos();

                    return View("registrarPuesto", model);
                }
                catch (Exception ex)
                {
                    // Registrar el mensaje de la excepción y la traza de pila
                    Debug.WriteLine("Error al registrar puesto: " + ex.Message);
                    Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                    ViewBag.Mensaje = "Error al registrar puesto: " + ex.Message;
                    ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error

                    // Volver a cargar los departamentos en caso de error
                    //model.Departamentos = ObtenerDepartamentos();
                }
            }

            // Volver a cargar los departamentos en caso de que el modelo no sea válido
            //model.Departamentos = ObtenerDepartamentos();
            return View("registrarPuesto", model);
        }
        [HttpPost]
        public ActionResult registrarDepartamento(DepartamentosViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertarDepartamento", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@nombreDep", model.NombreDep);
                            cmd.Parameters.AddWithValue("@correoDep", model.CorreoDep);
                            cmd.Parameters.AddWithValue("@telefonoDep", model.TelefonoDep);
                            cmd.Parameters.AddWithValue("@estadoDep", model.EstadoDep);
                            cmd.Parameters.AddWithValue("@idJefe", model.IdJefe);

                            SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 100)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(mensajeParam);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                            conn.Close();

                            string mensaje = mensajeParam.Value.ToString();
                            if (mensaje.Contains("correctamente"))
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "success"; // Tipo de alerta para mensajes de éxito
                            }
                            else
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                            }
                        }
                    }

                    return View("registrarDepartamento", model);
                }
                catch (Exception ex)
                {
                    // Registrar el mensaje de la excepción y la traza de pila
                    Debug.WriteLine("Error al registrar departamento: " + ex.Message);
                    Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                    ViewBag.Mensaje = "Error al registrar departamento: " + ex.Message;
                    ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                }
            }

            return View("registrarDepartamento", model);
        }



    }

}