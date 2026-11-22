# Transcritor

App console .NET 10 que transcreve provas. Usuário joga arquivos numa pasta; o app gera um MD único e cria um Google Docs formatado no Drive.

## Fluxo (MVP)
1. `ArquivoScanner.EscanearPasta` lista `.pdf .png .jpg .jpeg .txt .docx` (ordem por nome).
2. `.txt` e `.docx`: texto extraído localmente (`ExtratorTextoLocal`), sem IA.
3. `.pdf` e imagens: `GeminiService` envia o arquivo (base64) e recebe a transcrição em Markdown.
4. Todas as transcrições viram `transcricao_yyyyMMdd_HHmmss.md` na própria pasta, separadas por `---`, cada uma com `## nome do arquivo`.
5. `GoogleDocsService` converte o MD em HTML (Markdig) e faz upload ao Drive como `application/vnd.google-apps.document`. Imprime a URL.

## Arquivos
- `Program.cs`: orquestra o fluxo. Pasta vem do 1º argumento ou do caminho padrão.
- `ArquivoScanner.cs` / `IArquivoScanner.cs`: descoberta de arquivos.
- `DocumentoEntrada.cs`: DTO do arquivo.
- `ITranscritorIA.cs` / `GeminiService.cs`: transcrição por IA. Modelo em `Modelo`.
- `ExtratorTextoLocal.cs`: `.txt` e `.docx` sem IA.
- `GoogleDocsService.cs`: OAuth desktop + Drive API, escopo `drive.file`.

## Configuração (não commitar)
- Variável de ambiente `GEMINI_API_KEY`. Nunca colocar chave no código.
- `credentials.json` (OAuth client tipo Desktop, Google Cloud) na pasta de execução.
- `token_drive/` gerado no 1º login. Ignorado pelo git.

## Regras do prompt de transcrição
- Só enunciados e alternativas A–E.
- Sem dados pessoais, sem rasuras, sem respostas marcadas, sem manuscritos.
- Questão com figura: marcar `**[ESTA QUESTÃO CONTÉM UMA IMAGEM]**`.
- Sem numeração no início dos enunciados.
- Saída só Markdown limpo.

## Convenções
- Código e comentários em português. Nomes de domínio em português (`EscanearPasta`, `DocumentoEntrada`).
- `Nullable` e `ImplicitUsings` ativos.
- Erro por arquivo não derruba o lote: `Program.cs` loga e segue.

## Comandos
- Build: `dotnet build`
- Rodar: `dotnet run -- "C:\caminho\pasta"`

## Estado / TODO
- Desenvolvimento em passos pequenos. Cada passo validado e commitado antes do próximo. Não avançar sem aprovação do usuário.
- Fluxo acima é a meta do MVP. Ainda não testado com arquivos reais nem com o Drive.
- Futuro: executável (publish), depois web. Discutir custo de API antes de escalar (cache por hash, limite por usuário, OCR local para documentos digitais).
