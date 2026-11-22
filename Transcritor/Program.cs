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

// Prioridade da pasta de entrada: argumento da linha de comando > appsettings.
string? caminho = args.Length > 0 ? args[0] : config["Transcricao:PastaEntrada"];
if (string.IsNullOrWhiteSpace(caminho))
{
    Console.WriteLine("Erro: informe a pasta de entrada por argumento ou em 'Transcricao:PastaEntrada'.");
    return;
}
if (!Directory.Exists(caminho))
{
    Console.WriteLine($"Erro: a pasta de entrada não existe: {caminho}");
    return;
}

string pastaSaida = config["Transcricao:PastaSaida"] is { Length: > 0 } saida ? saida : caminho;
Directory.CreateDirectory(pastaSaida);
string nomeArquivoSaida = config["Transcricao:NomeArquivoSaida"] is { Length: > 0 } nome ? nome : "resultado_consolidado.md";
string caminhoSaida = Path.Combine(pastaSaida, nomeArquivoSaida);
string nomeDocumentoDrive = config["Transcricao:NomeDocumentoDrive"] is { Length: > 0 } nomeDoc ? nomeDoc : "Prova Consolidada";
using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromMinutes(5);

IArquivoScanner scanner = new ArquivoScanner();
IEnumerable<DocumentoEntrada> documentos = scanner.EscanearPasta(caminho);

ITranscritorIA transcritor = new GeminiService(apiKey, httpClient);

IEscritorDocumento escritor = new EscritorDocumento();

StringBuilder sb = new StringBuilder();

int numeroQuestaoGlobal = 1;

foreach (var documento in documentos)
{
    Console.WriteLine($"Processando documento: {documento.Nome}");
    string resultadoIA = await transcritor.EnviarParaIAAsync(documento);
    var blocosBrutos = resultadoIA.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
    List<string> questoesConsolidadas = new List<string>();
    string questaoAtual = string.Empty;

    foreach (var bloco in blocosBrutos)
    {
        string blocoTrim = bloco.Trim();
        bool pareceAlternativa = System.Text.RegularExpressions.Regex.IsMatch(blocoTrim, @"^[a-eA-E][\)\.-]");

        if (pareceAlternativa && !string.IsNullOrEmpty(questaoAtual))
        {
            questaoAtual += "\n\n" + blocoTrim;
        }
        else
        {
            if (!string.IsNullOrEmpty(questaoAtual))
            {
                questoesConsolidadas.Add(questaoAtual);
            }
            questaoAtual = blocoTrim;
        }
    }

    if (!string.IsNullOrEmpty(questaoAtual))
    {
        questoesConsolidadas.Add(questaoAtual);
    }

    StringBuilder sbQuestaoFormatada = new StringBuilder();

    foreach (var questao in questoesConsolidadas)
    {
        sbQuestaoFormatada.AppendLine($"{numeroQuestaoGlobal} - {questao}");
        sbQuestaoFormatada.AppendLine();
        sbQuestaoFormatada.AppendLine();
        numeroQuestaoGlobal++;
    }

    sb.AppendLine(sbQuestaoFormatada.ToString());
    await Task.Delay(2000);
}

string conteudoMarkdown = sb.ToString();
conteudoMarkdown = System.Text.RegularExpressions.Regex.Replace(conteudoMarkdown, @"\s+(?=[A-Da-d]\)|\([A-Da-d]\))", "  \n");
await escritor.SalvarAsync(caminhoSaida, conteudoMarkdown);


string googleToken = config["GoogleDrive:AccessToken"];

if (!string.IsNullOrEmpty(googleToken))
{
    var driveService = new GoogleDriveService(googleToken, httpClient);
    await driveService.EnviarParaGoogleDocsAsync(caminhoSaida, nomeDocumentoDrive);
}
else
{
    Console.WriteLine("Token do Google Drive não configurado. Pulando envio para a nuvem.");
}