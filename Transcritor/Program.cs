using Microsoft.Extensions.Configuration;
using System.Text;
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

string caminho = @"E:\Documentos\TranscreverProvas";
string caminhoSaida = Path.Combine(caminho, "resultado_consolidado.md");
using var httpClient = new HttpClient();

IArquivoScanner scanner = new ArquivoScanner();
IEnumerable<DocumentoEntrada> documentos = scanner.EscanearPasta(caminho);

ITranscritorIA transcritor = new GeminiService(apiKey, httpClient);

IEscritorDocumento escritor = new EscritorDocumento();

StringBuilder sb = new StringBuilder();

foreach (var documento in documentos)
{
    string resultado = await transcritor.EnviarParaIAAsync(documento);
    sb.AppendLine(resultado + "\n");
}

await escritor.SalvarAsync(caminhoSaida, sb.ToString());