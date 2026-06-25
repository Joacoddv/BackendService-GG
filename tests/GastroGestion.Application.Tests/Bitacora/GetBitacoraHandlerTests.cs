using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Bitacora.GetBitacora;
using GastroGestion.Domain.Enums;
using NSubstitute;

namespace GastroGestion.Application.Tests.Bitacora;

/// <summary>
/// Unit tests for GetBitacoraHandler. The repository is mocked — no database involved.
/// Tests verify that the handler delegates to the repository with the correct query.
/// </summary>
public sealed class GetBitacoraHandlerTests
{
    private readonly IBitacoraRepository _repository = Substitute.For<IBitacoraRepository>();
    private readonly GetBitacoraHandler  _sut;

    public GetBitacoraHandlerTests()
        => _sut = new GetBitacoraHandler(_repository);

    private static BitacoraEntryReadModel MakeEntry(DateTime fechaUtc, Guid? usuarioId = null)
        => new(
            Guid.NewGuid(),
            usuarioId ?? Guid.NewGuid(),
            "user@test.local",
            RolUsuario.Administrador,
            "Test action",
            null,
            200,
            fechaUtc);

    // ── Delegation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DelegatesToRepository_WithQueryUnchanged()
    {
        var query = new GetBitacoraQuery(
            Desde: new DateTime(2025, 1, 1),
            Hasta: new DateTime(2025, 12, 31),
            UsuarioId: Guid.NewGuid(),
            Page: 2,
            PageSize: 10);

        var expected = new GetBitacoraResult([], 0, 2, 10);
        _repository.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(query);

        result.Should().Be(expected);
        await _repository.Received(1).GetPagedAsync(query, Arg.Any<CancellationToken>());
    }

    // ── Filters: the handler is a thin delegator, but we verify the contract.
    // Real filter logic is in BitacoraRepository; these tests confirm the correct
    // query object is forwarded.

    [Fact]
    public async Task Handle_WithDesdeFilter_ForwardsDesdeToRepository()
    {
        var desde = new DateTime(2025, 6, 1);
        var query = new GetBitacoraQuery(Desde: desde);
        _repository.GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.Desde == desde), Arg.Any<CancellationToken>())
                   .Returns(new GetBitacoraResult([], 0, 1, 50));

        await _sut.Handle(query);

        await _repository.Received(1)
            .GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.Desde == desde), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUsuarioIdFilter_ForwardsUsuarioIdToRepository()
    {
        var uid   = Guid.NewGuid();
        var query = new GetBitacoraQuery(UsuarioId: uid);
        _repository.GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.UsuarioId == uid), Arg.Any<CancellationToken>())
                   .Returns(new GetBitacoraResult([], 0, 1, 50));

        await _sut.Handle(query);

        await _repository.Received(1)
            .GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.UsuarioId == uid), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPageAndPageSize_ForwardsToRepository()
    {
        var query = new GetBitacoraQuery(Page: 3, PageSize: 25);
        _repository.GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.Page == 3 && q.PageSize == 25), Arg.Any<CancellationToken>())
                   .Returns(new GetBitacoraResult([], 0, 3, 25));

        await _sut.Handle(query);

        await _repository.Received(1)
            .GetPagedAsync(Arg.Is<GetBitacoraQuery>(q => q.Page == 3 && q.PageSize == 25), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultUnchanged()
    {
        var entry1 = MakeEntry(new DateTime(2025, 6, 1, 10, 0, 0));
        var entry2 = MakeEntry(new DateTime(2025, 6, 2, 10, 0, 0));
        var expected = new GetBitacoraResult(
            Items: new[] { entry2, entry1 }, // newest first
            TotalCount: 2, Page: 1, PageSize: 50);

        _repository.GetPagedAsync(Arg.Any<GetBitacoraQuery>(), Arg.Any<CancellationToken>())
                   .Returns(expected);

        var result = await _sut.Handle(new GetBitacoraQuery());

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].FechaUtc.Should().BeAfter(result.Items[1].FechaUtc);
    }
}
