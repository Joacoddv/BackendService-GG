using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for Cliente aggregate round-trip persistence.
/// Covers REQ-03 Scenario 03-A and REQ-04 Scenario 04-A.
/// </summary>
[Trait("Category", "SliceA")]
public sealed class ClienteRoundTripTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public ClienteRoundTripTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ClienteWithDirecciones_RoundTrips()
    {
        // Arrange
        var cliente = Cliente.Crear(
            "Juan Perez",
            CondicionIVA.ConsumidorFinal,
            cuit: null,
            email: new Email("juan@example.com"));

        var dir1 = new Direccion(
            Guid.NewGuid(), "Corrientes", "1234",
            ciudad: "Buenos Aires", provincia: "CABA", codigoPostal: "1043");

        var dir2 = new Direccion(
            Guid.NewGuid(), "Rivadavia", "456",
            ciudad: "Rosario", provincia: "Santa Fe", codigoPostal: "2000",
            piso: "3", departamento: "B");

        cliente.AgregarDireccion(dir1);
        cliente.AgregarDireccion(dir2);

        // Act — save
        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.SaveChangesAsync();
        }

        // Assert — reload in fresh context
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Clientes
            .FirstOrDefaultAsync(c => c.Id == cliente.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Juan Perez", reloaded.Nombre);
        Assert.Equal(CondicionIVA.ConsumidorFinal, reloaded.CondicionIVA);
        Assert.Equal("juan@example.com", reloaded.Email!.Valor);
        Assert.Null(reloaded.Cuit);
        Assert.Equal(2, reloaded.Direcciones.Count);

        var loadedDir2 = reloaded.Direcciones.First(d => d.Piso == "3");
        Assert.Equal("B", loadedDir2.Departamento);
        Assert.Equal("Rosario", loadedDir2.Ciudad);
    }

    [Fact]
    public async Task Cuit_Email_ConvertersPreserveNormalizedValues()
    {
        // Arrange — CUIT with hyphens, email with mixed case
        var cuit = new Cuit("20-30452742-1"); // valid CUIT with hyphens
        var email = new Email("Test@EXAMPLE.COM");

        var cliente = Cliente.Crear(
            "Empresa SA",
            CondicionIVA.ResponsableInscripto,
            cuit,
            email);

        // Act
        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Clientes.FirstOrDefaultAsync(c => c.Id == cliente.Id);

        // Assert — converters normalize on write, preserve normalized form on read
        Assert.NotNull(reloaded);
        Assert.Equal("20304527421", reloaded.Cuit!.Valor);   // hyphens stripped
        Assert.Equal("test@example.com", reloaded.Email!.Valor); // lowercased
    }
}
