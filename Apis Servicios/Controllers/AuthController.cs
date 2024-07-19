using AutoMapper;
using DTO.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Servicios.BLL;
using Servicios.Domain.Usuario_Patente_Familia;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Apis_Servicios.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;

        private readonly ITokenService _tokenService;

        private readonly IMapper _mapper;

        public AuthController(IAuthRepository authRepository, ITokenService tokenService, IMapper mapper)
        {
            _authRepository = authRepository;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public string Register(UsuarioLoginDTO usuarioDto)
        {
            usuarioDto.Mail = usuarioDto.Mail.ToLower();
            if (_authRepository.ExisteUsuario(usuarioDto.Mail) is true)
            {
                return JsonConvert.SerializeObject("Ya existe un usario con ese mail");
            }
            else
            {
                var usuarioNuevo = _mapper.Map<Usuario>(usuarioDto);
                var usuarioCreado = _authRepository.Registrar(usuarioNuevo, usuarioDto.Password);
                var usuarioCreadoDTO = _mapper.Map<UsuarioToListDTO>(usuarioCreado);
                return JsonConvert.SerializeObject(usuarioCreadoDTO);
            }
        }


        [HttpPost("Login")]
        public string Login(UsuarioLoginDTO usuarioLoginDTO)
        {
            var usuarioFromRepo = _authRepository.Login(usuarioLoginDTO.Mail, usuarioLoginDTO.Password);

            if (usuarioFromRepo == null)
            {
                return JsonConvert.SerializeObject(Unauthorized());
            }
            else
            {
                var usuario = _mapper.Map<UsuarioToListDTO>(usuarioFromRepo);

                var token = _tokenService.CreateToken(usuarioFromRepo);

                return JsonConvert.SerializeObject(
                    new
                    {
                        token,
                        usuario
                    });
            }
        }

    }
}
