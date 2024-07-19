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

    internal sealed class DALFamilia_Patente
    {
        private readonly static DALFamilia_Patente _instance = new DALFamilia_Patente();

        public static DALFamilia_Patente Current
        {
            get
            {
                return _instance;
            }
        }

        private DALFamilia_Patente()
        {
            //Implent here the initialization of your singleton
        }

        //Statements
        #region Statements
        private static string SelectAsignadas
        {
            get => "SELECT A.IdPatente, B.Nombre " +
                "FROM [dbo].[Familia_Patente] A " +
                "INNER JOIN [dbo].[Patente] B on B.IdPatente = A.IdPatente " +
                "WHERE A.IdFamilia = @IdFamilia";
        }
        private static string SelectDisponibles
        {
            get => "SELECT A.IdPatente, A.Nombre " +
                "FROM [dbo].[Patente] A " +
                "WHERE A.IdPatente not in (SELECT B.IdPatente FROM Familia_Patente B WHERE B.IdFamilia = @IdFamilia)";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Familia_Patente] ([IdFamilia],[IdPatente]) VALUES (@IdFamilia, @IdPatente)";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Familia_Patente] WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente";
        }
        #endregion

        public IEnumerable<Patente> GetPatentesAsignadasaFamilia(Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                List<Patente> patentes = new List<Patente>();

                using (var dr = SqlHelper.ExecuteReader(SelectAsignadas, CommandType.Text, p.ToArray()))
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

        public IEnumerable<Patente> GetPatentesDisponiblesenFamilia(Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                List<Patente> patentes = new List<Patente>();

                using (var dr = SqlHelper.ExecuteReader(SelectDisponibles, CommandType.Text, p.ToArray()))
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

        public void AddFamiliaPatente(Familia familia, Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteFamiliaPatente(Familia familia, Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {

            }
        }
    }

}
