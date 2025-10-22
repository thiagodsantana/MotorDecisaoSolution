using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Storage.V1;
using Google.Events.Protobuf.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using ProcessarProposta.Entidades;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessarProposta;

public class Function(ILogger<Function> logger) : ICloudEventFunction<StorageObjectData>
{
    private readonly StorageClient _storage = StorageClient.Create();
    private const string OutputBucket = "motor-decisao-output";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
    {
        if (!EhArquivoValido(data))
        {
            logger.LogInformation("Ignorado: {Bucket}/{Name}", data.Bucket, data.Name);
            return;
        }

        try
        {
            logger.LogInformation("Processando arquivo: gs://{Bucket}/{Name}", data.Bucket, data.Name);

            var proposta = await BaixarECarregarPropostaAsync(data, cancellationToken);
            logger.LogInformation("Proposta recebida: {Nome}, Idade {Idade}, Renda {Renda}",
                proposta.Nome, proposta.Idade, proposta.RendaMensal);

            var decisao = GerarDecisao(proposta);

            var outputName = await SalvarDecisaoAsync(data, decisao, cancellationToken);

            logger.LogInformation("Decisão gerada com sucesso: gs://{Bucket}/{ObjectName}",
                OutputBucket, outputName);
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Erro ao desserializar arquivo JSON {Name}", data.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado ao processar evento do Cloud Storage.");
            throw;
        }
    }

    private static bool EhArquivoValido(StorageObjectData data) =>
        !string.IsNullOrEmpty(data.Name)
        && Path.GetExtension(data.Name).Equals(".json", StringComparison.OrdinalIgnoreCase);

    private async Task<Proposta> BaixarECarregarPropostaAsync(StorageObjectData data, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await _storage.DownloadObjectAsync(data.Bucket, data.Name, ms, cancellationToken: ct);
        ms.Position = 0;

        return await JsonSerializer.DeserializeAsync<Proposta>(ms, JsonOptions, ct)
               ?? throw new InvalidOperationException($"Arquivo {data.Name} contém JSON inválido ou vazio!");
    }

    private async Task<string> SalvarDecisaoAsync(StorageObjectData origem, DecisaoResult decisao, CancellationToken ct)
    {
        var baseName = Path.GetFileNameWithoutExtension(origem.Name);
        var objectName = $"decisions/{baseName}_{decisao.Id}.json";

        using var outStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(decisao, JsonOptions));
        await _storage.UploadObjectAsync(OutputBucket, objectName, "application/json", outStream, cancellationToken: ct);

        return objectName;
    }

    private static DecisaoResult GerarDecisao(Proposta proposta)
    {
        bool aprovado = proposta is { Idade: > 18 and < 60, RendaMensal: > 2000 };

        return new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = aprovado ? "APPROVED" : "REJECTED",
            ValorAprovado = aprovado ? (proposta.RendaMensal * 12) / 10m : 0,
            DataDecisao = DateTime.UtcNow
        };
    }
}
