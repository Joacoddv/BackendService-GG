using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Servicios.Services;

namespace Servicios.DAL
{
    internal sealed class DALLangugage
    {
        #region Singleton
        private readonly static DALLangugage _instance = new DALLangugage();

        private string filePath;

        public static DALLangugage Current
        {
            get
            {
                return _instance;
            }
        }

        private DALLangugage()
        {
            //Implement here the initialization code
            filePath = @"I18n\idioma.";
        }
        #endregion

        public string Traducir(string clave)
        {
            string codigoCultura = ConfigurationManager.AppSettings["Idioma"].ToString();

            string palabraTraducida = clave;

            LoggerManager.Current.Write($"Traduciendo palabra '{clave}' a idioma '{codigoCultura}'", System.Diagnostics.Tracing.EventLevel.Informational);

            using (StreamReader streamReader = new StreamReader(filePath + codigoCultura))
            {
                while (!streamReader.EndOfStream)
                {
                    string linea = streamReader.ReadLine();
                    string[] keyValuePair = linea.Split(';');

                    if (keyValuePair[0].ToLower() == clave.ToLower())
                    {
                        palabraTraducida = keyValuePair[1];
                        break;
                    }
                }
            }

            return palabraTraducida;
        }
    }
}
