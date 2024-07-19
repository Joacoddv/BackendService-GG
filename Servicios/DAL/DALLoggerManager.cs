using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Adapters;
using Servicios.DAL.Tools;
using Servicios.Domain;
using Servicios.Services;

namespace Servicios.DAL
{

    public sealed class DALLoggerManager
    {
        private readonly static DALLoggerManager _instance = new DALLoggerManager();

        public static DALLoggerManager Current
        {
            get
            {
                return _instance;
            }
        }

        private DALLoggerManager()
        {
            //Implent here the initialization of your singleton
        }


        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[Bitacora] (Mensaje,Fecha,Usuario,Severidad) VALUES (@Mensaje,@Fecha,@Usuario,@Severidad)";
        }

        private string SelectAllStatement
        {
            get => "Select Mensaje,Fecha,Usuario,Severidad from [dbo].[Bitacora]";
        }

        public void Insert(Log obj)
        {
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Mensaje", obj.Message),
                                              new SqlParameter("@Fecha", obj.Fecha),
                                              new SqlParameter("@Usuario", obj.Usuario),
                                              new SqlParameter("@Severidad", obj.Severity)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al insertar Log: {ex.Message}", EventLevel.Error);
            }
        }

        public IEnumerable<Log> GetAll()
        {
            List<Log> logs = new List<Log>();
            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] { }))
                {
                    while (dr.Read())
                    {
                        Log log = new Log();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        log = LogAdapter.Current.Adapt(values);
                        logs.Add(log);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al listar Logs: {ex.Message}", EventLevel.Error);
            }
            return logs;
        }
    }
}
