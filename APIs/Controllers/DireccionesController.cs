using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Cliente;
using DTO.Direcciones;
using DTO.Ingredientes;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DireccionesController : ControllerBase
    {

        private readonly IMapper _mapper;


        public DireccionesController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        [HttpPut("Editar")]
        public IActionResult EditarDireccion([FromBody] DireccionEdicionDTO direccionEdicionDTO)
        {
            try
            {
                direccionEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                DireccionBusinessLogic.Current.Update(_mapper.Map<Dominio.Direccion>(direccionEdicionDTO));

                return StatusCode(200, "Direccion actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Crear Cliente

        [HttpPost("Alta")]
        public IActionResult CrearDireccion([FromBody] DireccionCreacionDTO direccionCreacionDTO)
        {
            try
            {
                DireccionBusinessLogic.Current.Add(_mapper.Map<Direccion>(direccionCreacionDTO));

                return StatusCode(201, "Direccion dada de alta");
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
                var direcciones = DireccionBusinessLogic.Current.GetAll(new Direccion { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f") }).ToList();

                if (direcciones.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<DireccionToListDTO[]>(DireccionBusinessLogic.Current.GetAll(new Direccion { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f") }).ToList())));
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

        public IActionResult BuscarDireccion([FromBody] DireccionBusquedaDTO direccionBusquedaDTO)
        {
            try
            {
                switch (direccionBusquedaDTO.busquedaDireccion)
                {
                    case DTO.EBusquedaDireccion.Id:
                        return BuscarDireccionxIdDireccion(_mapper.Map<Direccion>(direccionBusquedaDTO));
                    case DTO.EBusquedaDireccion.Numero_Direccion:
                        return BuscarDireccionxNumeroExactoDireccion(_mapper.Map<Direccion>(direccionBusquedaDTO));
                    case DTO.EBusquedaDireccion.Numero_Cliente:
                        return BuscarDireccionxNumeroCliente(_mapper.Map<Direccion>(direccionBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Direccion x Id Direccion
        private IActionResult BuscarDireccionxIdDireccion(Direccion direccion)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = DireccionBusinessLogic.Current.GetOne(direccion);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<DireccionToListDTO>(result)));
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


        //Buscar Direccion x Numero Exacto Direccion
        private IActionResult BuscarDireccionxNumeroExactoDireccion(Direccion direccion)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = DireccionBusinessLogic.Current.BuscarDireccionxNumeroDireccion(direccion);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<DireccionToListDTO>(result)));
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



        //Buscar Direccion x Numero Cliente


        private IActionResult BuscarDireccionxNumeroCliente(Direccion direccion)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = DireccionBusinessLogic.Current.BuscarDireccionxNumeroCliente(direccion);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<DireccionToListDTO[]>(result)));
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
