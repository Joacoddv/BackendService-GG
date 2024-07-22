using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Cliente;
using BLL.Contracts;
using DTO;
using Microsoft.AspNetCore.Cors;
using DTO.Direcciones;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    //[Authorize]
    public class ClienteController : ControllerBase
    {
        private readonly IMapper _mapper;

        public ClienteController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }

        [HttpPut("Editar")]
        public IActionResult EditarCliente([FromBody] ClienteEdicionDTO clienteEdicionDTO)
        {
            try
            {
                //clienteEdicionDTO.Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293");
                ClienteBusinessLogic.Current.Update(_mapper.Map<Dominio.Cliente>(clienteEdicionDTO));

                return Ok(JsonConvert.SerializeObject("Cliente actualizado correctamente"));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Crear Cliente

        [HttpPost("Alta")]
        public IActionResult CrearCliente([FromBody] ClienteCreacionDTO clienteCreacionDTO)
        {
            try
            {
                clienteCreacionDTO.Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293");
                clienteCreacionDTO.Id_Sucursal = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293");
                ClienteBusinessLogic.Current.Add(_mapper.Map<Cliente>(clienteCreacionDTO));

                return StatusCode(200, JsonConvert.SerializeObject("Cliente dado de alta"));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Baja de Cliente

        [HttpDelete("Baja")]
        public IActionResult BajaCliente([FromBody] ClienteEdicionDTO clienteEdicionDTO)
        {
            try
            {
                clienteEdicionDTO.Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293");
                ClienteBusinessLogic.Current.Remove(_mapper.Map<Dominio.Cliente>(clienteEdicionDTO));

                return Ok(JsonConvert.SerializeObject("Cliente dado de baja correctamente"));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Getl All

        [HttpGet()]
        public IActionResult GetALL()
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.GetAll(new Cliente { Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293") });

                if (clientes.Count() > 0)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, ("No hay registros"));
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Gets de Busqueda
        [HttpGet("Buscar")]
        public IActionResult BuscarCliente([FromQuery] string campoBusqueda, [FromQuery] string valorBusqueda)
        {
            try
            {
                ClienteBusquedaDTO clienteBusquedaDTO = new ClienteBusquedaDTO
                {
                    CampoBusquedaCliente = (EBusquedaCliente)Enum.Parse(typeof(EBusquedaCliente), campoBusqueda, true),
                };
                clienteBusquedaDTO.Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293");

                switch (clienteBusquedaDTO.CampoBusquedaCliente)
                {
                    case EBusquedaCliente.Numero_Cliente:
                        clienteBusquedaDTO.Numero_Cliente = int.Parse(valorBusqueda);
                        return BuscarClientexNumeroCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
                    case EBusquedaCliente.Nombre_Cliente:
                        clienteBusquedaDTO.Nombre = valorBusqueda;
                        return BuscarClientesxNombreCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
                    case EBusquedaCliente.Apellido_Cliente:
                        clienteBusquedaDTO.Apellido = valorBusqueda;
                        return BuscarClientesxApellidoCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
                    case EBusquedaCliente.Nro_Doc_Cliente:
                        clienteBusquedaDTO.Nro_Doc = int.Parse(valorBusqueda);
                        return BuscarClientesxNroDocCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
                    case EBusquedaCliente.Estado_Cliente:
                        clienteBusquedaDTO.Estado = valorBusqueda.Equals("Activo", StringComparison.OrdinalIgnoreCase);
                        return BuscarClientesxEstadoCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
                    default:
                        return BadRequest("Campo de búsqueda no válido");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        // Buscar Cliente x Numero Cliente
        private IActionResult BuscarClientexNumeroCliente(Cliente cliente)
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNumeroCliente(cliente);
                if (clientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, "No hay registros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // Buscar Cliente x Nombre Cliente
        private IActionResult BuscarClientesxNombreCliente(Cliente cliente)
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNombreCliente(cliente);
                if (clientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, "No hay registros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // Buscar Cliente x Apellido Cliente
        private IActionResult BuscarClientesxApellidoCliente(Cliente cliente)
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.BuscarClientesxApellidoCliente(cliente);
                if (clientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, "No hay registros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // Buscar Cliente x Nro Doc Cliente
        private IActionResult BuscarClientesxNroDocCliente(Cliente cliente)
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNroDocCliente(cliente);
                if (clientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, "No hay registros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // Buscar Cliente x Estado Cliente
        private IActionResult BuscarClientesxEstadoCliente(Cliente cliente)
        {
            try
            {
                var clientes = ClienteBusinessLogic.Current.BuscarClientesxEstadoCliente(cliente);
                if (clientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
                }
                else
                {
                    return StatusCode(204, "No hay registros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
    }
}






//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using BLL;
//using Dominio;
//using System.Linq;
//using AutoMapper;
//using Newtonsoft.Json;
//using System;
//using DTO.Cliente;
//using BLL.Contracts;
//using DTO;
//using Microsoft.AspNetCore.Cors;

//namespace APIs.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [EnableCors("AllowAll")]
//    public class ClienteController : ControllerBase
//    {
//        private readonly IMapper _mapper;

//        public ClienteController(IMapper mapper)
//        {
//            _mapper = mapper;
//        }

//        private IActionResult HandleError(Exception ex)
//        {
//            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
//        }

//        [HttpPut("Editar")]
//        public IActionResult EditarCliente([FromBody] ClienteEdicionDTO clienteEdicionDTO)
//        {
//            try
//            {
//                clienteEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
//                ClienteBusinessLogic.Current.Update(_mapper.Map<Dominio.Cliente>(clienteEdicionDTO));

//                return Ok(JsonConvert.SerializeObject("Cliente actualizado correctamente"));
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        [HttpPost("Alta")]
//        public IActionResult CrearCliente([FromBody] ClienteCreacionDTO clienteCreacionDTO)
//        {
//            try
//            {
//                ClienteBusinessLogic.Current.Add(_mapper.Map<Cliente>(clienteCreacionDTO));
//                return StatusCode(200, JsonConvert.SerializeObject("Cliente dado de alta"));
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        [HttpGet()]
//        public IActionResult GetALL()
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.GetAll(new Cliente { Id_Empresa = Guid.Parse("2F678A85-B654-4464-BDDC-0C4D4CA20293") });

//                if (clientes.Count() > 0)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        [HttpGet("Buscar")]
//        public IActionResult BuscarCliente([FromQuery] string campoBusqueda, [FromQuery] string valorBusqueda)
//        {
//            ClienteBusquedaDTO clienteBusquedaDTO = new ClienteBusquedaDTO
//            {
//                CampoBusquedaCliente = (EBusquedaCliente)Enum.Parse(typeof(EBusquedaCliente), campoBusqueda, true),
//            };

//            switch (clienteBusquedaDTO.CampoBusquedaCliente)
//            {
//                case EBusquedaCliente.Numero_Cliente:
//                    clienteBusquedaDTO.Numero_Cliente = int.Parse(valorBusqueda);
//                    return BuscarClientexNumeroCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
//                case EBusquedaCliente.Nombre_Cliente:
//                    clienteBusquedaDTO.Nombre = valorBusqueda;
//                    return BuscarClientesxNombreCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
//                case EBusquedaCliente.Apellido_Cliente:
//                    clienteBusquedaDTO.Apellido = valorBusqueda;
//                    return BuscarClientesxApellidoCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
//                case EBusquedaCliente.Nro_Doc_Cliente:
//                    clienteBusquedaDTO.Nro_Doc = int.Parse(valorBusqueda);
//                    return BuscarClientesxNroDocCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
//                case EBusquedaCliente.Estado_Cliente:
//                    clienteBusquedaDTO.Estado = valorBusqueda.Equals("Activo", StringComparison.OrdinalIgnoreCase);
//                    return BuscarClientesxEstadoCliente(_mapper.Map<Cliente>(clienteBusquedaDTO));
//                default:
//                    return BadRequest("Campo de búsqueda no válido");
//            }
//        }

//        private IActionResult BuscarClientexNumeroCliente(Cliente cliente)
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNumeroCliente(cliente);
//                if (clientes != null)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        private IActionResult BuscarClientesxNombreCliente(Cliente cliente)
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNombreCliente(cliente);
//                if (clientes != null)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        private IActionResult BuscarClientesxApellidoCliente(Cliente cliente)
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.BuscarClientesxApellidoCliente(cliente);
//                if (clientes != null)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        private IActionResult BuscarClientesxNroDocCliente(Cliente cliente)
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.BuscarClientesxNroDocCliente(cliente);
//                if (clientes != null)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }

//        private IActionResult BuscarClientesxEstadoCliente(Cliente cliente)
//        {
//            try
//            {
//                var clientes = ClienteBusinessLogic.Current.BuscarClientesxEstadoCliente(cliente);
//                if (clientes != null)
//                {
//                    return Ok(JsonConvert.SerializeObject(_mapper.Map<ClienteToListDTO[]>(clientes)));
//                }
//                else
//                {
//                    return StatusCode(204, ("No hay registros"));
//                }
//            }
//            catch (Exception ex)
//            {
//                return HandleError(ex);
//            }
//        }
//    }
//}
