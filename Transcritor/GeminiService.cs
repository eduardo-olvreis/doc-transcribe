using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Transcritor
{
    internal class GeminiService : ITranscritorIA
    {

        private readonly string apiKey;
        private readonly HttpClient client;

        public GeminiService(string apiKey, HttpClient client)
        {
            this.apiKey = apiKey;
            this.client = client;
        }

        public async Task<string> EnviarParaIAAsync(DocumentoEntrada documento)
        {
            var bytes = await File.ReadAllBytesAsync(documento.Caminho);
            string base64String = Convert.ToBase64String(bytes);
            string mimeType;

            switch (documento.Extensao.ToLower())
            {
                case ".pdf":
                    mimeType = "application/pdf";
                    break;
                case ".png":
                    mimeType = "image/png";
                    break;
                case ".jpg" or ".jpeg":
                    mimeType = "image/jpeg";
                    break;
                default:
                    throw new NotSupportedException("Extensão não suportada.");
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent";
            string prompt = """
                            Você é um assistente especialista em transcrição e digitalização de documentos acadêmicos e provas.

                            Sua tarefa é transcrever o conteúdo textual da imagem/PDF enviado seguindo estritamente estas regras:

                            1. CONTEÚDO PERMITIDO: Transcreva APENAS os enunciados das questões e suas respectivas alternativas (A, B, C, D, E).
                            2. OMITA DADOS PESSOAIS E RASURAS: Ignore completamente anotações feitas à mão, respostas marcadas/assinaladas, nomes de alunos, cabeçalhos de instituição, rasuras, gabaritos rabiscados ou qualquer elemento escrito manualmente.
                            3. CONTEÚDO VISUAL/IMAGENS: Se uma questão contiver uma imagem, gráfico, diagrama ou figura necessária para a resolução, transcreva o texto do enunciado e adicione a seguinte marcação em uma nova linha no local da imagem: **[ESTA QUESTÃO CONTÉM UMA IMAGEM]**.
                            3.1. QUESTÃO CORTADA: Se uma questão estiver cortada pela borda da página (começa sem enunciado completo, ou termina sem todas as alternativas), adicione a marcação **[QUESTÃO CORTADA]** em uma linha própria, colada à questão (SEM linha em branco entre a marcação e o texto): na primeira linha se faltar o início, na última linha se faltar o fim.
                            4. SEM NUMERAÇÃO: Não adicione números no início dos enunciados (ex: não escreva "1.", "Questão 1"). Apenas extraia o texto dos enunciados e alternativas separados por uma linha em branco.
                            5. FORMATO DE SAÍDA: Retorne apenas o texto transcrito em Markdown limpo, sem introduções, saudações ou explicações antes ou depois.
                            """;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new {text = prompt},
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64String
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 8192
                }
            };

            string json = JsonSerializer.Serialize(payload);

            int tentativasMaximas = 5;
            string? responseBody = null;

            for (int tentativa = 1; tentativa <= tentativasMaximas; tentativa++)
            {
                // Chave no header (não na URL), para não vazar em logs e mensagens de erro.
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-goog-api-key", this.apiKey);

                using var response = await this.client.SendAsync(request);
                string corpo = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    responseBody = corpo;
                    break;
                }

                if (((int)response.StatusCode == 503 || (int)response.StatusCode == 429) && tentativa < tentativasMaximas)
                {
                    int tempoEspera = tentativa * 4;
                    Console.WriteLine($"[AVISO] Servidor instável ou lotado ({response.StatusCode}). Tentativa {tentativa}/{tentativasMaximas}. Aguardando {tempoEspera}s...");
                    await Task.Delay(TimeSpan.FromSeconds(tempoEspera));
                    continue;
                }

                throw new HttpRequestException($"Falha na API ({response.StatusCode}): {corpo}");
            }

            if (responseBody is null)
            {
                throw new HttpRequestException("Falha na API: nenhuma resposta válida após as tentativas.");
            }

            return ExtrairTexto(responseBody, documento.Nome);
        }

        private static string ExtrairTexto(string responseBody, string nomeDocumento)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var raiz = doc.RootElement;

            if (!raiz.TryGetProperty("candidates", out var candidatos) || candidatos.GetArrayLength() == 0)
            {
                string motivo = raiz.TryGetProperty("promptFeedback", out var feedback) &&
                                feedback.TryGetProperty("blockReason", out var bloqueio)
                    ? bloqueio.GetString() ?? "desconhecido"
                    : "resposta sem candidatos";
                throw new InvalidOperationException($"A IA não retornou conteúdo para '{nomeDocumento}' ({motivo}).");
            }

            var candidato = candidatos[0];
            string finishReason = candidato.TryGetProperty("finishReason", out var fr) ? fr.GetString() ?? string.Empty : string.Empty;

            var texto = new StringBuilder();
            if (candidato.TryGetProperty("content", out var conteudo) && conteudo.TryGetProperty("parts", out var partes))
            {
                foreach (var parte in partes.EnumerateArray())
                {
                    if (parte.TryGetProperty("text", out var t))
                    {
                        texto.Append(t.GetString());
                    }
                }
            }

            if (texto.Length == 0)
            {
                throw new InvalidOperationException($"A IA retornou texto vazio para '{nomeDocumento}' (finishReason: {finishReason}).");
            }

            if (finishReason is not ("" or "STOP"))
            {
                Console.WriteLine($"[AVISO] '{nomeDocumento}' terminou com finishReason={finishReason}. A transcrição pode estar incompleta.");
            }

            return texto.ToString();
        }
    }
}
