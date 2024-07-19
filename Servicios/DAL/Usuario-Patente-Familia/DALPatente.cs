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


    internal sealed class DALPatente
    {
        private readonly static DALPatente _instance = new DALPatente();

        public static DALPatente Current
        {
            get
            {
                return _instance;
            }
        }

        private DALPatente()
        {
            //Implent here the initialization of your singleton
        }

        //Statements
        #region Statements
        private static string SelectAll
        {
            get => "SELECT IdPatente, Nombre FROM [dbo].[Patente]";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Patente] ([IdPatente],[Nombre]) VALUES (@IdPatente, @Nombre,@timestamp)";
        }
        private static string Update
        {
            get => "UPDATE [dbo].[Patente] SET Nombre = @Nombre WHERE IdPatente = @IdPatente";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Patente] WHERE IdPatente = @IdPatente";
        }
        private static string SelectOne
        {
            get => "SELECT IdPatente, Nombre FROM [dbo].[Patente] WHERE IdPatente = @IdPatente";
        }
        #endregion


        public Patente GetOne(Patente obj)
        {
            List<SqlParameter> p = new List<SqlParameter>();

            p.Add(new SqlParameter("@IdPatente", obj.IdPatente));

            using (var dr = SqlHelper.ExecuteReader(SelectOne, CommandType.Text,p.ToArray()))
            {
                Patente patente = new Patente();
                Object[] values = new Object[dr.FieldCount];

                while (dr.Read())
                {
                    dr.GetValues(values);

                    patente.IdPatente = Guid.Parse(values[0].ToString());
                    patente.Nombre = values[1].ToString();

                }
                return patente;
            }
        }



        public IEnumerable<Patente> GetAll()
        {
            try
            {
                List<Patente> patentes = new List<Patente>();

                using (var dr = SqlHelper.ExecuteReader(SelectAll, CommandType.Text))
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

        public void AddPatente(Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));
                p.Add(new SqlParameter("@Nombre", patente.Nombre));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void UpdatePatente(Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));
                p.Add(new SqlParameter("@Nombre", patente.Nombre));

                SqlHelper.ExecuteNonQuery(Update, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }


        public void DeletePatente(Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }
    }

}
