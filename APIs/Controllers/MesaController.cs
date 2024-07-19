using AutoMapper;
using BLL;
using Dominio;
using DTO.Mesa;
using DTO.Plato_Pedido;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MesaController : ControllerBase
    {

        private readonly IMapper _mapper;


        public MesaController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        //Editar Mesa

        [HttpPut("Editar")]
        public IActionResult EditarMesa([FromBody] MesaEdicionDTO mesaEdicionDTO)
        {
            try
            {
                mesaEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                MesaBusinessLogic.Current.Update(_mapper.Map<Dominio.Mesa>(mesaEdicionDTO));

                return StatusCode(200, "Mesa actualizada correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Eliminar Mesa

        [HttpDelete("Eliminar")]
        public IActionResult EliminarMesa([FromBody] MesaEdicionDTO mesaEdicionDTO)
        {
            try
            {
                mesaEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                MesaBusinessLogic.Current.Remove(_mapper.Map<Dominio.Mesa>(mesaEdicionDTO));

                return StatusCode(200, "Mesa eliminada correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Mesa

        [HttpPost("Alta")]
        public IActionResult CrearMesa([FromBody] MesaCreacionDTO mesaCreacionDTO)
        {
            try
            {
                MesaBusinessLogic.Current.Add(_mapper.Map<Mesa>(mesaCreacionDTO));

                return StatusCode(201, "Mesa dada de alta");
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
                var mesas = MesaBusinessLogic.Current.GetAll(new Mesa { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal =Guid.Parse("D73A9380-DA60-463F-A277-D5BC88DFA5D3") }).ToList();

                if (mesas.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MesaToListDTO[]>(MesaBusinessLogic.Current.GetAll(new Mesa { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("D73A9380-DA60-463F-A277-D5BC88DFA5D3") }).ToList())));
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

        public IActionResult BuscarMesa([FromBody] MesaBusquedaDTO mesaBusquedaDTO)
        {
            try
            {
                switch (mesaBusquedaDTO.EBusquedaMesa)
                {
                    case DTO.EBusquedaMesa.Id:
                        return GetOne(_mapper.Map<Mesa>(mesaBusquedaDTO));
                    case DTO.EBusquedaMesa.NumeroExactoMesa:
                        return BuscarMesaxNumeroExactoMesa(_mapper.Map<Mesa>(mesaBusquedaDTO));
                    case DTO.EBusquedaMesa.NumeroMesa:
                        return BuscarMesaxNumeroMesa(_mapper.Map<Mesa>(mesaBusquedaDTO));
                    case DTO.EBusquedaMesa.Capacidad:
                        return BuscarMesaxCapacidad(_mapper.Map<Mesa>(mesaBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Mesa x Id Mesa
        private IActionResult GetOne(Mesa mesa)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MesaBusinessLogic.Current.GetOne(mesa);
                if (result != null && result.Numero_Mesa != 0)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MesaToListDTO>(result)));
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



        //Buscar Mesa x Numero Mesa
        private IActionResult BuscarMesaxNumeroMesa(Mesa mesa)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MesaBusinessLogic.Current.BuscarMesaxNumeroMesa(mesa);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MesaToListDTO[]>(result)));
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


        //Buscar Mesa x Numero Exacto Mesa
        private IActionResult BuscarMesaxNumeroExactoMesa(Mesa mesa)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MesaBusinessLogic.Current.BuscarMesaxNumeroMesaExacto(mesa);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MesaToListDTO>(result)));
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

        //Buscar Mesa x Capacidad
        private IActionResult BuscarMesaxCapacidad(Mesa mesa)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MesaBusinessLogic.Current.BuscarMesaxCapacidad(mesa);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MesaToListDTO[]>(result)));
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