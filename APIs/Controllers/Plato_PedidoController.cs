using Microsoft.AspNetCore.Mvc;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Plato_Pedido;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class Plato_PedidoController : ControllerBase
    {

        private readonly IMapper _mapper;


        public Plato_PedidoController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        //Editar Plato Pedido

        [HttpPut("Editar")]
        public IActionResult EditarPlatoPedido([FromBody] Plato_PedidoEdicionDTO plato_PedidoEdicionDTO)
        {
            try
            {
                plato_PedidoEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Plato_PedidoBusinessLogic.Current.Update(_mapper.Map<Dominio.Plato_Pedido>(plato_PedidoEdicionDTO));

                return StatusCode(200, "Plato del pedido actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Plato Pedido

        [HttpPost("Alta")]
        public IActionResult CrearPlatoPedido([FromBody] Plato_PedidoCreacionDTO plato_PedidoCreacionDTO)
        {
            try
            {
                Plato_PedidoBusinessLogic.Current.Add(_mapper.Map<Dominio.Plato_Pedido>(plato_PedidoCreacionDTO));

                return StatusCode(201, "Plato dado de alta en pedido");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Eliminar Plato Pedido

        [HttpDelete("Eliminar")]
        public IActionResult EliminarPlatoPedido([FromBody] Plato_PedidoEdicionDTO plato_PedidoEdicionDTO)
        {
            try
            {
                Plato_PedidoBusinessLogic.Current.Remove(_mapper.Map<Dominio.Plato_Pedido>(plato_PedidoEdicionDTO));

                return StatusCode(201, "Plato eliminado del pedido");
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
                var platospedido = Plato_PedidoBusinessLogic.Current.GetAll(new Plato_Pedido { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal=Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList();

                if (platospedido.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PedidoToListDTO[]>(Plato_PedidoBusinessLogic.Current.GetAll(new Plato_Pedido { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList())));
                }
                else
                {
                    return StatusCode(204, "No hay regsitros");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }





        //Gets de Busqueda
        [HttpGet("Buscar")]

        public IActionResult BuscarPlatoPedido([FromBody] Plato_PedidoBusquedaDTO plato_PedidoBusquedaDTO)
        {
            try
            {
                switch (plato_PedidoBusquedaDTO.eBusquedaPlato_Pedido) 
                {
                    case DTO.EBusquedaPlato_Pedido.Id:
                        return GetOne(_mapper.Map<Plato_Pedido>(plato_PedidoBusquedaDTO));
                    case DTO.EBusquedaPlato_Pedido.GetOnePedido:
                        return GetOnePedido(_mapper.Map<Plato_Pedido>(plato_PedidoBusquedaDTO));
                    case DTO.EBusquedaPlato_Pedido.BuscarPlatoPedidoxPedido:
                        return BuscarPlatoPedidoxPedido(_mapper.Map<Plato_Pedido>(plato_PedidoBusquedaDTO));
                    case DTO.EBusquedaPlato_Pedido.CalcularMontoPedido:
                        return CalcularMontoPedido(_mapper.Map<Plato_Pedido>(plato_PedidoBusquedaDTO));

                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar platopedido x Id platopedido
        private IActionResult GetOne(Plato_Pedido plato_pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PedidoBusinessLogic.Current.GetOne(plato_pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PedidoToListDTO>(result)));
                }
                else
                {
                    return StatusCode(204, ("No hay regsitros"));
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }



        //Buscar platopedido x Id platopedido
        private IActionResult GetOnePedido(Plato_Pedido plato_pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PedidoBusinessLogic.Current.GetOnePedido(plato_pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PedidoToListDTO[]>(result)));
                }
                else
                {
                    return StatusCode(204, ("No hay regsitros"));
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Buscar plato x pedido
        private IActionResult BuscarPlatoPedidoxPedido(Plato_Pedido plato_pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PedidoBusinessLogic.Current.BuscarPlatoPedidoxPedido(plato_pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PedidoToListDTO[]>(result)));
                }
                else
                {
                    return StatusCode(204, ("No hay regsitros"));
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Calcular Monto Pedido
        private IActionResult CalcularMontoPedido(Plato_Pedido plato_pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PedidoBusinessLogic.Current.CalcularMontoPedido(plato_pedido);
                if (result >=0)
                {
                    return Ok(JsonConvert.SerializeObject((result)));
                }
                else
                {
                    return StatusCode(204, ("No hay regsitros"));
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

    }
}
