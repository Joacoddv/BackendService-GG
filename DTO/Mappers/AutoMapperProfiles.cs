using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Servicios.Domain.Usuario_Patente_Familia;
using DTO.Usuarios;
using DTO.Ingredientes;
using DTO.Cliente;
using Dominio;
using DTO.Direcciones;
using DTO.Plato;
using DTO.Plato_Ingrediente;
using DTO.Menu;
using DTO.Plato_Pedido;
using DTO.Mesa;
using DTO.Plato_Precio;
using DTO.Factura;
using DTO.Factura_Pedido;
using DTO.Pedido;

namespace DTO.Mappers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            //Mapeo para Usuarios
            CreateMap<UsuarioLoginDTO, Usuario>();

            CreateMap<UsuarioCreacionDTO, Usuario>();

            CreateMap<Usuario, UsuarioToListDTO>();



            //Mapeo para Ingredientes
            CreateMap<IngredienteBusquedaDTO, Ingrediente>();

            CreateMap<IngredienteCreacionDTO, Ingrediente>();

            CreateMap<IngredienteEdicionDTO, Ingrediente>();

            CreateMap<IngredienteToListDTO, Ingrediente>();

            CreateMap<Ingrediente, IngredienteToListDTO>();

            CreateMap <Ingrediente[], IngredienteToListDTO[]>();


            //Mapeo para Clientes

            CreateMap<ClienteBusquedaDTO, Dominio.Cliente>();

            CreateMap<ClienteCreacionDTO, Dominio.Cliente>();

            CreateMap<ClienteEdicionDTO, Dominio.Cliente>();

            CreateMap<ClienteToListDTO, Dominio.Cliente>();

            CreateMap<Dominio.Cliente, ClienteToListDTO>();

            CreateMap<Dominio.Cliente[], ClienteToListDTO[]>();



            //Mapeo para Direccion

            CreateMap<DireccionBusquedaDTO, Dominio.Direccion>().ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => src.ClienteBusquedaDTO));

            CreateMap<DireccionCreacionDTO, Dominio.Direccion>().ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => src.ClienteBusquedaDTO));

            CreateMap<DireccionEdicionDTO, Dominio.Direccion>().ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => src.ClienteBusquedaDTO));

            CreateMap<DireccionToListDTO, Dominio.Direccion>();

            CreateMap<Dominio.Direccion, DireccionToListDTO>().ForMember(dest => dest.clienteToListDTO, opt => opt.MapFrom(src => src.Cliente));

            CreateMap<Dominio.Direccion[], DTO.Direcciones.DireccionToListDTO[]>();


            //Mapeo para platos

            CreateMap<PlatoBusquedaDTO, Dominio.Plato>();

            CreateMap<PlatoCreacionDTO, Dominio.Plato>();

            CreateMap<PlatoEdicionDTO, Dominio.Plato>();

            CreateMap<PlatoToListDTO, Dominio.Plato>();

            CreateMap<Dominio.Plato, PlatoToListDTO>();

            CreateMap<Dominio.Plato[], PlatoToListDTO[]>();


            //Mapeo para Platos_Ingrediente
            CreateMap<Plato_IngredienteBusquedaDTO, Dominio.Plato_Ingrediente>();

            CreateMap<Plato_IngredienteCreacionDTO, Dominio.Plato_Ingrediente>();//.ForMember(dest => dest.Plato, opt => opt.MapFrom(src => src.Plato)).ForMember(dest => dest.Ingrediente, opt => opt.MapFrom(src => src.Ingrediente));

            CreateMap<Plato_IngredienteEdicionDTO, Dominio.Plato_Ingrediente>();

            CreateMap<Plato_IngredienteToListDTO, Dominio.Plato_Ingrediente>();

            CreateMap<Dominio.Plato_Ingrediente, DTO.Plato_Ingrediente.Plato_IngredienteToListDTO>();

            CreateMap<Dominio.Plato_Ingrediente[], DTO.Plato_Ingrediente.Plato_IngredienteToListDTO[]>();



            //Mapeo para Menu

            CreateMap<MenuBusquedaDTO, Dominio.Menu>();

            CreateMap<MenuCreacionDTO, Dominio.Menu>();

            CreateMap<MenuEdicionDTO, Dominio.Menu>();

            CreateMap<Dominio.Menu, MenuToListDTO>();

            CreateMap<Dominio.Menu[], MenuToListDTO[]>();


            //Mapeo para Platos_Pedido
            CreateMap<Plato_PedidoBusquedaDTO, Dominio.Plato_Pedido>();

            CreateMap<Plato_PedidoCreacionDTO, Dominio.Plato_Pedido>();

            CreateMap <Plato_PedidoEdicionDTO, Dominio.Plato_Pedido>();

            CreateMap<Dominio.Plato_Pedido, Plato_PedidoToListDTO >();

            CreateMap<Dominio.Plato_Pedido[], Plato_PedidoToListDTO[]>();



            //Mapeo para Mesas
            CreateMap<MesaBusquedaDTO, Dominio.Mesa>();

            CreateMap<MesaCreacionDTO, Dominio.Mesa>();

            CreateMap<MesaEdicionDTO, Dominio.Mesa>();

            CreateMap<MesaToListDTO, Dominio.Mesa>();

            CreateMap<Dominio.Mesa, MesaToListDTO>();

            CreateMap<Dominio.Mesa[], MesaToListDTO[]>();



            //Mapeo para Plato_Precio

            CreateMap<Plato_PrecioBusquedaDTO, Dominio.Plato_Precio>();

            CreateMap<Plato_PrecioCreacionDTO, Dominio.Plato_Precio>();

            CreateMap<Plato_PrecioEdicionDTO, Dominio.Plato_Precio>();

            CreateMap<Dominio.Plato_Precio, Plato_PrecioToListoDTO>();

            CreateMap<Dominio.Plato_Precio[], Plato_PrecioToListoDTO[]>();




            //Mapeo para Factura
            CreateMap<FacturaBusquedaDTO, Dominio.Factura>();

            CreateMap<FacturaCreacionDTO, Dominio.Factura>();

            CreateMap<FacturaToListDTO, Dominio.Factura>();

            CreateMap<FacturaEdicionDTO, Dominio.Factura>();

            CreateMap<Dominio.Factura, FacturaToListDTO>();

            CreateMap<Dominio.Factura[], FacturaToListDTO[]>();




            //Mapeo para Factura_Pedido
            CreateMap<Factura_PedidoBusquedaDTO, Dominio.Factura_Pedido>();

            CreateMap<Factura_PedidoCreacionDTO, Dominio.Factura_Pedido>();

            CreateMap<Factura_PedidoEdicionDTO, Dominio.Factura_Pedido>();

            CreateMap<Dominio.Factura_Pedido, Factura_PedidoToListDTO>();

            CreateMap<Dominio.Factura_Pedido[], Factura_PedidoToListDTO[]>();




            //Mapeo para Pedido
            CreateMap<PedidoBusquedaDTO, Dominio.Pedido>();

            CreateMap<DTO.Pedido.PedidoCreacionDTO, Dominio.Pedido>().ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => (src.Cliente)));

            CreateMap<PedidoEdicionDTO, Dominio.Pedido>();

            CreateMap<PedidoToListDTO, Dominio.Pedido>();

            CreateMap<Dominio.Pedido, PedidoToListDTO>();

            CreateMap<Dominio.Pedido[], PedidoToListDTO[]>();
        }
    }
}
