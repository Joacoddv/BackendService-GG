using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AutoMapper;
using BLL;
using DTO;
using DTO.Ingredientes;
using DTO.Usuarios;
using Dominio;
using DLL.Repositories.SqlServer.Adapters;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class IngredienteController : ControllerBase
    {

        private readonly IMapper _mapper;


        public IngredienteController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }

        //Editar Ingrediente

        [HttpPut("Editar")]
        public IActionResult EditarIngrediente([FromBody] IngredienteEdicionDTO ingredienteEdicionDTO)
        {
            try
            {
                IngredienteBusinessLogic.Current.Update(_mapper.Map<Ingrediente>(ingredienteEdicionDTO));

                //IngredienteBusinessLogic.Current.Add(new Ingrediente { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3"), Numero_ingrediente = Convert.ToInt32(numero_igrediente), Nombre_Ingrediente = nombre_ingrediente, Descripcion = "", Medida = "", Estado = true });

                return StatusCode(200, "Ingrediente actualizado correctamente");
            }
            catch (Exception ex)
            {
                return      HandleError(ex);
            }
        }


        //Crear Ingrediente

        [HttpPost("Alta")]
        public IActionResult CrearIngrediente([FromBody] IngredienteCreacionDTO ingredienteCreacionDTO)
        {
            try
            {
                IngredienteBusinessLogic.Current.Add(_mapper.Map<Ingrediente>(ingredienteCreacionDTO));

                //IngredienteBusinessLogic.Current.Add(new Ingrediente { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3"), Numero_ingrediente = Convert.ToInt32(numero_igrediente), Nombre_Ingrediente = nombre_ingrediente, Descripcion = "", Medida = "", Estado = true });

                return StatusCode(200, "Ingrediente dado de alta");
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
                var ingredientes = IngredienteBusinessLogic.Current.GetAll(new Ingrediente { Id_Empresa = Guid.Parse("60A4A5FA-76B2-4B1D-A961-2A1AC316F55F") }).ToList();
                if (ingredientes.Count() > 0)
                {
                    //return JsonConvert.SerializeObject(IngredienteBusinessLogic.Current.GetAll(new Ingrediente { Id_Empresa = Guid.Parse("60A4A5FA-76B2-4B1D-A961-2A1AC316F55F") }));

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO[]>(ingredientes)));
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
        //[HttpGet("Search")]
        //public string SearchIngrediente([FromQuery] IngredienteBusquedaDTO ingredienteBusquedaDTO)
        //{
        //    switch (ingredienteBusquedaDTO.CampoBusquedaIngrediente)
        //    {
        //        case Dominio.EBusquedaIngrediente.Id:
        //            return BuscarIngredientexIdIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

        //        case Dominio.EBusquedaIngrediente.Numero_Ingrediente:
        //            return BuscarIngredientePorNumeroIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

        //        case Dominio.EBusquedaIngrediente.Nombre_Ingrediente:
        //            return BuscarIngredientexNombreIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

        //        case Dominio.EBusquedaIngrediente.Nombre_exacto_Ingrediente:
        //            return BuscarUnIngredientexNombreIngredienteExacto(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

        //        case Dominio.EBusquedaIngrediente.Descripcion_Ingrediente:
        //            return BuscarIngredientexDescrpicionIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

        //        case Dominio.EBusquedaIngrediente.Medida_Ingrediente:
        //            return BuscarIngredientexMedidaIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));
        //        default:
        //            return JsonConvert.SerializeObject("Campo de busqueda no valido");
        //    }
        //}



        //Gets de Busqueda
        [HttpGet("Buscar")]

        public IActionResult BuscarIngrediente([FromBody] IngredienteBusquedaDTO ingredienteBusquedaDTO)
        {
            switch (ingredienteBusquedaDTO.CampoBusquedaIngrediente)
            {
                case DTO.EBusquedaIngrediente.Id:
                    return BuscarIngredientexIdIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

                case DTO.EBusquedaIngrediente.Numero_Ingrediente:
                    return BuscarIngredientePorNumeroIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

                case DTO.EBusquedaIngrediente.Nombre_Ingrediente:
                    return BuscarIngredientexNombreIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

                case DTO.EBusquedaIngrediente.Nombre_exacto_Ingrediente:
                    return BuscarUnIngredientexNombreIngredienteExacto(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

                case DTO.EBusquedaIngrediente.Descripcion_Ingrediente:
                    return BuscarIngredientexDescrpicionIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));

                case DTO.EBusquedaIngrediente.Medida_Ingrediente:
                    return BuscarIngredientexMedidaIngrediente(_mapper.Map<Ingrediente>(ingredienteBusquedaDTO));
                default:
                    return BadRequest("Campo de busqueda no valido");
            }
        }

        //Buscar Ingrediente x IdIngrediente
        private IActionResult BuscarIngredientexIdIngrediente(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.GetOne(ingrediente);
                if (ingredientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO>(ingredientes)));
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

        // Buscar Ingrediente por Numero de Ingrediente

        private IActionResult BuscarIngredientePorNumeroIngrediente(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.BuscarIngredientexNumeroIngrediente(ingrediente);
                if (ingredientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO>(ingredientes)));
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


        //Buscar Ingrediente por Nombre de Ingrediente
        private IActionResult BuscarIngredientexNombreIngrediente(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.BuscarIngredientexNombreIngrediente(ingrediente);
                if (ingredientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO[]>(ingredientes)));
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

        //Buscar Ingrediente por Nombre exacto de Ingrediente
        private IActionResult BuscarUnIngredientexNombreIngredienteExacto(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.BuscarUnIngredientexNombreIngredienteExacto(ingrediente);
                if (ingredientes != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO>(ingredientes)));
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

        //Buscar Ungrediente por Descirpcion
        private IActionResult BuscarIngredientexDescrpicionIngrediente(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.BuscarIngredientexDescrpicionIngrediente(ingrediente);
                if (ingredientes.Count > 0)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO[]>(ingredientes)));
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


        //Buscar Ingrediente por Medida
        private IActionResult BuscarIngredientexMedidaIngrediente(Ingrediente ingrediente)
        {
            try
            {
                ingrediente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                ingrediente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var ingredientes = IngredienteBusinessLogic.Current.BuscarIngredientexMedidaIngrediente(ingrediente);
                if (ingredientes.Count > 0)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<IngredienteToListDTO[]>(ingredientes)));
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