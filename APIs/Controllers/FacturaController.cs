using AutoMapper;
using BLL;
using Dominio;
using DTO.Factura;
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
    public class FacturaController : ControllerBase
    {

        private readonly IMapper _mapper;


        public FacturaController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        [HttpPut("Editar")]
        public IActionResult EditarFactura([FromBody] FacturaEdicionDTO facturaEdicionDTO)
        {
            try
            {
                facturaEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                FacturaBusinessLogic.Current.Update(_mapper.Map<Dominio.Factura>(facturaEdicionDTO));

                return StatusCode(200, "Factura actualizada correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpPut("ActualizarEstado")]
        public IActionResult ActuralizarEstadoPedidos([FromBody] FacturaEdicionDTO facturaEdicionDTO)
        {
            try
            {
                facturaEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                FacturaBusinessLogic.Current.ActualizarEstadopedidosFactura(_mapper.Map<Dominio.Factura>(facturaEdicionDTO));

                return StatusCode(200, "Estado de los pedidos en la factura actualizados correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Factura

        [HttpPost("Alta")]
        public IActionResult CrearFactura([FromBody] FacturaCreacionDTO facturaCreacionDTO)
        {
            try
            {
                FacturaBusinessLogic.Current.Add(_mapper.Map<Factura>(facturaCreacionDTO));

                return StatusCode(201, "Factura dada de alta");
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
                var facturas = FacturaBusinessLogic.Current.GetAll(new Factura { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("D73A9380-DA60-463F-A277-D5BC88DFA5D3") }).ToList();

                if (facturas.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(facturas)));
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

        public IActionResult BuscarFactura([FromBody] FacturaBusquedaDTO facturaBusquedaDTO)
        {
            try
            {
                switch (facturaBusquedaDTO.eBusquedaFactura)
                {
                    case DTO.EBusquedaFactura.Id:
                        return BuscarFacturaxIdFactura(_mapper.Map<Factura>(facturaBusquedaDTO));
                    case DTO.EBusquedaFactura.BuscarFacturaxNumeroFacturaExacto:
                        return BuscarFacturaxNumeroFacturaExacto(_mapper.Map<Factura>(facturaBusquedaDTO));
                    case DTO.EBusquedaFactura.BuscarFacturaxFechaFacturaExacto:
                        return BuscarFacturaxFechaFacturaExacto(_mapper.Map<Factura>(facturaBusquedaDTO));
                    case DTO.EBusquedaFactura.BuscarFacturaxCliente:
                        return BuscarFacturaxCliente(_mapper.Map<Factura>(facturaBusquedaDTO));
                    case DTO.EBusquedaFactura.BuscarFacturaxEstadoFactura:
                        return BuscarFacturaxEstadoFactura(_mapper.Map<Factura>(facturaBusquedaDTO));

                        //VALIDARRRRRRRRRRRRRRRRRRRRRRRRRRRRR
                    case DTO.EBusquedaFactura.BuscarFacturaxNumeroPedido:  
                        return BuscarFacturaxNumeroPedido(_mapper.Map<Pedido>(facturaBusquedaDTO.PedidoBusquedaDTO));
                    case DTO.EBusquedaFactura.ValidarEstadosPosibles:
                        return ValidarEstadosPosibles(_mapper.Map<Factura>(facturaBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Factura x Id Factura
        private IActionResult BuscarFacturaxIdFactura(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.GetOne(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO>(result)));
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


        //Buscar Factura x Id Numero Factura Exacto
        private IActionResult BuscarFacturaxNumeroFacturaExacto(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.BuscarFacturaxNumeroFacturaExacto(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO>(result)));
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



        //Buscar Factura x Fecha Factura
        private IActionResult BuscarFacturaxFechaFacturaExacto(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.BuscarFacturaxFechaFacturaExacto(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(result)));
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

        //Buscar Factura x Cliente
        private IActionResult BuscarFacturaxCliente(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.BuscarFacturaxCliente(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(result)));
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

        //Buscar Factura x Estado Factura
        private IActionResult BuscarFacturaxEstadoFactura(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.BuscarFacturaxEstadoFactura(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(result)));
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

        //Buscar Factura x Numero Pedido
        private IActionResult BuscarFacturaxNumeroPedido(Pedido pedido)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.BuscarFacturaxNumeroPedido(pedido);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(result)));
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



        //Buscar Factura x Numero Pedido
        private IActionResult ValidarEstadosPosibles(Factura factura)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = FacturaBusinessLogic.Current.ValidarEstadosPosibles(factura);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<FacturaToListDTO[]>(result)));
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
