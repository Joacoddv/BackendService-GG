using Servicios.Domain.Usuario_Patente_Familia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.BLL
{
    public interface ITokenService
    {
        string CreateToken(Usuario usuario);
    }
}
