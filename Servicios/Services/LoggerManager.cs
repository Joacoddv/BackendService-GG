using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.BLL;
using Servicios.Domain;

namespace Servicios.Services
{
    public sealed class LoggerManager
    {
        //private string filePath;

        #region Singleton
        private readonly static LoggerManager _instance = new LoggerManager();

        private BLLLogerManager bLLLogerManager = new BLLLogerManager();
        public static LoggerManager Current
        {
            get
            {
                return _instance;
            }
        }

        private LoggerManager()
        {
            //filePath = ConfigurationManager.AppSettings["filePathLogger"];
        }
        #endregion

        public void Write(string message, EventLevel eventLevel)
        {

            bLLLogerManager.InsertBitacora(new Log {Message=message, Severity = eventLevel.ToString(), Fecha = DateTime.Now, /*Usuario = ConfigurationManager.AppSettings["username"].ToString()*/ Usuario =  "Joaco" });

            //using (StreamWriter streamWriter = new StreamWriter(filePath, true))
            //{
            //    streamWriter.WriteLine($"{DateTime.Now.ToString("dd-MM-yy hh:mm:ss")} [Severity {eventLevel.ToString()}] : {message}");
            //}
        }

        public List<Log> Read()
        {
            return bLLLogerManager.GetAll().ToList();
        }
    }
}
