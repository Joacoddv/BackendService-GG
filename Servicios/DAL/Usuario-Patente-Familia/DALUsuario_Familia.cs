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

    public sealed class DALUsuario_Familia
    {
        private readonly static DALUsuario_Familia _instance = new DALUsuario_Familia();

        public static DALUsuario_Familia Current
        {
            get
            {
                return _instance;
            }
        }

        private DALUsuario_Familia()
        {
            //Implent here the initialization of your singleton
        }

        //Statements
        #region Statements
        private static string SelectAsignadas
        {
            get => "SELECT A.IdFamilia, B.Nombre " +
                "FROM [dbo].[Usuario_Familia] A " +
                "INNER JOIN [dbo].[Familia] B on B.IdFamilia = A.IdFamilia " +
                "WHERE A.IdUsuario = @IdUsuario";
        }
        private static string SelectDisponibles
        {
            get => "SELECT A.IdFamilia, A.Nombre " +
                "FROM [dbo].[Familia] A " +
                "WHERE A.IdFamilia not in (SELECT B.IdFamilia FROM Usuario_Familia B WHERE B.IdUsuario = @IdUsuario)";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Usuario_Familia] ([IdUsuario],[IdFamilia]) VALUES (@IdUsuario, @IdFamilia)";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Usuario_Familia] WHERE IdUsuario = @IdUsuario AND IdFamilia = @IdFamilia";
        }
        #endregion
        public IEnumerable<Familia> GetFamiliasAsignadas(Usuario usuario)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));

                List<Familia> familias = new List<Familia>();

                using (var dr = SqlHelper.ExecuteReader(SelectAsignadas, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Familia familia = new Familia();
                        familia.IdFamilia = Guid.Parse(values[0].ToString());
                        familia.Nombre = values[1].ToString();

                        familias.Add(familia);
                    }
                }
                return familias;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IEnumerable<Familia> GetFamiliasDisponibles(Usuario usuario)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));

                List<Familia> familias = new List<Familia>();

                using (var dr = SqlHelper.ExecuteReader(SelectDisponibles, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Familia familia = new Familia();
                        familia.IdFamilia = Guid.Parse(values[0].ToString());
                        familia.Nombre = values[1].ToString();

                        familias.Add(familia);
                    }
                }
                return familias;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void AddUsuarioFamilia(Usuario usuario,Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteUsuarioFamilia(Usuario usuario, Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }
    }
}
