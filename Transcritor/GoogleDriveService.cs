using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Transcritor
{
    public class GoogleDriveService
    {
        private readonly string accessToken;
        private readonly HttpClient client;

        public GoogleDriveService(string accessToken, HttpClient client)
        {
            this.accessToken = accessToken;
            this.client = client;
        }

        public async Task EnviarParaGoogleDocsAsync(string caminhoArquivoMd, string nomeDocumentoDrive)
        {
            if (!File.Exists(caminhoArquivoMd))
            {
                throw new FileNotFoundException("O arquivo Markdown consolidado não foi encontrado.", caminhoArquivoMd);
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(caminhoArquivoMd);
            string url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);

            string boundary = "-------314159265358979323846";
            var multipartContent = new MultipartContent("related", boundary);

            var metadata = new
            {
                name = nomeDocumentoDrive,
                mimeType = "application/vnd.google-apps.document"
            };

            string jsonMetadata = JsonSerializer.Serialize(metadata);
            var jsonContent = new StringContent(jsonMetadata, Encoding.UTF8, "application/json");
            jsonContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=UTF-8");
            multipartContent.Add(jsonContent);

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/markdown");
            multipartContent.Add(fileContent);

            request.Content = multipartContent;

            HttpResponseMessage response = await this.client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro ao enviar para o Google Drive ({response.StatusCode}): {errorDetails}");
            }

            Console.WriteLine("Sucesso! O documento foi enviado e convertido para o Google Docs no seu Drive.");
        }
    }
}