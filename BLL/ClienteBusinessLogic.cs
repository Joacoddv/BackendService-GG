using System;
using System.Collections.Generic;
using BLL.Contracts;
using Dominio;
using DAL.Repositories;
using DAL.Contracts;
using DAL.Factories;
using System.Linq;
using Servicios.Services;
using System.Diagnostics.Tracing;
using Servicios.Services.Extensions;

namespace BLL
{

    public sealed class ClienteBusinessLogic : IGenericBusinessLogic<Cliente>
    {
        private readonly static ClienteBusinessLogic _instance = new ClienteBusinessLogic();
        List<Cliente> clientes = new List<Cliente>();

        IGenericRepository<Cliente> ClienteRepository = Factory.Current.GetClienteRepository();

        public static ClienteBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private ClienteBusinessLogic()
        {
            //Implent here the initialization of your singleton

            //clientes = ClienteRepository.GetAll().ToList();
        }

        public void Add(Cliente obj)
        {
            //Doy de alta un CLIENTE
            try
            {
                LoggerManager.Current.Write($"BLL Clientes - Validando alta de cliente", EventLevel.Informational);
                if (clientes.Any(o => o.Email.ToUpper().Equals(obj.Email.ToUpper())))
                {
                    //Ya existe un cliente con ese Email
                    throw new Exception($"Ya existe un cliente con el Email {obj.Email}");
                }
                else if ((clientes.Any(o => o.Nro_Doc.Equals(obj.Nro_Doc))) && obj.Nro_Doc != null)
                {
                    //Ya existe un Cliente con ese Número DNI
                    throw new Exception($"Ya existe un cliente con ese Número de Documento {obj.Nro_Doc}");
                }
                else
                {
                    ClienteRepository.Insert(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes: - Error al dar de alta cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            clientes = ClienteRepository.GetAll(obj).ToList();
        }

        public void Remove(Cliente obj)
        {
            //Remuevo un CLIENTE
            try
            {
                LoggerManager.Current.Write($"BLL Clientes - Validando desactivación de cliente", EventLevel.Informational);
                if (clientes.Any(o => o.Id_Cliente.Equals(obj.Id_Cliente)))
                {
                    if (clientes.Any(o => o.Id_Cliente.Equals(obj.Id_Cliente) & o.Estado.Equals(true)))
                    {
                        ClienteRepository.Delete(obj);
                    }
                    else
                    {
                        throw new Exception("El cliente ya se encuentra desactivado".Traducir());
                    }

                }
                else
                {
                    throw new Exception("No existe el cliente que se desea desactivar".Traducir());
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clentes: - Error al desactivar  cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            clientes = ClienteRepository.GetAll(obj).ToList();
        }

        public void Update(Cliente obj)
        {
            //Actualizo un CLIENTE
            try
            {
                bool estado = true;
                LoggerManager.Current.Write($"BLL Clientes - Validando actualización de cliente", EventLevel.Informational);
                if (clientes.Any(o => o.Email.ToUpper().Equals(obj.Email.ToUpper())))
                {
                    if ((from o in clientes where o.Email.ToUpper().Equals(obj.Email.ToUpper()) select o.Id_Cliente).First() != obj.Id_Cliente)
                    {
                        //Ya existe un cliente con ese Email
                        estado = false;
                        throw new Exception($"Ya existe un cliente con el Email {obj.Email}");
                    }

                }
                else if ((clientes.Any(o => o.Nro_Doc.Equals(obj.Nro_Doc))) && obj.Nro_Doc != null)
                {
                    if ((from o in clientes where (o.Nro_Doc.Equals(obj.Nro_Doc)) && obj.Nro_Doc != null select o.Id_Cliente).First() != obj.Id_Cliente)
                    {
                        //Ya existe un Cliente con ese Número DNI
                        estado = false;
                        throw new Exception($"Ya existe un cliente con ese Número de Documento {obj.Nro_Doc}");
                    }
                }

                if (estado == true)
                {
                    ClienteRepository.Update(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes: - Error al dar actualizar cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            clientes = ClienteRepository.GetAll(obj).ToList();

        }

        public IEnumerable<Cliente> GetAll(Cliente obj)
        {
            //Listo todos los clientes en orden descendente
            LoggerManager.Current.Write($"BLL Clientes - Validando listar clientes", EventLevel.Informational);
            try
            {
                return from o in ClienteRepository.GetAll(obj)
                       orderby o.Numero_Cliente descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al listar clientes: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Cliente GetOne(Cliente obj)
        {
            //Busco un cliente por su ID
            LoggerManager.Current.Write($"BLL Clientes - Validando buscar cliente por ID cliente", EventLevel.Informational);
            try
            {
                return ClienteRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por ID cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Cliente BuscarClientexNumeroExactoCliente(Cliente obj)
        {
            //Busco un cliente a partir del numero cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar empresa por número empresa", EventLevel.Informational);
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                if (clientes.Any(o => o.Numero_Cliente.Equals(obj.Numero_Cliente)))
                {
                    obj = clientes.FirstOrDefault(o => o.Numero_Cliente.Equals(obj.Numero_Cliente));
                }
                else
                {
                    //Cuando no coincide el numero de cliente lanzo exepcion
                    throw new Exception($"No existe cliente con el número {obj.Numero_Cliente}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Cliente - Error al buscar cliente por número cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return obj;
        }


        public List<Cliente> BuscarClientesxNumeroCliente(Cliente obj)
        {
            //Busco  clientes a partir del numero cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por número cliente", EventLevel.Informational);
            List<Cliente> clientesxnumerocliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco clientes que en el nombre contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Numero_Cliente.ToString().Trim().Contains(obj.Numero_Cliente.ToString().Trim())))
                {
                    clientesxnumerocliente = (from o in clientes
                                              where o.Numero_Cliente.ToString().Trim().Contains(obj.Numero_Cliente.ToString().Trim())
                                              select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su número de cliente \"{obj.Numero_Cliente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por número de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxnumerocliente;
        }

        public List<Cliente> BuscarClientesxIdCliente(Cliente obj)
        {
            //Busco clientes a partir del numero cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por Id cliente", EventLevel.Informational);
            List<Cliente> clientesxidcliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco clientes que en el nombre contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Id_Cliente.ToString().Trim().Contains(obj.Id_Cliente.ToString().Trim())))
                {
                    clientesxidcliente = (from o in clientes
                                          where o.Id_Cliente.ToString().Trim().Contains(obj.Id_Cliente.ToString().Trim())
                                          select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su Id de cliente \"{obj.Id_Cliente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por Id de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxidcliente;
        }


        public List<Cliente> BuscarClientesxNombreCliente(Cliente obj)
        {
            //Busco clientes a partir del nombre cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por nombre cliente", EventLevel.Informational);
            List<Cliente> clientesxnombrecliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco clientes que en el nombre contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Nombre.ToString().Trim().ToUpper().Contains(obj.Nombre.ToString().ToUpper().Trim())))
                {
                    clientesxnombrecliente = (from o in clientes
                                              where o.Nombre.ToString().Trim().ToUpper().Contains(obj.Nombre.ToString().ToUpper().Trim())
                                              select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su nombre de cliente \"{obj.Nombre}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por nombre: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxnombrecliente;
        }

        public List<Cliente> BuscarClientesxApellidoCliente(Cliente obj)
        {
            //Busco cliente a partir del Apellido cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por apellido cliente", EventLevel.Informational);
            List<Cliente> clientesxapellidocliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco clientes que en el Apellido contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Apellido.ToString().Trim().ToUpper().Contains(obj.Apellido.ToString().ToUpper().Trim())))
                {
                    clientesxapellidocliente = (from o in clientes
                                                where o.Apellido.ToString().Trim().ToUpper().Contains(obj.Apellido.ToString().ToUpper().Trim())
                                                select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su apellido: \"{obj.Apellido}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por apellido de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxapellidocliente;
        }

        public List<Cliente> BuscarClientesxNroDocCliente(Cliente obj)
        {
            //Busco un cliente a partir del Nro_Doc cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por Nro_Doc cliente", EventLevel.Informational);
            List<Cliente> clientesxNroDoccliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco cliente que en el Nro_Doc contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Nro_Doc.ToString().Trim().ToUpper().Contains(obj.Nro_Doc.ToString().ToUpper().Trim())))
                {
                    clientesxNroDoccliente = (from o in clientes
                                              where o.Nro_Doc.ToString().Trim().ToUpper().Contains(obj.Nro_Doc.ToString().ToUpper().Trim())
                                              select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su Número Documento: \"{obj.Nro_Doc}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por Número Documento de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxNroDoccliente;
        }

        public List<Cliente> BuscarClientesxEstadoCliente(Cliente obj)
        {
            //Busco un cliente a partir del Estado cliente
            LoggerManager.Current.Write($"BLL Cliente - Validando buscar cliente por Estado cliente", EventLevel.Informational);
            List<Cliente> clientesxestadocliente = new List<Cliente>();
            try
            {
                clientes = ClienteRepository.GetAll(obj).ToList();
                //Busco cliente que en el Estado contengan los valores ingresados por el usuario
                if (clientes.Any(o => o.Estado.ToString().Trim().ToUpper().Contains(obj.Estado.ToString().ToUpper().Trim())))
                {
                    clientesxestadocliente = (from o in clientes
                                              where o.Estado.ToString().Trim().ToUpper().Contains(obj.Estado.ToString().ToUpper().Trim())
                                              select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun cliente contiene en su Estado: \"{obj.Estado}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Clientes - Error al buscar cliente por Estado de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return clientesxestadocliente;
        }

    }
}
