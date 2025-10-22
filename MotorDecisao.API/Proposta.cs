namespace MotorDecisao.API;

public record Proposta(
    string? Nome,
    string? Cpf,
    decimal? RendaMensal,
    int? Idade,
    string? Telefone,
    string? Email,
    object? DadosAdicionais
);
