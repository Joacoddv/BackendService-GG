using FluentAssertions;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

public class ClienteTests
{
    private static Cuit ValidCuit() => new("20123456786"); // 20-12345678-6, valid check digit
    private static Email ValidEmail() => new("cliente@example.com");

    [Fact]
    public void Crear_WithValidData_CreatesActiveCliente()
    {
        var cliente = Cliente.Crear("Juan Perez", CondicionIVA.ConsumidorFinal, null, null);

        cliente.Nombre.Should().Be("Juan Perez");
        cliente.CondicionIVA.Should().Be(CondicionIVA.ConsumidorFinal);
        cliente.Activo.Should().BeTrue();
        cliente.Id.Should().NotBe(Guid.Empty);
        cliente.NumeroCliente.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_WithEmptyNombre_ThrowsDomainException()
    {
        var act = () => Cliente.Crear("", CondicionIVA.ConsumidorFinal, null, null);
        act.Should().Throw<DomainException>()
           .WithMessage("*Nombre*");
    }

    [Fact]
    public void Crear_WithNullNombre_ThrowsDomainException()
    {
        var act = () => Cliente.Crear(null!, CondicionIVA.ConsumidorFinal, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Crear_ResponsableInscripto_WithoutCuit_ThrowsDomainException()
    {
        var act = () => Cliente.Crear("Empresa SA", CondicionIVA.ResponsableInscripto, null, null);
        act.Should().Throw<DomainException>()
           .WithMessage("*CUIT*required*");
    }

    [Fact]
    public void Crear_ResponsableInscripto_WithCuit_Succeeds()
    {
        var cliente = Cliente.Crear("Empresa SA", CondicionIVA.ResponsableInscripto, ValidCuit(), null);
        cliente.Cuit.Should().NotBeNull();
        cliente.CondicionIVA.Should().Be(CondicionIVA.ResponsableInscripto);
    }

    [Fact]
    public void Crear_NumeroCliente_IsImmutableAndNonEmpty()
    {
        var cliente = Cliente.Crear("Ana Lopez", CondicionIVA.Monotributista, null, null);
        var numeroOriginal = cliente.NumeroCliente;

        // NumeroCliente has no setter — immutability is structural
        cliente.NumeroCliente.Should().Be(numeroOriginal);
        cliente.NumeroCliente.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Desactivar_ActiveCliente_SetsActivoFalse()
    {
        var cliente = Cliente.Crear("Maria", CondicionIVA.ConsumidorFinal, null, null);

        cliente.Desactivar();

        cliente.Activo.Should().BeFalse();
    }

    [Fact]
    public void Desactivar_AlreadyInactive_IsIdempotent()
    {
        var cliente = Cliente.Crear("Pedro", CondicionIVA.ConsumidorFinal, null, null);
        cliente.Desactivar();

        // Second call must not throw
        var act = () => cliente.Desactivar();
        act.Should().NotThrow();
        cliente.Activo.Should().BeFalse();
    }

    [Fact]
    public void AgregarDireccion_AddsToList()
    {
        var cliente = Cliente.Crear("Luis", CondicionIVA.ConsumidorFinal, null, null);
        var direccion = new Direccion(Guid.NewGuid(), "Corrientes", "1234", "CABA", "Buenos Aires", "C1043");

        cliente.AgregarDireccion(direccion);

        cliente.Direcciones.Should().HaveCount(1);
        cliente.Direcciones[0].Calle.Should().Be("Corrientes");
    }

    [Fact]
    public void EliminarDireccion_RemovesFromList()
    {
        var cliente = Cliente.Crear("Luis", CondicionIVA.ConsumidorFinal, null, null);
        var direccionId = Guid.NewGuid();
        var direccion = new Direccion(direccionId, "Corrientes", "1234", "CABA", "Buenos Aires", "C1043");
        cliente.AgregarDireccion(direccion);

        cliente.EliminarDireccion(direccionId);

        cliente.Direcciones.Should().BeEmpty();
    }

    // ── ActualizarDatos (CCC-T13, CCC-T27) ───────────────────────────────────

    [Fact]
    public void ActualizarDatos_WithValidData_UpdatesFields()
    {
        var cliente = Cliente.Crear("Original", CondicionIVA.ConsumidorFinal, null, null);
        var cuit  = ValidCuit();
        var email = ValidEmail();
        var originalNumero = cliente.NumeroCliente;
        var originalActivo = cliente.Activo;

        cliente.ActualizarDatos("Updated Name", CondicionIVA.Monotributista, null, email);

        cliente.Nombre.Should().Be("Updated Name");
        cliente.CondicionIVA.Should().Be(CondicionIVA.Monotributista);
        cliente.Email.Should().NotBeNull();
        // NumeroCliente and Activo must remain unchanged
        cliente.NumeroCliente.Should().Be(originalNumero);
        cliente.Activo.Should().Be(originalActivo);
    }

    [Fact]
    public void ActualizarDatos_UpdatesCuit_WhenProvided()
    {
        var cliente = Cliente.Crear("Empresa", CondicionIVA.ConsumidorFinal, null, null);
        var cuit = ValidCuit();

        cliente.ActualizarDatos("Empresa SA", CondicionIVA.ResponsableInscripto, cuit, null);

        cliente.Cuit.Should().NotBeNull();
        cliente.Cuit!.Valor.Should().Be(cuit.Valor);
    }

    [Fact]
    public void ActualizarDatos_NumeroClienteIsImmutable()
    {
        var cliente = Cliente.Crear("Test", CondicionIVA.ConsumidorFinal, null, null);
        var originalNumero = cliente.NumeroCliente;

        cliente.ActualizarDatos("New Name", CondicionIVA.Monotributista, null, null);

        cliente.NumeroCliente.Should().Be(originalNumero);
    }

    [Fact]
    public void ActualizarDatos_ActivoIsUntouched()
    {
        var cliente = Cliente.Crear("Test", CondicionIVA.ConsumidorFinal, null, null);
        cliente.Desactivar(); // inactive

        cliente.ActualizarDatos("New Name", CondicionIVA.ConsumidorFinal, null, null);

        cliente.Activo.Should().BeFalse(); // still inactive
    }

    [Fact]
    public void ActualizarDatos_ResponsableInscripto_WithoutCuit_ThrowsDomainException()
    {
        var cliente = Cliente.Crear("Test", CondicionIVA.ConsumidorFinal, null, null);

        var act = () => cliente.ActualizarDatos("Test", CondicionIVA.ResponsableInscripto, null, null);

        act.Should().Throw<DomainException>().WithMessage("*CUIT*required*");
    }

    [Fact]
    public void ActualizarDatos_EmptyNombre_ThrowsDomainException()
    {
        var cliente = Cliente.Crear("Test", CondicionIVA.ConsumidorFinal, null, null);

        var act = () => cliente.ActualizarDatos("", CondicionIVA.ConsumidorFinal, null, null);

        act.Should().Throw<DomainException>().WithMessage("*Nombre*");
    }

    [Fact]
    public void ActualizarDatos_WhitespaceNombre_ThrowsDomainException()
    {
        var cliente = Cliente.Crear("Test", CondicionIVA.ConsumidorFinal, null, null);

        var act = () => cliente.ActualizarDatos("   ", CondicionIVA.ConsumidorFinal, null, null);

        act.Should().Throw<DomainException>().WithMessage("*Nombre*");
    }
}
