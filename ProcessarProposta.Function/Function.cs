using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Storage.V1;
using Google.Events.Protobuf.Cloud.Storage.V1;
using CloudNative.CloudEvents;

namespace ProcessarProposta.Function;

public class ProcessadorProposta : ICloudEventFunction<StorageObjectData>
{
    public async Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
    {
        var bucketOutput = "motor-decisao-output";
        var storage = StorageClient.Create();

        var nomeArquivo = data.Name;

        using var stream = new MemoryStream();
        await storage.DownloadObjectAsync(data.Bucket, nomeArquivo, stream, cancellationToken: cancellationToken);
        stream.Position = 0;

        var propostaJson = new StreamReader(stream).ReadToEnd();
        // Aqui você processa a proposta (ex: motor de decisão de crédito)
        var resultado = new { idProposta = nomeArquivo, decisao = "aprovada", limite = 5000 };

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resultado));
        using var outStream = new MemoryStream(bytes);
        await storage.UploadObjectAsync(bucketOutput, nomeArquivo.Replace(".json", "-result.json"), "application/json", outStream);
    }
}