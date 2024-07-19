using AutoMapper;
using BLL;
using Dominio;
using DTO.Ingredientes;
using DTO.Pedido;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace APIs.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class PedidoController : ControllerBase
    {

        private readonly IMapper _mapper;


        public PedidoController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }

        //Editar Pedido

        [HttpPut("Editar")]
        public IActionResult EditarPedido([FromBody] PedidoEdicionDTO pedidoEdicionDTO)
        {
            try
            {
                PedidoBusinessLogic.Current.Update(_mapper.Map<Pedido>(pedidoEdicionDTO));

                //IngredienteBusinessLogic.Current.Add(new Ingrediente { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3"), Numero_ingrediente = Convert.ToInt32(numero_igrediente), Nombre_Ingrediente = nombre_ingrediente, Descripcion = "", Medida = "", Estado = true });

                return StatusCode(200, "Pedido actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Pedido

        [HttpPost("Alta")]
        public IActionResult CrearPedido([FromBody] PedidoCreacionDTO pedidoCreacionDTO)
        {
            try
            {
                PedidoBusinessLogic.Current.Add(_mapper.Map<Dominio.Pedido>(pedidoCreacionDTO));

                //IngredienteBusinessLogic.Current.Add(new Ingrediente { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3"), Numero_ingrediente = Convert.ToInt32(numero_igrediente), Nombre_Ingrediente = nombre_ingrediente, Descripcion = "", Medida = "", Estado = true });

                return StatusCode(200, "Pedido dado de alta");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Get All

        [HttpGet()]
        public IActionResult GetALL()
        {
            try
            {
                var pedidos = PedidoBusinessLogic.Current.GetAll(new Pedido { Id_Empresa = Guid.Parse("60A4A5FA-76B2-4B1D-A961-2A1AC316F55F"), Id_Sucursal = Guid.Parse("D73A9380-DA60-463F-A277-D5BC88DFA5D3") }).ToList();
                if (pedidos.Count() > 0)
                {
                    //return JsonConvert.SerializeObject(IngredienteBusinessLogic.Current.GetAll(new Ingrediente { Id_Empresa = Guid.Parse("60A4A5FA-76B2-4B1D-A961-2A1AC316F55F") }));

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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



        //Gets de Busqueda
        [HttpGet("Buscar")]

        public IActionResult BuscarPedido([FromBody] PedidoBusquedaDTO pedidoBusquedaDTO)
        {
            switch (pedidoBusquedaDTO.BusquedaPedido) 
            {
                case DTO.EBusquedaPedido.Id:
                    return BuscarPedidoxIdPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxNumeroPedido:
                    return BuscarPedidoxNumeroPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidoxNumeroPedidoExacto:
                    return BuscarPedidoxNumeroPedidoExacto(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosDisponiblesparaFacturarxxIdCliente:
                    return BuscarPedidosDisponiblesparaFacturarxxIdCliente(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxCliente:
                    return BuscarPedidosxCliente(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxDireccion:
                    return BuscarPedidosxDirecicon(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxMesa:
                    return BuscarPedidosxMesa(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxEstadoPedido:
                    return BuscarPedidosxEstadoPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxEstadoFacturaPedido:
                    return BuscarPedidosxEstadoFacturaPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxFechaPedido:
                    return BuscarPedidosxFechaPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.BuscarPedidosxFechaEntregaPedido:
                    return BuscarPedidosxFechaEntregaPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                //case DTO.EBusquedaPedido.BuscarPedidosxFechaEntregaPedidoSinElse:
                //    return BuscarPedidosxFechaEntregaPedidoSinElse(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.HidratarPedido:
                    return HidratarPedido(_mapper.Map<Pedido>(pedidoBusquedaDTO));
                case DTO.EBusquedaPedido.ValidarEstadosPosibles:
                    return ValidarEstadosPosibles(_mapper.Map<Pedido>(pedidoBusquedaDTO));

                default:
                    return BadRequest("Campo de busqueda no valido");
            }
        }

        //Buscar Pedido x Id Pedido
        private IActionResult BuscarPedidoxIdPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.GetOne(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO>(pedidos)));
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



        //Buscar Pedido x Numero Pedido
        private IActionResult BuscarPedidoxNumeroPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxNumeroPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map <PedidoToListDTO[]>(pedidos)));
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


        //Buscar Pedido x Numero Pedido Exacto
        private IActionResult BuscarPedidoxNumeroPedidoExacto(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO>(pedidos)));
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



        //Buscar Pedido x Id Cliente
        private IActionResult BuscarPedidosxCliente(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxCliente(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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

        //Buscar Pedido x Id Direccion
        private IActionResult BuscarPedidosxDirecicon(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxDireccion(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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


        //Buscar Pedido x Id Mesa
        private IActionResult BuscarPedidosxMesa(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxMesa(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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

        //Buscar Pedido x Estado Pedido
        private IActionResult BuscarPedidosxEstadoPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxEstadoPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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


        //Buscar Pedido x Estado Factura-Pedido
        private IActionResult BuscarPedidosxEstadoFacturaPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxEstadoFacturaPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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

        //Buscar Pedido x Fecha Pedido
        private IActionResult BuscarPedidosxFechaPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxFechaPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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


        //Buscar Pedido x Cliente para facturar
        private IActionResult BuscarPedidosDisponiblesparaFacturarxxIdCliente(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosDisponiblesparaFacturarxxIdCliente(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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


        //Buscar Pedido x Fecha Entrega Pedido
        private IActionResult BuscarPedidosxFechaEntregaPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxFechaEntregaPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
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

        ////Buscar Pedido x Fecha Entrega Pedido Sin Else
        //private IActionResult BuscarPedidosxFechaEntregaPedidoSinElse(Pedido pedido)
        //{
        //    try
        //    {
        //        pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
        //        pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
        //        var pedidos = PedidoBusinessLogic.Current.BuscarPedidosxFechaEntregaPedidoSinElse(pedido);
        //        if (pedidos >=0)
        //        {
        //            return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO[]>(pedidos)));
        //        }
        //        else
        //        {
        //            return StatusCode(204, ("No hay regsitros"));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return HandleError(ex);
        //    }
        //}



        //Hidratar Pedido
        private IActionResult HidratarPedido(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var pedidos = PedidoBusinessLogic.Current.HidratarPedido(pedido);
                if (pedidos != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PedidoToListDTO>(pedidos)));
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



        //Validar Estados Posibles
        private IActionResult ValidarEstadosPosibles(Pedido pedido)
        {
            try
            {
                pedido.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                pedido.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var estadosposibles = PedidoBusinessLogic.Current.ValidarEstadosPosibles(pedido);
                if (estadosposibles != null)
                {
                    return Ok(JsonConvert.SerializeObject(estadosposibles));
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
