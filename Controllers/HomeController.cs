using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ControlSoft.Models;


namespace ControlSoft.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ControlSoftCrDbContext"].ConnectionString;


        public ActionResult InsertarMarca()
        {
            return View();
        }

        // Método para insertar una marca y generar inconsistencias
        [HttpPost]
        public ActionResult InsertarMarca(TimeSpan horaEntrada, TimeSpan horaSalida)
        {
            try
            {
                List<string> mensajesInconsistencias = new List<string>();
                string correoDestino = null;
                int idEmpleado = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Insertar la marca y obtener el ID de la marca insertada
                    int idMarca;
                    using (SqlCommand command = new SqlCommand("sp_InsertarMarca", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@idEmpleado", idEmpleado);
                        command.Parameters.AddWithValue("@horaEntrada", horaEntrada);
                        command.Parameters.AddWithValue("@horaSalida", horaSalida);

                        try
                        {
                            idMarca = Convert.ToInt32(command.ExecuteScalar());
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 50000) // Número de error personalizado en RAISERROR
                            {
                                return Json(new { success = false, message = ex.Message });
                            }
                            throw;
                        }
                    }

                    // Generar inconsistencias y obtener el correo y mensajes
                    using (SqlCommand inconsistenciaCommand = new SqlCommand("sp_GenerarInconsistencia", connection))
                    {
                        inconsistenciaCommand.CommandType = CommandType.StoredProcedure;
                        inconsistenciaCommand.Parameters.AddWithValue("@idMarca", idMarca);
                        inconsistenciaCommand.Parameters.Add("@correoDestino", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;
                        inconsistenciaCommand.Parameters.Add("@seGeneroInconsistencia", SqlDbType.Bit).Direction = ParameterDirection.Output;

                        using (SqlDataReader reader = inconsistenciaCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                mensajesInconsistencias.Add(reader["mensaje"].ToString());
                            }
                        }

                        bool seGeneroInconsistencia = Convert.ToBoolean(inconsistenciaCommand.Parameters["@seGeneroInconsistencia"].Value);
                        if (seGeneroInconsistencia)
                        {
                            correoDestino = inconsistenciaCommand.Parameters["@correoDestino"].Value.ToString();
                            // Enviar el correo de inconsistencia
                            string mensaje = string.Join("<br>", mensajesInconsistencias);
                            EnviarCorreoInconsistencia(correoDestino, mensaje);
                        }
                    }
                }

                return Json(new { success = true, message = "Marca insertada correctamente.", inconsistencias = mensajesInconsistencias });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Método para enviar correos de inconsistencias
        private void EnviarCorreoInconsistencia(string correoDestino, string mensaje)
        {
            try
            {
                var fromAddress = new MailAddress("kreuzdev@gmail.com", "Equipo ControlSoftCR");
                var toAddress = new MailAddress(correoDestino);
                const string fromPassword = "kgqr aeqr rosm eydn"; // Asegúrate de que esta contraseña es correcta
                const string subject = "Notificación de Inconsistencia";

                string body = $@"
                <html>
                <body>
                    <h2>Hola,</h2>
                    <p>{mensaje}</p>
                    <br>
                    <p>Saludos,</p>
                    <p>El equipo de ControlSoftCr</p>
                </body>
                </html>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Registrar el mensaje de la excepción y la traza de pila
                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }


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



        // Bandeja Inconsistencias supervisor
        public ActionResult BandejaInconsistenciasJefe()
        {
            int idEmpleado = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

            List<RegistroInconsistencia> inconsistencias = new List<RegistroInconsistencia>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosLosRegistrosGestiones", conn))
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
                    TempData["MensajeTipo"] = mensajeParam.Value.ToString() == "Actividad eliminada exitosamente." ? "exito" : "error";
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
            int idEmpleado = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

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

            int idEmp = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

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
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

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
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión
                                                                 // 
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


        // Pantalla para monitorear el rendimiento del empleado en vista jefe
        public ActionResult MonitoreoRendimientoJefe()
        {
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

            List<MonitoreoRendimiento> monitoreo = new List<MonitoreoRendimiento>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerMonitoreoRendimientoJefe", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idJefe", idJefe); // Pasar el idJefe como parámetro
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
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

            List<HistorialHoras> solicitudes = new List<HistorialHoras>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerHistorialHorasEmp", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idJefe", idJefe); // Pasar el idJefe como parámetro
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
                                fechaSolicitud = Convert.ToDateTime(reader["fechaSolicitud"]),
                                fechaSolicitada = Convert.ToDateTime(reader["fechaSolicitada"]),
                                motivoSolicitud = reader["motivoSolicitud"].ToString()
                            };

                            solicitudes.Add(solicitud);
                        }
                    }
                }
            }

            ViewBag.Empleados = ObtenerEmpleados();
            ViewBag.Actividades = ObtenerActividades();
            return View(solicitudes);
        }


        //Metodo para obtener empleados
        private List<Empleado> ObtenerEmpleados()
        {
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

            List<Empleado> empleados = new List<Empleado>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("ObtenerEmpleadosSesion", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idJefe", idJefe);
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

        // Acción para crear una nueva solicitud de horas extra
        [HttpPost]
        public ActionResult CrearSolicitudHoras(int idEmpleado, int idAct, decimal cantidadHoras, DateTime fechaSolicitada, string motivoSolicitud)
        {
            int idJefe = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

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

        //Metodo para leer las solicitudes de horas extra que se han realizado al empleado que inicia sesion.
        private List<HistorialHoras> ObtenerHistorialHorasEmpSolicitadas()
        {
            int idEmpleado = Convert.ToInt32(Session["idEmpleado"]); // Obtener el idEmpleado de la sesión

            List<HistorialHoras> solicitudes = new List<HistorialHoras>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_LeerHistorialHorasEmpSolicitadas", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado); // Añadir parámetro para idEmpleado

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string estadoSolicitudHoras = reader["estadoSolicitudHoras"] == DBNull.Value ? "Pendiente" :
                                (Convert.ToBoolean(reader["estadoSolicitudHoras"]) ? "Aprobada" : "Rechazada");

                            HistorialHoras solicitud = new HistorialHoras
                            {
                                idSolicitud = Convert.ToInt32(reader["idSolicitud"]),
                                Empleado = reader["Empleado"].ToString(),
                                Actividad = reader["Actividad"].ToString(),
                                cantidadHoras = Convert.ToDecimal(reader["cantidadHoras"]),
                                Estado = estadoSolicitudHoras,
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

        // Acción para mostrar el historial de horas extras
        public ActionResult HistorialHorasEmp()
        {
            List<HistorialHoras> solicitudes = ObtenerHistorialHorasEmpSolicitadas();
            ViewBag.Empleados = ObtenerEmpleados();
            ViewBag.Actividades = ObtenerActividades();
            return View(solicitudes);
        }

        // Acción para aprobar o rechazar una solicitud de horas extra
        [HttpPost]
        public ActionResult AprobarRechazarSolicitudHoras(int idSolicitud, bool aprobar)
        {
            string mensaje = string.Empty;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_AprobarRechazarHoras", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                    cmd.Parameters.AddWithValue("@estadoSolicitudHoras", aprobar); // Aprobado/Rechazado
                    cmd.Parameters.Add("@mensaje", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    mensaje = cmd.Parameters["@mensaje"].Value.ToString();
                }
            }

            TempData["Mensaje"] = mensaje;
            TempData["TipoMensaje"] = "success"; // Siempre éxito

            return RedirectToAction("HistorialHorasEmp");
        }

        public ActionResult EdicionComponentes()
        {
            return View();
        }


        public ActionResult shared()
        {
            return View();
        }

        public ActionResult DashboardInicioAdmin()
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

        /*public ActionResult registroEmpleado()
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
                conn.Open();

                // Insertar el empleado
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

                    cmd.ExecuteNonQuery();

                    string mensaje = mensajeParam.Value.ToString();
                    Debug.WriteLine("Mensaje del procedimiento almacenado: " + mensaje);

                    if (!mensaje.Contains("exitosamente"))
                    {
                        ViewBag.Mensaje = mensaje;
                        ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                        return View("registroEmpleado", model);
                    }

                    ViewBag.Mensaje = mensaje;
                    ViewBag.AlertType = "success"; // Tipo de alerta para mensajes de éxito
                }

                // Buscar las credenciales del usuario después de insertar el empleado
                Debug.WriteLine("Buscando credenciales del usuario...");
                string queryCredenciales = "SELECT usuario, contraseña FROM Credenciales WHERE idEmp = @idEmp";
                SqlCommand cmdCredenciales = new SqlCommand(queryCredenciales, conn);
                cmdCredenciales.Parameters.AddWithValue("@idEmp", model.idEmpleado);

                string usuario = null;
                string contrasena = null;

                using (SqlDataReader reader = cmdCredenciales.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = reader["usuario"].ToString();
                        contrasena = reader["contraseña"].ToString();
                    }
                }

                Debug.WriteLine("Usuario: " + usuario);
                Debug.WriteLine("Contraseña: " + contrasena);

                if (usuario != null && contrasena != null)
                {
                    // Enviar correo electrónico con las credenciales al correo del usuario
                    EnviarCorreo(model.CorreoElectronico, usuario, contrasena);
                }
                else
                {
                    ViewBag.Mensaje = "Empleado registrado, pero no se encontraron credenciales para la cédula ingresada.";
                    ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                }

                conn.Close();
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

private void EnviarCorreo(string correoDestino, string usuario, string contrasena)
{
    try
    {
        Debug.WriteLine("Iniciando el envío de correo...");
        Debug.WriteLine("Correo destino: " + correoDestino);
        Debug.WriteLine("Usuario: " + usuario);
        Debug.WriteLine("Contraseña: " + contrasena);

        var fromAddress = new System.Net.Mail.MailAddress("kreuzdev@gmail.com", "Equipo ControlSoftCR");
        var toAddress = new System.Net.Mail.MailAddress(correoDestino);
        const string fromPassword = "kgqr aeqr rosm eydn"; // Asegúrate de que esta contraseña es correcta
        const string subject = "Credenciales de Acceso";

        string body = $@"
    <html>
    <body>
        <h2>Hola,</h2>
        <p>Tus credenciales de acceso son:</p>
        <p><strong>Usuario:</strong> {usuario}</p>
        <p><strong>Contraseña:</strong> {contrasena}</p>
        <br>
        <p>Saludos,</p>
        <p>El equipo de ControlSoftCr</p>
    </body>
    </html>";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true // Esto indica que el cuerpo del mensaje es HTML
        })
        {
            smtp.Send(message);
        }

        Debug.WriteLine("Correo enviado exitosamente.");
    }
    catch (Exception ex)
    {
        // Registrar el mensaje de la excepción y la traza de pila
        Debug.WriteLine("Error al enviar correo: " + ex.Message);
        Debug.WriteLine("Stack Trace: " + ex.StackTrace);
    }
}
*/


        public ActionResult registroEmpleado()
        {
            var viewModel = ObtenerRegistroEmpleadoViewModel();
            return View(viewModel);
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
                        conn.Open();

                        // Insertar el empleado
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

                            cmd.ExecuteNonQuery();

                            string mensaje = mensajeParam.Value.ToString();
                            Debug.WriteLine("Mensaje del procedimiento almacenado: " + mensaje);

                            if (!mensaje.Contains("exitosamente"))
                            {
                                ViewBag.Mensaje = mensaje;
                                ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                                model = ObtenerRegistroEmpleadoViewModel(); // Recargar datos de Puestos y Roles
                                return View("registroEmpleado", model);
                            }

                            ViewBag.Mensaje = mensaje;
                            ViewBag.AlertType = "success"; // Tipo de alerta para mensajes de éxito
                        }

                        // Buscar las credenciales del usuario después de insertar el empleado
                        Debug.WriteLine("Buscando credenciales del usuario...");
                        string queryCredenciales = "SELECT usuario, contraseña FROM Credenciales WHERE idEmp = @idEmp";
                        SqlCommand cmdCredenciales = new SqlCommand(queryCredenciales, conn);
                        cmdCredenciales.Parameters.AddWithValue("@idEmp", model.idEmpleado);

                        string usuario = null;
                        string contrasena = null;

                        using (SqlDataReader reader = cmdCredenciales.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                usuario = reader["usuario"].ToString();
                                contrasena = reader["contraseña"].ToString();
                            }
                        }

                        Debug.WriteLine("Usuario: " + usuario);
                        Debug.WriteLine("Contraseña: " + contrasena);

                        if (usuario != null && contrasena != null)
                        {
                            // Enviar correo electrónico con las credenciales al correo del usuario
                            EnviarCorreo(model.CorreoElectronico, usuario, contrasena);
                        }
                        else
                        {
                            ViewBag.Mensaje = "Empleado registrado, pero no se encontraron credenciales para la cédula ingresada.";
                            ViewBag.AlertType = "danger"; // Tipo de alerta para mensajes de error
                        }

                        conn.Close();
                    }

                    model = ObtenerRegistroEmpleadoViewModel(); // Recargar datos de Puestos y Roles
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

            model = ObtenerRegistroEmpleadoViewModel(); // Recargar datos de Puestos y Roles
            return View("registroEmpleado", model);
        }

        private RegistroEmpleadoViewModel ObtenerRegistroEmpleadoViewModel()
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

            return viewModel;
        }

        private void EnviarCorreo(string correoDestino, string usuario, string contrasena)
        {
            try
            {
                Debug.WriteLine("Iniciando el envío de correo...");
                Debug.WriteLine("Correo destino: " + correoDestino);
                Debug.WriteLine("Usuario: " + usuario);
                Debug.WriteLine("Contraseña: " + contrasena);

                var fromAddress = new System.Net.Mail.MailAddress("kreuzdev@gmail.com", "Equipo ControlSoftCR");
                var toAddress = new System.Net.Mail.MailAddress(correoDestino);
                const string fromPassword = "kgqr aeqr rosm eydn"; // Asegúrate de que esta contraseña es correcta
                const string subject = "Credenciales de Acceso";

                string body = $@"
    <html>
    <body>
        <h2>Hola,</h2>
        <p>Tus credenciales de acceso son:</p>
        <p><strong>Usuario:</strong> {usuario}</p>
        <p><strong>Contraseña:</strong> {contrasena}</p>
        <br>
        <p>Saludos,</p>
        <p>El equipo de ControlSoftCr</p>
    </body>
    </html>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // Esto indica que el cuerpo del mensaje es HTML
                })
                {
                    smtp.Send(message);
                }

                Debug.WriteLine("Correo enviado exitosamente.");
            }
            catch (Exception ex)
            {
                // Registrar el mensaje de la excepción y la traza de pila
                Debug.WriteLine("Error al enviar correo: " + ex.Message);
                Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
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
                                Session["idEmpleado"] = model.Usuario;
                                Session["usuario"] = model.Usuario;
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




        [HttpPost]
        public ActionResult RegistrarHorarios(HorarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        foreach (var horario in model.Horarios)
                        {
                            using (SqlCommand command = new SqlCommand("sp_InsertarHorarioConFecha", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                command.Parameters.AddWithValue("@idEmpleado", model.IdEmpleado);
                                command.Parameters.AddWithValue("@fecha", horario.Fecha);

                                if (horario.IdTurno.HasValue)
                                {
                                    command.Parameters.AddWithValue("@idTurno", horario.IdTurno.Value);
                                }
                                else
                                {
                                    command.Parameters.AddWithValue("@idTurno", DBNull.Value);
                                }

                                if (!string.IsNullOrEmpty(horario.NombreTurno))
                                {
                                    command.Parameters.AddWithValue("@nombreTurno", horario.NombreTurno);
                                }
                                else
                                {
                                    command.Parameters.AddWithValue("@nombreTurno", DBNull.Value);
                                }

                                command.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 1000).Direction = ParameterDirection.Output;

                                command.ExecuteNonQuery();

                                string mensaje = (string)command.Parameters["@Mensaje"].Value;
                                if (!string.IsNullOrEmpty(mensaje) && mensaje.Contains("Error"))
                                {
                                    ModelState.AddModelError("", mensaje);
                                    return View("Error");
                                }
                            }
                        }
                    }

                    return RedirectToAction("horarios");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocurrió un error al registrar los horarios: " + ex.Message);
                    return View("Error");  // Asegúrate de tener una vista de error configurada
                }
            }

            return View(model);
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

        [HttpPost]
        public ActionResult registrarTurnoTrabajo(TurnoViewModel turnoTrabajo)
        {
            string mensaje;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertarTurnoTrabajo", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nombreTurno", turnoTrabajo.NombreTurno);
                    cmd.Parameters.AddWithValue("@horaInicio", turnoTrabajo.HoraInicio);
                    cmd.Parameters.AddWithValue("@horaFin", turnoTrabajo.HoraFin);
                    cmd.Parameters.AddWithValue("@estadoTurno", turnoTrabajo.EstadoTurno);

                    SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000);
                    mensajeParam.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(mensajeParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    mensaje = mensajeParam.Value.ToString();
                }
            }

            ViewBag.Mensaje = mensaje;
            ViewBag.AlertType = mensaje.Contains("exitosamente") ? "success" : "danger";

            return View("registrarTurnoTrabajo", turnoTrabajo); // Ajusta esto al nombre de tu vista
        }

        public ActionResult registrarTurnoTrabajo()
        {
            return View();
        }

        public ActionResult filtraEmpleadoPuesto()
        {
            return View();
        }









        /*public ActionResult FiltrarEmpleadosPorPuesto(string puesto)
        {

            List<EmpleadoViewModel> empleados = new List<EmpleadoViewModel>();

            string connectionString = ConfigurationManager.ConnectionStrings["TuConnectionString"].l;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("sp_ObtenerEmpleadosPorPuesto", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Puesto", (object)puesto ?? DBNull.Value);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    EmpleadoViewModel empleado = new EmpleadoViewModel
                    {
                        IdEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                        NombreCompleto = reader["NombreCompleto"].ToString(),
                        Puesto = reader["Puesto"].ToString(),
                        FechaIngreso = Convert.ToDateTime(reader["fechaIngreso"])
                    };
                    empleados.Add(empleado);
                }
            }

            return Json(empleados, JsonRequestBehavior.AllowGet);
        }*/

        public ActionResult DashboardInicioEmp()
        {
            // Obtener el nombre de usuario de la sesión
            string usuario = Session["Usuario"]?.ToString();

            // Mostrar el nombre de usuario en la consola de depuración
            Debug.WriteLine($"Usuario en sesión: {usuario}");

            if (string.IsNullOrEmpty(usuario))
            {
                // Si no hay usuario en sesión, redirigir al login
                return RedirectToAction("Login");
            }

            // Puedes pasar el usuario o cualquier dato adicional a la vista si es necesario
            ViewBag.Usuario = usuario;

            return View();
        }



        public ActionResult perfilEmpleado()
        {
            // Obtener el ID del empleado de la sesión
            string usuario = Session["Usuario"]?.ToString();

            // Mostrar el ID del usuario en la consola de depuración
            Debug.WriteLine($"ID del usuario en sesión PERFIL USUARIO: {usuario}");

            if (string.IsNullOrEmpty(usuario))
            {
                // Si no hay usuario en sesión, redirigir al login
                return RedirectToAction("Login");
            }

            PerfilViewModel empleado = new PerfilViewModel();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ObtenerDatosEmpleado", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usuario", usuario);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Asignar los datos recuperados al modelo
                            empleado.IdEmpleado = reader["idEmpleado"].ToString();
                            empleado.Nombre = reader["Nombre"].ToString();
                            empleado.Apellidos = reader["Apellidos"].ToString();
                            empleado.Correo = reader["Correo"].ToString();
                            empleado.Telefono = reader["Telefono"].ToString();
                            empleado.FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]);
                            empleado.NombrePuesto = reader["nombrePuesto"].ToString();
                            empleado.Contraseña = reader["Contraseña"].ToString(); // Manejar con cuidado

                            // Mostrar los datos en la consola de depuración
                            Debug.WriteLine($"Nombre: {empleado.IdEmpleado}");
                            Debug.WriteLine($"Nombre: {empleado.Nombre}");
                            Debug.WriteLine($"Apellidos: {empleado.Apellidos}");
                            Debug.WriteLine($"Correo: {empleado.Correo}");
                            Debug.WriteLine($"Teléfono: {empleado.Telefono}");
                            Debug.WriteLine($"Fecha de Ingreso: {empleado.FechaIngreso}");
                            Debug.WriteLine($"Puesto: {empleado.NombrePuesto}");
                            Debug.WriteLine($"Contraseña: {empleado.Contraseña}"); // Manejar con cuidado
                        }
                    }
                }
            }

            return View(empleado);
        }
        [HttpPost]

        public ActionResult ModificarContraseña(ActualizarContraseñaViewModel model)
        {
            string mensaje;
            bool exito = false;

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conexion = new SqlConnection(connectionString))
                    {
                        using (SqlCommand comando = new SqlCommand("sp_ActualizarContraseña", conexion))
                        {
                            comando.CommandType = CommandType.StoredProcedure;
                            comando.Parameters.AddWithValue("@IdEmpleado", model.IdEmpleado);
                            comando.Parameters.AddWithValue("@NuevaContraseña", model.NuevaContraseña);

                            SqlParameter parametroMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 1000)
                            {
                                Direction = ParameterDirection.Output
                            };
                            comando.Parameters.Add(parametroMensaje);

                            conexion.Open();
                            comando.ExecuteNonQuery();
                            conexion.Close();

                            mensaje = parametroMensaje.Value.ToString();
                            exito = mensaje.StartsWith("Contraseña actualizada con éxito");
                        }
                    }
                }
                catch (Exception ex)
                {
                    mensaje = "Error al actualizar la contraseña: " + ex.Message;
                }
            }
            else
            {
                mensaje = "Datos inválidos. Por favor, revise la información.";
            }

            TempData["AlertType"] = exito ? "success" : "danger";
            TempData["Mensaje"] = mensaje;

            return RedirectToAction("PerfilEmpleado");
        }

        /*public ActionResult gestionarEmpleados()
        {
            List<GEmpleadoViewModel> usuarios = new List<GEmpleadoViewModel>();



            Debug.WriteLine("Iniciando ObtenerUsuarios");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                Debug.WriteLine("Creada la conexión a la base de datos");

                using (SqlCommand cmd = new SqlCommand("sp_ObtenerEmpleadosGes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    Debug.WriteLine("Conexión abierta a la base de datos");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Debug.WriteLine("Ejecutado el procedimiento almacenado");

                        if (reader.HasRows)
                        {
                            Debug.WriteLine("El lector de datos tiene filas");

                            while (reader.Read())
                            {
                                // Imprimir los parámetros recibidos en la consola para depuración
                                Debug.WriteLine("ID Empleado: " + reader["idEmpleado"]);
                                Debug.WriteLine("Nombre Completo: " + reader["NombreCompleto"]);
                                Debug.WriteLine("Apellidos: " + reader["ApellidosCompletos"]);
                                Debug.WriteLine("Correo Electrónico: " + reader["CorreoElectronico"]);
                                Debug.WriteLine("Teléfono: " + reader["Telefono"]);
                                Debug.WriteLine("Puesto: " + reader["Puesto"]);
                                Debug.WriteLine("Rol: " + reader["Rol"]);
                                Debug.WriteLine("Fecha de Ingreso: " + reader["FechaIngreso"]);

                                usuarios.Add(new GEmpleadoViewModel
                                {
                                    idEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                                    NombreCompleto = reader["NombreCompleto"].ToString(),
                                    ApellidosCompletos = reader["ApellidosCompletos"].ToString(),
                                    CorreoElectronico = reader["CorreoElectronico"].ToString(),
                                    Telefono = reader["Telefono"].ToString(),
                                    Puesto = reader["Puesto"].ToString(),
                                    Rol = reader["Rol"].ToString(),
                                    FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"])
                                });
                            }
                        }
                        else
                        {
                            Debug.WriteLine("El lector de datos no tiene filas");
                        }
                    }
                }
            }

            // Verifica si la lista está vacía o null antes de pasarla a la vista
            if (usuarios == null || !usuarios.Any())
            {
                Debug.WriteLine("No se encontraron empleados");
                return HttpNotFound("No se encontraron empleados.");
            }

            Debug.WriteLine("Devolviendo la vista gestionarEmpleados con usuarios");
            // Devuelve la vista 'gestionarEmpleados' con el modelo 'usuarios'
            return View("gestionarEmpleados", usuarios);
        }*/




        public ActionResult gestionarEmpleados()
        {
            List<GEmpleadoViewModel> usuarios = new List<GEmpleadoViewModel>();
            List<Puesto> puestos = new List<Puesto>();
            List<Rol> roles = new List<Rol>();

            Debug.WriteLine("Iniciando ObtenerUsuarios");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                Debug.WriteLine("Creada la conexión a la base de datos");

                // Obtener empleados
                using (SqlCommand cmd = new SqlCommand("sp_ObtenerEmpleadosGes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    Debug.WriteLine("Conexión abierta a la base de datos");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Debug.WriteLine("Ejecutado el procedimiento almacenado");

                        if (reader.HasRows)
                        {
                            Debug.WriteLine("El lector de datos tiene filas");

                            while (reader.Read())
                            {
                                // Imprimir los parámetros recibidos en la consola para depuración
                                Debug.WriteLine("ID Empleado: " + reader["idEmpleado"]);
                                Debug.WriteLine("Nombre Completo: " + reader["NombreCompleto"]);
                                Debug.WriteLine("Apellidos: " + reader["ApellidosCompletos"]);
                                Debug.WriteLine("Correo Electrónico: " + reader["CorreoElectronico"]);
                                Debug.WriteLine("Teléfono: " + reader["Telefono"]);
                                Debug.WriteLine("Puesto: " + reader["Puesto"]);
                                Debug.WriteLine("Rol: " + reader["Rol"]);
                                Debug.WriteLine("Fecha de Ingreso: " + reader["FechaIngreso"]);

                                usuarios.Add(new GEmpleadoViewModel
                                {
                                    idEmpleado = Convert.ToInt32(reader["idEmpleado"]),
                                    NombreCompleto = reader["NombreCompleto"].ToString(),
                                    ApellidosCompletos = reader["ApellidosCompletos"].ToString(),
                                    CorreoElectronico = reader["CorreoElectronico"].ToString(),
                                    Telefono = reader["Telefono"].ToString(),
                                    Puesto = reader["Puesto"].ToString(),
                                    Rol = reader["Rol"].ToString(),
                                    FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"])
                                });
                            }
                        }
                        else
                        {
                            Debug.WriteLine("El lector de datos no tiene filas");
                        }
                    }
                }

                // Obtener puestos
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosPuestos", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Debug.WriteLine("Ejecutado el procedimiento almacenado para puestos");

                        if (reader.HasRows)
                        {
                            Debug.WriteLine("El lector de datos tiene filas para puestos");

                            while (reader.Read())
                            {
                                Puesto puesto = new Puesto
                                {
                                    idPuesto = Convert.ToInt32(reader["idPuesto"]),
                                    nombrePuesto = reader["nombrePuesto"].ToString()
                                };

                                puestos.Add(puesto);
                            }
                        }
                    }
                }

                // Obtener roles
                using (SqlCommand cmd = new SqlCommand("sp_LeerTodosRoles", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Debug.WriteLine("Ejecutado el procedimiento almacenado para roles");

                        if (reader.HasRows)
                        {
                            Debug.WriteLine("El lector de datos tiene filas para roles");

                            while (reader.Read())
                            {
                                Rol rol = new Rol
                                {
                                    idRol = Convert.ToInt32(reader["idRol"]),
                                    nombreRol = reader["nombreRol"].ToString()
                                };

                                roles.Add(rol);
                            }
                        }
                    }
                }
            }

            // Verifica si la lista está vacía o null antes de pasarla a la vista
            if (usuarios == null || !usuarios.Any())
            {
                Debug.WriteLine("No se encontraron empleados");
                return HttpNotFound("No se encontraron empleados.");
            }

            Debug.WriteLine("Devolviendo la vista gestionarEmpleados con usuarios");
            // Pasar los datos adicionales a la vista
            ViewBag.Puestos = puestos;
            ViewBag.Roles = roles;

            // Devuelve la vista 'gestionarEmpleados' con el modelo 'usuarios'
            return View("gestionarEmpleados", usuarios);
        }



        [HttpPost]
        public ActionResult ActualizarEmpleado(GEmpleadoViewModel model)
        {
            string mensaje;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ActualizarEmpleado", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@idEmpleado", model.idEmpleado);
                    cmd.Parameters.AddWithValue("@NombreCompleto", model.NombreCompleto);
                    cmd.Parameters.AddWithValue("@ApellidosCompletos", model.ApellidosCompletos);
                    cmd.Parameters.AddWithValue("@CorreoElectronico", model.CorreoElectronico);
                    cmd.Parameters.AddWithValue("@Telefono", model.Telefono);
                    cmd.Parameters.AddWithValue("@NombrePuesto", model.Puesto);
                    cmd.Parameters.AddWithValue("@NombreRol", model.Rol);
                    cmd.Parameters.AddWithValue("@FechaIngreso", model.FechaIngreso);

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

            if (mensaje == "Actualización exitosa.")
            {
                // Enviar correo de notificación
                EnviarCorreoDatosActualizado(model.CorreoElectronico, model.NombreCompleto, model.ApellidosCompletos, model.Telefono, model.Puesto, model.Rol, model.FechaIngreso);
            }

            ViewBag.Mensaje = mensaje;
            return View("gestionarEmpleados");
        }
        private void EnviarCorreoDatosActualizado(string correoDestino, string nombreCompleto, string apellidosCompletos, string telefono, string nombrePuesto, string nombreRol, DateTime fechaIngreso)
        {
            try
            {
                Debug.WriteLine("Iniciando el envío de correo...");
                Debug.WriteLine("Correo destino: " + correoDestino);

                var fromAddress = new System.Net.Mail.MailAddress("kreuzdev@gmail.com", "Equipo ControlSoftCR");
                var toAddress = new System.Net.Mail.MailAddress(correoDestino);
                const string fromPassword = "kgqr aeqr rosm eydn"; // Asegúrate de que esta contraseña es correcta
                const string subject = "Actualización de Información";

                // Construir el cuerpo del correo
                string body = $@"
<html>
<body>
    <h2>Hola, {nombreCompleto},</h2>
    <p>Tu información ha sido actualizada exitosamente en el sistema.</p>
    <p><strong>Nombre Completo:</strong> {nombreCompleto}</p>    
    <p><strong>Apellidos:</strong> {apellidosCompletos}</p>
    <p><strong>Teléfono:</strong> {telefono}</p>
    <p><strong>Puesto:</strong> {nombrePuesto}</p>
    <p><strong>Rol:</strong> {nombreRol}</p>
    <p><strong>Fecha de Ingreso:</strong> {fechaIngreso.ToString("dd/MM/yyyy")}</p>
    <br>
    <p>Saludos,</p>
    <p>El equipo de ControlSoftCr</p>
</body>
</html>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // Esto indica que el cuerpo del mensaje es HTML
                })
                {
                    smtp.Send(message);
                }

                Debug.WriteLine("Correo enviado exitosamente.");
            }
            catch (Exception ex)
            {
                // Registrar el mensaje de la excepción y la traza de pila
                Debug.WriteLine("Error al enviar correo: " + ex.Message);
                Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }




    }

}