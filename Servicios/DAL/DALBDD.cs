using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Services;
using Servicios.Services.Extensions;

namespace Servicios.DAL
{
    class DALBDD
    {
        readonly static string conStringSec = "Data Source=JOAQUINDIAZ\\SQLEXPRESS;Initial Catalog=GastroGestion_Seguridad;Integrated Security=True";

        //readonly static string conStringMain = ConfigurationManager.ConnectionStrings["MainConString"].ConnectionString;

        readonly static string conStringMain =  "Data Source=JOAQUINDIAZ\\SQLEXPRESS;Initial Catalog=GastroGestion;Integrated Security=True";

        private static string conString;
        public static void Backup(string path, string db)
        {
            if (db == "GastroGestion")
            {
                conString = conStringMain;
            }
            else
            {
                conString = conStringSec;
            }
            using (SqlConnection sql = new SqlConnection(conString))
            {
                try
                {
                    LoggerManager.Current.Write($"DAL BDD - Creando Backup de base de datos {db.ToString()}", EventLevel.Informational);
                    sql.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_backup", sql))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ruta", SqlDbType.VarChar).Value = path;
                        cmd.Parameters.AddWithValue("@dbase", SqlDbType.VarChar).Value = db;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    LoggerManager.Current.Write($"DAL BDD - Error al crear Backup de base de datos {db.ToString()}", EventLevel.Informational);
                    throw new Exception("Error al crear Backup de base de datos".Traducir() + $"{db.ToString()}" + $": {ex.Message}");
                }
                finally
                {
                    sql.Close();
                }
            }
        }
        public static void Restore(string path, string db)
        {
            if (db == "GastroGestion")
            {
                conString = conStringMain;
            }
            else
            {
                conString = conStringSec;
            }
            using (SqlConnection sql = new SqlConnection(conString))
            {
                try
                {
                    LoggerManager.Current.Write($"DAL BDD - Restarurando base de datos {db.ToString()}", EventLevel.Informational);
                    sql.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_restore_backup", sql))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@archivoBak", SqlDbType.VarChar).Value = path;
                        cmd.Parameters.AddWithValue("@dbase", SqlDbType.VarChar).Value = db;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    LoggerManager.Current.Write($"DAL BDD - Error restaurar base de datos {db.ToString()}", EventLevel.Informational);
                    throw new Exception("Error al restaurar base de datos".Traducir() + $"{db.ToString()}" + $": {ex.Message}");
                }
                finally
                {
                    sql.Close();
                }
            }
        }
    }
}
