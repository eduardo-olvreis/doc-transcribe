# Transcritor

App console .NET 10 (`Transcritor/Transcritor.csproj`, solução `Transcritor.slnx`). Lê provas (PDF e imagens) de uma pasta, transcreve com a API do Gemini e gera um Markdown consolidado. Depois envia o MD ao Google Drive, convertido em Google Docs.

## Fluxo atual (`Program.cs`)
1. Lê `Gemini:ApiKey` de `appsettings.json` + `appsettings.Development.json`. Sem chave, encerra.
2. Pasta de entrada: `args[0]` ou `Transcricao:PastaEntrada`. Saída: `Transcricao:PastaSaida` (vazio = pasta de entrada), arquivo `Transcricao:NomeArquivoSaida`. Pasta inexistente encerra com erro.
3. `ArquivoScanner.EscanearPasta` lista `.pdf .png .jpg .jpeg` em ordem natural de nome.
4. Para cada arquivo: `GeminiService.EnviarParaIAAsync` (modelo `gemini-3.1-flash-lite`, temperature 0.1, até 5 tentativas em 429/503 com espera crescente).
5. Resposta dividida em blocos por linha em branco. Bloco que começa com `a)`–`e)` (ou `A.`–`E.`) é agrupado à questão anterior.
6. Numeração global sequencial (`1 - `, `2 - `...) entre todos os arquivos.
7. Espera 2s entre arquivos.
8. Grava o MD consolidado na pasta de saída. Regex adiciona duas espaços antes de alternativas.
9. Se `GoogleDrive:AccessToken` existir, `GoogleDriveService` envia o MD por multipart ao Drive com `mimeType = application/vnd.google-apps.document`. Sem token, pula esta etapa.

## Arquivos
- `Program.cs`: orquestração inteira (config, loop, numeração, escrita, envio).
- `ArquivoScanner.cs` / `IArquivoScanner.cs`: descoberta de arquivos.
- `DocumentoEntrada.cs`: DTO (`Caminho`, `Nome`, `Extensao`, `TamanhoMB`).
- `ITranscritorIA.cs` / `GeminiService.cs`: chamada HTTP ao Gemini (`generateContent`, chave no header `x-goog-api-key`; resposta validada: erro em bloqueio/texto vazio, aviso se `finishReason` != STOP).
- `IEscritorDocumento.cs` / `EscritorDocumento.cs`: grava o arquivo.
- `GoogleDriveService.cs`: upload ao Drive com Bearer token.
- `appsettings.json`: `Gemini:ApiKey` vazia (valor real vai em `appsettings.Development.json`, não versionado).

## Configuração (não commitar)
- `appsettings.Development.json` com `Gemini:ApiKey` e, se usar Drive, `GoogleDrive:AccessToken`.
- Chave e token nunca no código nem no `appsettings.json` versionado.

## Convenções
- Código e comentários em português. Nomes de domínio em português.
- `Nullable` e `ImplicitUsings` ativos. Namespace `Transcritor`.
- Interfaces `I*` para serviços; implementação concreta por trás.

## Problemas conhecidos
- `GoogleDrive:AccessToken` é token de curta duração. Sem refresh, o envio para de funcionar após expirar. OAuth é o caminho certo.
- Sem OCR local nem cache por hash. Cada execução gasta API de novo.

## Comandos
- Build: `dotnet build Transcritor/Transcritor.csproj`
- Rodar: `dotnet run --project Transcritor/Transcritor.csproj`

## Estado
- Desenvolvimento em passos pequenos. Um passo validado e commitado por vez. Não avançar sem aprovação do usuário.
- Não há testes automatizados.
