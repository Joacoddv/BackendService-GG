using Org.BouncyCastle.Math.EC.Rfc7748;
using Servicios.DAL.Usuario_Patente_Familia;
using Servicios.Domain.Usuario_Patente_Familia;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.BLL
{
    public class AuthRepository : IAuthRepository
    {
        
        public AuthRepository()
        {
            
        }

        public bool ExisteUsuario(string mail)
        {
            List<Usuario> usuarios = DALUsuario.Current.GetAll().ToList();
            if (usuarios.Any(x => x.Mail == mail))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public  Usuario Login(string mail, string password)
        {
            List<Usuario> usuarios = DALUsuario.Current.GetAll().ToList();
            Usuario usuario = usuarios.FirstOrDefault(x => x.Mail.ToUpper() == mail.ToUpper());
            if(usuario == null)
            {
                return null;
            }
            if(!VerifyPasswordHash(password,usuario.PasswordHash,usuario.PasswordSalt))
            {
                return null;
            }
            return usuario;
        }

        public Usuario Registrar(Usuario usuario, string password)
        {
            byte[] passwordhash;
            byte[] passwordsalt;
            CreatePasswordHash(password, out passwordhash, out passwordsalt);
            usuario.PasswordHash= passwordhash;
            usuario.PasswordSalt = passwordsalt;
            usuario.UserId = Guid.NewGuid();
            DALUsuario.Current.AddUser(usuario);

            return usuario;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordhash, byte[] passwordsalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordsalt))
            {
                var computedhash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i=0; i<computedhash.Length; i++)
                {
                    if (computedhash[i] != passwordhash[i]) return false;
                }
            }
            return true;
        }



        private void CreatePasswordHash(string password, out byte[] passwordhash, out byte[] passwordsalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordsalt = hmac.Key;
                passwordhash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

    }
}
