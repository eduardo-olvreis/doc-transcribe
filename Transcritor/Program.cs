using Microsoft.Extensions.Configuration;
using Transcritor;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

string? apiKey = config["Gemini:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Erro: A chave 'Gemini:ApiKey' não foi configurada no appsettings.Development.json.");
    return;
}

string caminho = @"C:\Users\eduar\Desktop\TranscreverProvas";
using var httpClient = new HttpClient();

IArquivoScanner scanner = new ArquivoScanner();
IEnumerable<DocumentoEntrada> documentos = scanner.EscanearPasta(caminho);

ITranscritorIA transcritor = new GeminiService(apiKey, httpClient);

foreach (var documento in documentos)
{
    string resultado = await transcritor.EnviarParaIAAsync(documento);
    Console.WriteLine(resultado);
}