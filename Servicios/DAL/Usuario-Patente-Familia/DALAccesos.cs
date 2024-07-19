using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Tools;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.DAL.Usuario_Patente_Familia
{

        internal sealed class DALAccesos
        {
            private readonly static DALAccesos _instance = new DALAccesos();

            public static DALAccesos Current
            {
                get
                {
                    return _instance;
                }
            }

            private DALAccesos()
            {
                //Implent here the initialization of your singleton
            }

        //Statements
        #region Statements
        private static string SelectAccesos
        {
            get => "SELECT IdPatente, Nombre FROM [dbo].[PATENTES] WHERE UserName = @UserName";
        }
        #endregion


        public List<Patente> GetAccesos(string userName)
        {
            try
            {
                List<Patente> patentes = new List<Patente>();

                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@UserName", userName));

                using (var dr = SqlHelper.ExecuteReader(SelectAccesos, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Patente patente = new Patente();
                        patente.IdPatente = Guid.Parse(values[0].ToString());
                        patente.Nombre = values[1].ToString();

                        patentes.Add(patente);
                    }
                }
                return patentes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    }
