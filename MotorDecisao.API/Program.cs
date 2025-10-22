using System.Text.Json;
using System.Text;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configuração
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Services.AddSingleton(_ => StorageClient.Create());

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    mensagem = "API Motor de Decisão — Cloud Run",
    versao = "v1.0"
}));

// Configuração auxiliar
static string GetBucketName(IConfiguration config) =>
    config["BucketInput"] ?? Environment.GetEnvironmentVariable("BucketInput")
    ?? throw new InvalidOperationException("Nome do bucket não configurado (BucketInput).");

static IResult InternalError(string msg, Exception ex)
{
    Console.Error.WriteLine($"{msg}: {ex}");
    return Results.Problem(detail: msg, statusCode: 500);
}

// POST /propostas
app.MapPost("/propostas", async ([FromServices] StorageClient storageClient, HttpRequest request, IConfiguration config) =>
{
    string body;
    using (var sr = new StreamReader(request.Body))
        body = await sr.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest(new { erro = "Corpo da requisição vazio. Envie o JSON da proposta." });

    // valida JSON
    try { _ = JsonDocument.Parse(body); }
    catch (JsonException) { return Results.BadRequest(new { erro = "JSON inválido." }); }

    var idProposta = Guid.NewGuid().ToString("N");
    var bucket = GetBucketName(config);
    var path = $"applications/{idProposta}.json";

    try
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        using var ms = new MemoryStream(bytes);
        await storageClient.UploadObjectAsync(bucket, path, "application/json", ms);
    }
    catch (Exception ex)
    {
        return InternalError("Erro ao salvar a proposta no Cloud Storage", ex);
    }

    return Results.Created($"/propostas/{idProposta}", new
    {
        id = idProposta,
        local = $"gs://{bucket}/{path}",
        mensagem = "Proposta recebida e salva com sucesso."
    });
});

// POST /propostas/{id}/documentos
app.MapPost("/propostas/{id}/documentos", async ([FromServices] StorageClient storageClient, string id, HttpRequest request, IConfiguration config) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { erro = "Conteúdo deve ser multipart/form-data." });

    var form = await request.ReadFormAsync();
    if (form.Files.Count == 0)
        return Results.BadRequest(new { erro = "Nenhum arquivo enviado." });

    var bucket = GetBucketName(config);
    var resultados = new List<object>();

    foreach (var file in form.Files)
    {
        var objeto = $"documents/{id}/{Guid.NewGuid()}_{file.FileName}";

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            await storageClient.UploadObjectAsync(
                bucket,
                objeto,
                file.ContentType ?? "application/octet-stream",
                ms
            );

            resultados.Add(new { arquivo = file.FileName, local = $"gs://{bucket}/{objeto}" });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao enviar documento {file.FileName}: {ex}");
            resultados.Add(new { arquivo = file.FileName, erro = ex.Message });
        }
    }

    return Results.Ok(new { id, resultados });
});

// GET /propostas/{id}
app.MapGet("/propostas/{id}", async ([FromServices] StorageClient storageClient, string id, IConfiguration config) =>
{
    var bucket = GetBucketName(config);
    var path = $"applications/{id}.json";

    try
    {
        using var ms = new MemoryStream();
        await storageClient.DownloadObjectAsync(bucket, path, ms);
        ms.Position = 0;

        var json = await new StreamReader(ms).ReadToEndAsync();
        return Results.Content(json, "application/json");
    }
    catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { erro = "Proposta não encontrada." });
    }
    catch (Exception ex)
    {
        return InternalError("Erro ao ler a proposta do Cloud Storage", ex);
    }
});

app.Run();
