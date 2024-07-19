using AutoMapper;
using BLL;
using Dominio;
using DTO.Factura;
using DTO.Factura_Pedido;
using DTO.Plato;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using System;
using System.Linq;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class Factura_PedidoController : ControllerBase
    {

        private readonly IMapper _mapper;


        public Factura_PedidoController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        [HttpPut("Editar")]
        public IActionResult EditarFactura_Pedido([FromBody] Factura_PedidoEdicionDTO factura_PedidoEdicionDTO)
        {
            try
            {
                factura_PedidoEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Factura_PedidoBusinessLogic.Current.Update(_mapper.Map<Dominio.Factura_Pedido>(factura_PedidoEdicionDTO));

                return StatusCode(200, "Plato de Factura actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Agregar Pedido a Factura

        [HttpPost("Alta")]
        public IActionResult CrearFacutra_Pedido([FromBody] Factura_PedidoCreacionDTO factura_PedidoCreacionDTO)
        {
            try
            {
                Factura_PedidoBusinessLogic.Current.Add(_mapper.Map<Factura_Pedido>(factura_PedidoCreacionDTO));

                return StatusCode(201, "Pedido dado de alta en factura");
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
                var factura_pedidos = Factura_PedidoBusinessLogic.Current.GetAll(new Factura_Pedido { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f") }).ToList();

                if (factura_pedidos.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO[]>(factura_pedidos)));
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

        public IActionResult BuscarFacturaPedido([FromBody] Factura_PedidoBusquedaDTO factura_PedidoBusquedaDTO)
        {
            try
            {
                switch (factura_PedidoBusquedaDTO.eBusquedaFactura_Pedido)
                {
                    case DTO.EBusquedaFactura_Pedido.Id:
                        return BuscarPlatoxIdFactura_Pedido(_mapper.Map<Factura_Pedido>(factura_PedidoBusquedaDTO));
                    case DTO.EBusquedaFactura_Pedido.BuscarFacturaxNumeroPedido:
                        return BuscarFacturaxNumeroPedido(_mapper.Map<Factura_Pedido>(factura_PedidoBusquedaDTO));
                    case DTO.EBusquedaFactura_Pedido.ValidarPedidoenFactura:
                        return ValidarPedidoenFactura(_mapper.Map<Factura_Pedido>(factura_PedidoBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Factura Pedido x Id Factura Pedido
        private IActionResult BuscarPlatoxIdFactura_Pedido(Factura_Pedido factura_Pedido)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Factura_PedidoBusinessLogic.Current.GetOne(factura_Pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Factura_PedidoToListDTO>(result)));
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

        //Buscar Factura Pedido x Numero Pedido
        private IActionResult BuscarFacturaxNumeroPedido(Factura_Pedido factura_Pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Factura_PedidoBusinessLogic.Current.BuscarFacturaxNumeroPedido(factura_Pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Factura_PedidoToListDTO>(result)));
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

        //Buscar Validar Pedido en Factura
        private IActionResult ValidarPedidoenFactura(Factura_Pedido factura_Pedido)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Factura_PedidoBusinessLogic.Current.ValidarPedidoenFactura(factura_Pedido);
                if (result == true)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Factura_PedidoToListDTO>(result)));
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
