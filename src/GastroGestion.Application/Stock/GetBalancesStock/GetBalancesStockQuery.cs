using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Stock.GetBalancesStock;

/// <summary>Query for GET /stock/balances — current balance for every ingredient.</summary>
public sealed record GetBalancesStockQuery;

/// <summary>Per-ingredient stock balance projection (name + unit included for display).</summary>
public sealed record IngredienteBalanceResult(
    Guid IngredienteId,
    string Nombre,
    UnidadDeMedida Unidad,
    bool Activo,
    decimal Balance);
