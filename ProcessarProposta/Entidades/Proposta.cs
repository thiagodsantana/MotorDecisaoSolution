namespace ProcessarProposta.Entidades;

public record Proposta
{
    public string Nome { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public decimal RendaMensal { get; init; }
    public int Idade { get; init; }
    public string Telefone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}