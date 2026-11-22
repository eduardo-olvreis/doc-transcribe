# Transcritor

App console .NET 10 (`Transcritor/Transcritor.csproj`, solução `Transcritor.slnx`). Lê provas (PDF e imagens) de uma pasta, transcreve com a API do Gemini e gera um Markdown consolidado. Depois envia o MD ao Google Drive, convertido em Google Docs.

## Fluxo atual (`Program.cs`)
1. Lê `Gemini:ApiKey` de `appsettings.json` + `appsettings.Development.json`. Sem chave, encerra.
2. Pasta de entrada: `args[0]` ou `Transcricao:PastaEntrada`. Saída: `Transcricao:PastaSaida` (vazio = pasta de entrada), arquivo `Transcricao:NomeArquivoSaida`. Pasta inexistente encerra com erro.
3. `ArquivoScanner.EscanearPasta` lista `.pdf .png .jpg .jpeg` em ordem natural de nome.
4. Para cada arquivo: `GeminiService.EnviarParaIAAsync` (modelo `gemini-3.1-flash-lite`, temperature 0.1, até 5 tentativas em 429/503 com espera crescente).
5. `ParserQuestoes.Extrair` divide a resposta em questões: blocos por linha em branco; alternativas (`a)`, `A)`, `(a)`, `a.`, até `e`), itens de associação (`1)`, `1.`, `( )`), marcador de imagem e bloco "Resposta correta:" após itens são agrupados à questão anterior. Alternativas coladas na mesma linha são separadas. Cada linha da questão vira quebra rígida Markdown (dois espaços + `\n`).
6. Numeração global sequencial (`1 - `, `2 - `...) entre todos os arquivos.
7. Espera 2s entre arquivos.
8. Grava o MD consolidado na pasta de saída.
9. Se `GoogleDrive:AccessToken` existir, `GoogleDriveService` envia o MD por multipart ao Drive com `mimeType = application/vnd.google-apps.document`. Sem token, pula esta etapa.

## Arquivos
- `Program.cs`: orquestração (config, loop, numeração global, escrita, envio).
- `IParserQuestoes.cs` / `ParserQuestoes.cs`: agrupa blocos da IA em questões e formata em Markdown.
- `ArquivoScanner.cs` / `IArquivoScanner.cs`: descoberta de arquivos.
- `RenomeadorArquivos.cs` / `IRenomeadorArquivos.cs`: renomeia entrada para 001, 002... e grava `mapa_nomes.txt`.
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
- Testes: `Transcritor.Tests` (xUnit, só `ParserQuestoes`). Rodar: `dotnet test Transcritor.slnx`.

## Roadmap de melhorias (uma por vez, commit por melhoria)
Feitas:
1. ✅ Config de pastas, nome do arquivo/doc e ordenação natural.
2. ✅ `GeminiService` robusto (header, `finishReason`, parse seguro). Testado com chave real.
4. ✅ Parser de questões (`ParserQuestoes`): `a.`, `A)`, `E`, associação de colunas. Testado com Gemini real (3 imagens → 8 questões certas).

Próximas, na ordem sugerida:
- 4b. ✅ Testado com Gemini real e Drive. Marcar questão cortada e arquivo de origem. Parser devolve questão + nome do arquivo. Marcador de imagem vira `[IMAGEM — arquivo.jpg]`. Questão cortada: pedir no prompt do Gemini `**[QUESTÃO CORTADA]**` (início ou fim da página) e trocar por `[QUESTÃO CORTADA — arquivo.jpg]`; somar heurística (questão que começa direto em alternativa, como a nº 1 do teste). Mudar o prompt exige retestar com imagens reais.
- 3. ✅ Resiliência: `try/catch` por arquivo em `Program.cs`; salva `.md` parcial e lista arquivos que falharam no console. Drive é enviado mesmo com falhas (aviso). Testado manualmente.
- 7. ⏳ Ler `appsettings*.json` de `AppContext.BaseDirectory` (hoje usa `Directory.GetCurrentDirectory()`; só roda de dentro da pasta do projeto, senão `FileNotFoundException`).
- 5. ⏳ Drive com OAuth e refresh do token (hoje token do OAuth Playground dura ~1h; erro 401 = expirou).
- 6. ⏳ Cache por hash do arquivo (não gastar API de novo).
- 9. ✅ Renomear arquivos da pasta de entrada para `001`, `002`... antes de transcrever (`RenomeadorArquivos`; mapa em `mapa_nomes.txt`; flag `Transcricao:RenomearArquivos`, padrão true; idempotente). Falta teste manual.
- Flag `GoogleDrive:Enviar` (padrão true): false pula o envio ao Drive.
- `GeminiService` retenta `finishReason=RECITATION` até 3 vezes (temperature 0.1, 0.5, 0.8); falhando, o arquivo entra na lista de falhas. Falta teste manual.
- 8. ⏳ Juntar questões cortadas entre arquivos (arquivos sem ordem, podem estar distantes; junção por conteúdo, não por vizinhança). Plano: (a) prompt com dois marcadores, `[CORTADA NO FIM]` e `[CORTADA NO INÍCIO]`; (b) parser separa os pedaços órfãos (inclui os que começam em alternativa); (c) uma chamada extra ao Gemini só com texto, com os órfãos numerados, devolve JSON com os pares que se completam; (d) questão unida entra na posição do pedaço do início; (e) órfão sem par mantém `[QUESTÃO CORTADA — arquivo]`; avisar quando o pareamento for incerto. Risco: pareamento errado com muitos órfãos parecidos. Exige testes unitários do parser e teste real com Gemini.

Melhorias menores / dívida técnica (sem ordem):
- Console com acentos quebrados (`�`): definir `Console.OutputEncoding = Encoding.UTF8`.
- Warnings de nullable: `DocumentoEntrada` (propriedades não inicializadas), `Program.cs` (`googleToken` deve ser `string?`).
- `Task.Delay(2000)` fixo no código; mover para config. (`maxOutputTokens` já vem de `Gemini:MaxOutputTokens`, padrão 65536.)
- Arquivos acima de ~20MB não cabem em `inline_data` do Gemini; avisar ou usar a File API.
- `GoogleDriveService`: não descarta a resposta (`Dispose`) e não renova token (ver item 5).
- Prompt do Gemini diz "sem numeração"; se a IA numerar mesmo assim, o parser trata `1.`/`1)` como item da questão anterior, não como questão nova.
