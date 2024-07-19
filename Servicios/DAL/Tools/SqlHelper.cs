using Microsoft.Extensions.Configuration;
using Servicios.Services;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;

namespace Servicios.DAL.Tools
{
    internal static class SqlHelper
    {
        private readonly static string conString;

        static SqlHelper()
        {

            IConfiguration configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
              .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
              .AddJsonFile("appsettings.json")
              .Build();
            //conString = ConfigurationManager.ConnectionStrings["SecConString"].ConnectionString;
            conString = configuration.GetConnectionString("GastroGestionSeguridad");
        }

        public static Int32 ExecuteNonQuery(String commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(parameters);

                        conn.Open();
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex, "ExecuteNonQuery");
                return -1; // Puedes retornar un valor específico para indicar un error.
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, "ExecuteNonQuery");
                return -1; // Puedes retornar un valor específico para indicar un error.
            }
        }

        public static Object ExecuteScalar(String commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(parameters);

                        conn.Open();
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex, "ExecuteScalar");
                return null; // Puedes retornar un valor específico para indicar un error.
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, "ExecuteScalar");
                return null; // Puedes retornar un valor específico para indicar un error.
            }
        }

        public static SqlDataReader ExecuteReader(String commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            try
            {
                SqlConnection conn = new SqlConnection(conString);

                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.Parameters.AddRange(parameters);

                    conn.Open();
                    // When using CommandBehavior.CloseConnection, the connection will be closed when the 
                    // IDataReader is closed.
                    return cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex, "ExecuteReader");
                return null; // Puedes retornar un valor específico para indicar un error.
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, "ExecuteReader");
                return null; // Puedes retornar un valor específico para indicar un error.
            }
        }

        private static void HandleSqlException(SqlException ex, string operation)
        {
            //LoggerManager.Current.Write($"Sql Helper - Error en base de datos {operation}: {ex}", EventLevel.Error);
            // Puedes realizar acciones específicas según el tipo de error aquí.
            // Por ejemplo, loguear el error, notificar al usuario, etc.
        }

        private static void HandleGeneralException(Exception ex, string operation)
        {
            //LoggerManager.Current.Write($"Sql Helper - Otro error en {operation}: {ex}", EventLevel.Error);
            // Puedes realizar acciones específicas para manejar otros tipos de excepciones aquí.
            // Por ejemplo, loguear el error, notificar al usuario, etc.
        }
    }
}
