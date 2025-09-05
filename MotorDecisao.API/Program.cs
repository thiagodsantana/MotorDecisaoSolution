using System.Text.Json;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// registrar cliente do Google Cloud Storage
builder.Services.AddSingleton(provider => StorageClient.Create());

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { mensagem = "API Motor de Decisão — Cloud Run" }));

// Rota: receber proposta JSON e salvar em gs://<BUCKET_INPUT>/applications/{id}.json
app.MapPost("/propostas", async ([FromServices] StorageClient storageClient, HttpRequest request, IConfiguration config) =>
{
    using var sr = new StreamReader(request.Body);
    var corpo = await sr.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(corpo))
        return Results.BadRequest(new { erro = "Corpo da requisição vazio. Envie o JSON da proposta." });

    // valida JSON mínimo
    try { _ = JsonDocument.Parse(corpo); }
    catch (JsonException) { return Results.BadRequest(new { erro = "JSON inválido." }); }

    var idProposta = Guid.NewGuid().ToString();
    var nomeBucket = config["BucketInput"] ?? Environment.GetEnvironmentVariable("BucketInput");
    if (string.IsNullOrWhiteSpace(nomeBucket))
        return Results.Problem(detail: "Nome do bucket não configurado (BucketInput).", statusCode: 500);

    var caminhoObjeto = $"applications/{idProposta}.json";

    try
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(corpo));
        await storageClient.UploadObjectAsync(nomeBucket, caminhoObjeto, "application/json", ms);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Erro ao enviar para o Cloud Storage: {ex}");
        return Results.Problem(detail: "Erro ao salvar a proposta no Cloud Storage.", statusCode: 500);
    }

    var resposta = new
    {
        id = idProposta,
        local = $"gs://{nomeBucket}/{caminhoObjeto}",
        mensagem = "Proposta recebida e salva com sucesso."
    };

    return Results.Created($"/propostas/{idProposta}", resposta);
});

// Rota: upload de documento associado (multipart/form-data)
// recebe campo 'idProposta' e arquivos 'arquivo'
app.MapPost("/propostas/{id}/documentos", async ([FromServices] StorageClient storageClient, string id, HttpRequest request, IConfiguration config) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { erro = "Conteúdo deve ser multipart/form-data." });

    var form = await request.ReadFormAsync();
    var files = form.Files;
    if (files.Count == 0) return Results.BadRequest(new { erro = "Nenhum arquivo enviado." });

    var nomeBucket = config["BucketInput"] ?? Environment.GetEnvironmentVariable("BucketInput");
    if (string.IsNullOrWhiteSpace(nomeBucket))
        return Results.Problem(detail: "Nome do bucket não configurado (BucketInput).", statusCode: 500);

    var resultados = new List<object>();

    foreach (var file in files)
    {
        var objeto = $"documents/{id}/{Guid.NewGuid()}_{file.FileName}";
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            await storageClient.UploadObjectAsync(nomeBucket, objeto, file.ContentType ?? "application/octet-stream", ms);
            resultados.Add(new { arquivo = file.FileName, local = $"gs://{nomeBucket}/{objeto}" });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao enviar documento: {ex}");
            resultados.Add(new { arquivo = file.FileName, erro = ex.Message });
        }
    }

    return Results.Ok(new { id, resultados });
});

// Rota de teste: recuperar proposta salvo
app.MapGet("/propostas/{id}", async ([FromServices] StorageClient storageClient, string id, IConfiguration config) =>
{
    var nomeBucket = config["BucketInput"] ?? Environment.GetEnvironmentVariable("BucketInput");
    if (string.IsNullOrWhiteSpace(nomeBucket))
        return Results.Problem(detail: "Nome do bucket não configurado (BucketInput).", statusCode: 500);

    var caminhoObjeto = $"applications/{id}.json";

    try
    {
        using var ms = new MemoryStream();
        await storageClient.DownloadObjectAsync(nomeBucket, caminhoObjeto, ms);
        ms.Position = 0;
        using var sr = new StreamReader(ms);
        var conteudo = await sr.ReadToEndAsync();
        return Results.Content(conteudo, "application/json");
    }
    catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { erro = "Proposta não encontrada." });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Erro ao baixar do Cloud Storage: {ex}");
        return Results.Problem(detail: "Erro ao ler a proposta do Cloud Storage.", statusCode: 500);
    }
});

app.Run();