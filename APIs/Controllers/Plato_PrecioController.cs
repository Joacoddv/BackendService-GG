using AutoMapper;
using BLL;
using Dominio;
using DTO.Menu;
using DTO.Mesa;
using DTO.Plato_Precio;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using System;
using System.Linq;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class Plato_PrecioController : ControllerBase
    {

        private readonly IMapper _mapper;


        public Plato_PrecioController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        //Editar Plato Precio

        [HttpPut("Editar")]
        public IActionResult EditarPlatoPrecio([FromBody] Plato_PrecioEdicionDTO plato_PrecioEdicionDTO)
        {
            try
            {
                plato_PrecioEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Plato_PrecioBusinessLogic.Current.Update(_mapper.Map<Dominio.Plato_Precio>(plato_PrecioEdicionDTO));

                return StatusCode(200, "Precio del pato actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Eliminar Plato Precio
        [HttpDelete("Eliminar")]
        public IActionResult EliminarPlato_Precio([FromBody] Plato_PrecioEdicionDTO plato_PrecioEdicionDTO)
        {
            try
            {
                plato_PrecioEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Plato_PrecioBusinessLogic.Current.Remove(_mapper.Map<Dominio.Plato_Precio>(plato_PrecioEdicionDTO));

                return StatusCode(200, "Precio del pato eliminado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Plato Precio

        [HttpPost("Alta")]
        public IActionResult CrearMesa([FromBody] Plato_PrecioCreacionDTO plato_PrecioCreacionDTO)
        {
            try
            {
                Plato_PrecioBusinessLogic.Current.Add(_mapper.Map<Plato_Precio>(plato_PrecioCreacionDTO));

                return StatusCode(201, "Precio dado de alta");
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
                var plato_Precios = Plato_PrecioBusinessLogic.Current.GetAll(new Plato_Precio { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList();

                if (plato_Precios.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PrecioToListoDTO[]>(Plato_PrecioBusinessLogic.Current.GetAll(new Plato_Precio { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList())));
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

        public IActionResult BuscarPlatoPrecio([FromBody] Plato_PrecioBusquedaDTO plato_PrecioBusquedaDTO)
        {
            try
            {
                switch (plato_PrecioBusquedaDTO.EBusquedaPlatoPrecio)
                {
                    case DTO.EBusquedaPlatoPrecio.Id:
                        return GetOne(_mapper.Map<Plato_Precio>(plato_PrecioBusquedaDTO));
                    case DTO.EBusquedaPlatoPrecio.BuscarPrecioPorPlatoYFecha:
                        return BuscarPrecioPorPlatoYFecha(_mapper.Map<Plato_Precio>(plato_PrecioBusquedaDTO), plato_PrecioBusquedaDTO.Fecha_Precio);
                    case DTO.EBusquedaPlatoPrecio.BuscarPlatoPrecioXPlato:
                        return BuscarPlatoPrecioXPlato(_mapper.Map<Plato_Precio>(plato_PrecioBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Plato Precio x Id Plato Precio
        private IActionResult GetOne(Plato_Precio plato_Precio)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PrecioBusinessLogic.Current.GetOne(plato_Precio);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PrecioToListoDTO>(result)));
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



        //Buscar Plato Precio x Plato y Fecha
        private IActionResult BuscarPrecioPorPlatoYFecha(Plato_Precio plato_precio, DateTime Fecha_Plato)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PrecioBusinessLogic.Current.BuscarPrecioPorPlatoYFecha(plato_precio,Fecha_Plato);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PrecioToListoDTO>(result)));
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


        //Buscar Plato Precio x Plato
        private IActionResult BuscarPlatoPrecioXPlato(Plato_Precio plato_Precio)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_PrecioBusinessLogic.Current.BuscarPlatoPrecioXPlato(plato_Precio);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_PrecioToListoDTO[]>(result)));
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