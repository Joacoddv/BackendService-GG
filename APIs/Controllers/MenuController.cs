using Microsoft.AspNetCore.Mvc;
using BLL;
using Dominio;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using System;
using DTO.Ingredientes;
using DTO.Plato_Ingrediente;
using DTO.Menu;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MenuController : ControllerBase
    {

        private readonly IMapper _mapper;


        public MenuController(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }


        //Editar Menu

        [HttpPut("Editar")]
        public IActionResult EditarMenu([FromBody] MenuEdicionDTO menuEdicionDTO)
        {
            try
            {
                menuEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                MenuBusinessLogic.Current.Update(_mapper.Map<Dominio.Menu>(menuEdicionDTO));

                return StatusCode(200, "Menu actualizado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }


        //Eliminar Menu
        [HttpDelete("Eliminar")]
        public IActionResult EliminarMenu([FromBody] MenuEdicionDTO menuEdicionDTO)
        {
            try
            {
                menuEdicionDTO.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                MenuBusinessLogic.Current.Remove(_mapper.Map<Dominio.Menu>(menuEdicionDTO));

                return StatusCode(200, "Menu Eliminado correctamente");
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }



        //Crear Menu

        [HttpPost("Alta")]
        public IActionResult CrearMenu([FromBody] MenuCreacionDTO menuCreacionDTO)
        {
            try
            {
                MenuBusinessLogic.Current.Add(_mapper.Map<Menu>(menuCreacionDTO));

                return StatusCode(201, "Menu dado de alta");
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
                var menu = MenuBusinessLogic.Current.GetAll(new Menu { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList();

                if (menu.Count() > 0)
                {

                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MenuToListDTO[]>(MenuBusinessLogic.Current.GetAll(new Menu { Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f"), Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3") }).ToList())));
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

        public IActionResult BuscarMenu([FromBody] MenuBusquedaDTO menuBusquedaDTO)
        {
            try
            {
                switch (menuBusquedaDTO.busquedaMenu)
                {
                    case DTO.EBusquedaMenu.Id:
                        return BuscarMenuxId(_mapper.Map<Menu>(menuBusquedaDTO));
                    case DTO.EBusquedaMenu.MenuxNumeroMenu:
                        return BuscarMenuxNumeroMenu(_mapper.Map<Menu>(menuBusquedaDTO));
                    case DTO.EBusquedaMenu.MenuxFecha:
                        return BuscarMenuxFecha(_mapper.Map<Menu>(menuBusquedaDTO));
                    case DTO.EBusquedaMenu.MenuxPlato:
                        return BuscarMenuxPlato(_mapper.Map<Menu>(menuBusquedaDTO));
                    case DTO.EBusquedaMenu.BuscarPreioMenuxPlatoyFecha:
                        return BuscarPrecioMenuxFechayPlato(_mapper.Map<Menu>(menuBusquedaDTO));
                    default:
                        return BadRequest("Campo de busqueda no valido");
                }
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }
        //Buscar Menu x Id Menu
        private IActionResult BuscarMenuxId(Menu menu)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MenuBusinessLogic.Current.GetOne(menu);
                if (result != null && result.Numero_Menu != 0)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MenuToListDTO>(result)));
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


        //Buscar Menu x Numero Menu
        private IActionResult BuscarMenuxNumeroMenu(Menu menu)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MenuBusinessLogic.Current.BuscarMenuxNumeroMenu(menu);
                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MenuToListDTO[]>(result)));
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



        //Buscar Menu x Fecha Menu


        private IActionResult BuscarMenuxFecha(Menu menu)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MenuBusinessLogic.Current.BuscarMenuxFechaMenu(menu);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MenuToListDTO[]>(result)));
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


        //Buscar Menu x Plato


        private IActionResult BuscarMenuxPlato(Menu menu)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MenuBusinessLogic.Current.BuscarMenuxPlato(menu);

                if (result != null)
                {
                    return Ok(JsonConvert.SerializeObject(_mapper.Map<MenuToListDTO[]>(result)));
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

        //Buscar Precio Menu x Fecha y Plato


        private IActionResult BuscarPrecioMenuxFechayPlato(Menu menu)
        {
            try
            {
                //cliente.Id_Empresa = Guid.Parse("60a4a5fa-76b2-4b1d-a961-2a1ac316f55f");
                //cliente.Id_Sucursal = Guid.Parse("d73a9380-da60-463f-a277-d5bc88dfa5d3");
                var result = MenuBusinessLogic.Current.BuscarPrecioMenudelDiaoPrecioVIgentexPlatoyFecha(menu);

                if (result >= 0)
                {
                    return Ok(JsonConvert.SerializeObject(result));
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
