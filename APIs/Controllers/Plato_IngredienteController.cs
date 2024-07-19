using Microsoft.AspNetCore.Mvc;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Ingredientes;
using DTO.Plato_Ingrediente;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class Plato_IngredienteController : ControllerBase
    {

        private readonly IMapper _mapper;


        public Plato_IngredienteController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }



        //Eliminar Plato Ingrediente

        [HttpDelete("Eliminar")]
        public IActionResult EliminarPlatoIngrediente([FromBody] Plato_IngredienteEdicionDTO plato_IngredienteEdicionDTO)
        {
            try
            {
                plato_IngredienteEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Plato_IngredienteBusinessLogic.Current.Remove(_mapper.Map<Dominio.Plato_Ingrediente>(plato_IngredienteEdicionDTO));

                return StatusCode(200, "Ingrediente del plato elminado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Editar Plato Ingrediente

        [HttpPut("Editar")]
        public IActionResult EditarPlatoIngrediente([FromBody] Plato_IngredienteEdicionDTO plato_IngredienteEdicionDTO)
        {
            try
            {
                plato_IngredienteEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                Plato_IngredienteBusinessLogic.Current.Update(_mapper.Map<Dominio.Plato_Ingrediente>(plato_IngredienteEdicionDTO));

                return StatusCode(200, "Ingrediente del plato actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Plato Ingrediente

        [HttpPost("Alta")]
        public IActionResult CrearPlatoIngrediente([FromBody] Plato_IngredienteCreacionDTO plato_IngredienteCreacionDTO)
        {
            try
            {
                Plato_IngredienteBusinessLogic.Current.Add(_mapper.Map<Plato_Ingrediente>(plato_IngredienteCreacionDTO));

                return StatusCode(201, "Ingrediente dado de alta en plato");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        //Getl All

        [HttpGet()]
        public IActionResult GetALL(Plato_IngredienteBusquedaDTO plato_IngredienteBusquedaDTO)
        {
            try
            {
                //   var platosingredientes = Plato_IngredienteBusinessLogic.Current.GetAll(new Plato_Ingrediente { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f") }).ToList();
                var platosingredientes = Plato_IngredienteBusinessLogic.Current.GetAll(_mapper.Map<Plato_Ingrediente>(plato_IngredienteBusquedaDTO)).ToList();
                if (platosingredientes.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_IngredienteToListDTO[]>(Plato_IngredienteBusinessLogic.Current.GetAll(_mapper.Map<Plato_Ingrediente>(plato_IngredienteBusquedaDTO)).ToList())));
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

        public IActionResult BuscarDireccion([FromBody] Plato_IngredienteBusquedaDTO plato_IngredienteBusquedaDTO)
        {
            try
            {
                switch (plato_IngredienteBusquedaDTO.eBusquedaPlato_Ingrediente)
                {
                    case DTO.EBusquedaPlato_Ingrediente.Id:
                        return BuscarPlatoingredientexId(_mapper.Map<Plato_Ingrediente>(plato_IngredienteBusquedaDTO));
                    case DTO.EBusquedaPlato_Ingrediente.Ingredientes_x_Plato:
                        return BuscarPlatoIngredientexPlato(_mapper.Map<Plato_Ingrediente>(plato_IngredienteBusquedaDTO));
                    case DTO.EBusquedaPlato_Ingrediente.PlatoxIngrediente:
                        return BuscarPlatoIngredientexIngrediente(_mapper.Map<Plato_Ingrediente>(plato_IngredienteBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar PlatoIngrediente x Id Plato
        private IActionResult BuscarPlatoIngredientexPlato(Plato_Ingrediente plato_Ingrediente)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_IngredienteBusinessLogic.Current.PlatoIngredientexPlato(plato_Ingrediente);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_IngredienteToListDTO[]>(result)));
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


        //Buscar plato Ingrediente x Ingrediente
        private IActionResult BuscarPlatoIngredientexIngrediente(Plato_Ingrediente plato_Ingrediente)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_IngredienteBusinessLogic.Current.PlatoIngredientexIngrediente(plato_Ingrediente);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_IngredienteToListDTO[]>(result)));
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



        //Buscar PlatoIngrediente x Id Plato Ingrediente


        private IActionResult BuscarPlatoingredientexId(Plato_Ingrediente plato_Ingrediente)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = Plato_IngredienteBusinessLogic.Current.GetOne(plato_Ingrediente);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<Plato_IngredienteToListDTO>(result)));
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
