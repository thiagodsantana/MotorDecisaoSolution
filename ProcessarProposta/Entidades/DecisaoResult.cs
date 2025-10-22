using System;

namespace ProcessarProposta.Entidades;

public record DecisaoResult
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = "PENDING";
    public decimal ValorAprovado { get; init; }
    public DateTime DataDecisao { get; init; }
}
