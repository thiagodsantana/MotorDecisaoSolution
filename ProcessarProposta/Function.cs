using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.Storage.V1;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessarProposta;

public class Function(ILogger<Function> logger) : ICloudEventFunction<StorageObjectData>
{
    private readonly StorageClient _storage = StorageClient.Create();
    private readonly string _outputBucket = "motor-decisao-output";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
    {
        try
        {
            // Validação inicial
            if (!EhArquivoValido(data))
            {
                logger.LogInformation("Ignorado: {Bucket}/{Name}", data.Bucket, data.Name);
                return;
            }

            logger.LogInformation("Processando arquivo: gs://{Bucket}/{Name}", data.Bucket, data.Name);

            // Ler proposta
            var proposta = await BaixarECarregarPropostaAsync(data, cancellationToken);

            logger.LogInformation("Proposta recebida: {Nome}, Idade {Idade}, Renda {Renda}",
                proposta.nome, proposta.idade, proposta.rendaMensal);

            // Processar decisão
            var decisao = GerarDecisao(proposta);

            // Salvar decisão
            var outputName = await SalvarDecisaoAsync(data, decisao, cancellationToken);

            logger.LogInformation("Decisão gerada com sucesso: gs://{Bucket}/{ObjectName}", _outputBucket, outputName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar evento do Cloud Storage.");
            throw; // repropaga para logging da plataforma
        }
    }

    private static bool EhArquivoValido(StorageObjectData data) =>
        !string.IsNullOrEmpty(data.Name) &&
        Path.GetExtension(data.Name).Equals(".json", StringComparison.OrdinalIgnoreCase);

    private async Task<Proposta> BaixarECarregarPropostaAsync(StorageObjectData data, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await _storage.DownloadObjectAsync(data.Bucket, data.Name, ms, cancellationToken: ct);
        ms.Position = 0;

        return await JsonSerializer.DeserializeAsync<Proposta>(ms, _jsonOptions, ct)
               ?? throw new InvalidOperationException("JSON inválido ou vazio.");
    }

    private async Task<string> SalvarDecisaoAsync(StorageObjectData origem, DecisaoResult decisao, CancellationToken ct)
    {
        var baseName = Path.GetFileNameWithoutExtension(origem.Name);
        var objectName = $"decisions/{baseName}_{decisao.Id}.json";

        using var outStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(decisao, _jsonOptions));
        await _storage.UploadObjectAsync(_outputBucket, objectName, "application/json", outStream, cancellationToken: ct);

        return objectName;
    }

    private static DecisaoResult GerarDecisao(Proposta proposta)
    {
        bool aprovado = proposta.idade is > 18 and < 60 && proposta.rendaMensal > 2000;

        return new DecisaoResult
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = aprovado ? "APPROVED" : "REJECTED",
            ValorAprovado = aprovado ? (proposta.rendaMensal * 12) / 10m : 0,
            DataDecisao = DateTime.UtcNow
        };
    }
}

public class Proposta
{
    public string nome { get; set; } = string.Empty;
    public string cpf { get; set; } = string.Empty;
    public int rendaMensal { get; set; }
    public int idade { get; set; }
    public string telefone { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
}

public record DecisaoResult
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = "PENDING";
    public decimal ValorAprovado { get; init; }
    public DateTime DataDecisao { get; init; }
}
