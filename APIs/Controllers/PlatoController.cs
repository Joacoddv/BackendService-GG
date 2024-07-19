using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Plato;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class PlatoController : ControllerBase
    {

        private readonly IMapper _mapper;


        public PlatoController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        [HttpPut("Editar")]
        public IActionResult EditarPlato([FromBody] PlatoEdicionDTO platoEdicionDTO)
        {
            try
            {
                platoEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                PlatoBusinessLogic.Current.Update(_mapper.Map<Dominio.Plato>(platoEdicionDTO));

                return StatusCode(200, "Plato actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Plato

        [HttpPost("Alta")]
        public IActionResult CrearPlato([FromBody] PlatoCreacionDTO platoCreacionDTO)
        {
            try
            {
                PlatoBusinessLogic.Current.Add(_mapper.Map<Plato>(platoCreacionDTO));

                return StatusCode(201, "Plato dado de alta");
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
                var platos = PlatoBusinessLogic.Current.GetAll(new Plato { Id_Empresa = Guid.Parse("60A4A5FA-76B2-4B1D-A961-2A1AC316F55F"),
                    Id_Sucursal = Guid.Parse("D73A9380-DA60-463F-A277-D5BC88DFA5D3") }).ToList();

                if (platos.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO[]>(platos)));
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

        public IActionResult BuscarPlato([FromBody] PlatoBusquedaDTO platoBusquedaDTO)
        {
            try
            {
                switch (platoBusquedaDTO.busquedaPlato)
                {
                    case DTO.EBusquedaPlato.Id:
                        return BuscarPlatoxIdPlato(_mapper.Map<Plato>(platoBusquedaDTO));
                    case DTO.EBusquedaPlato.Numero_Plato:
                        return BuscarPlatoxNumeroExactoPlato(_mapper.Map<Plato>(platoBusquedaDTO));
                    case DTO.EBusquedaPlato.Nombre_Plato:
                        return BuscarPlatoxNombrePlato(_mapper.Map<Plato>(platoBusquedaDTO));
                    case DTO.EBusquedaPlato.Descipcion_Plato:
                        return BuscarPlatoxDescripcionPlato(_mapper.Map<Plato>(platoBusquedaDTO));
                    case DTO.EBusquedaPlato.Plato_x_Ingrediente:
                        return BadRequest("Campo de busqueda no valido");
                    case DTO.EBusquedaPlato.PLato_x_Nombre_Plato_Exacto:
                        return BuscarPlatoxNombrePlatoExacto(_mapper.Map<Plato>(platoBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Plato x Id Plato
        private IActionResult BuscarPlatoxIdPlato(Plato plato)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = PlatoBusinessLogic.Current.GetOne(plato);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO>(result)));
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


        //Buscar Plato x Numero Exacto Plato
        private IActionResult BuscarPlatoxNumeroExactoPlato(Plato plato)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = PlatoBusinessLogic.Current.BuscarPlatoxNumeroPlato(plato);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO>(result)));
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



        //Buscar Plato x Nombre Plato


        private IActionResult BuscarPlatoxNombrePlato(Plato plato)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = PlatoBusinessLogic.Current.BuscarPlatoxNombrePlato(plato);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO[]>(result)));
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

        //Buscar Plato x Descripcion Plato


        private IActionResult BuscarPlatoxDescripcionPlato(Plato plato)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = PlatoBusinessLogic.Current.BuscarPlatoxDescripcionPlato(plato);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO[]>(result)));
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

        //Buscar Plato x Nombre plato Exacto


        private IActionResult BuscarPlatoxNombrePlatoExacto(Plato plato)
        {
            try
            {
                //plato.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //plato.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = PlatoBusinessLogic.Current.BuscarPlatoxNombrePlatoExacto(plato);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<PlatoToListDTO>(result)));
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
