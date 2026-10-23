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

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={this.apiKey}";
            string prompt = """
                            Você é um assistente especialista em transcrição e digitalização de documentos acadêmicos e provas.

                            Sua tarefa é transcrever o conteúdo textual da imagem/PDF enviado seguindo estritamente estas regras:

                            1. CONTEÚDO PERMITIDO: Transcreva APENAS os enunciados das questões e suas respectivas alternativas (A, B, C, D, E).
                            2. OMITA DADOS PESSOAIS E RASURAS: Ignore completamente anotações feitas à mão, respostas marcadas/assinaladas, nomes de alunos, cabeçalhos de instituição, rasuras, gabaritos rabiscados ou qualquer elemento escrito manualmente.
                            3. CONTEÚDO VISUAL/IMAGENS: Se uma questão contiver uma imagem, gráfico, diagrama ou figura necessária para a resolução, transcreva o texto do enunciado e adicione a seguinte marcação em uma nova linha no local da imagem: **[ESTA QUESTÃO CONTÉM UMA IMAGEM]**.
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
                }
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await this.client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
            return text;
        }
    }
}
