using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal class EscritorDocumento : IEscritorDocumento
    {
        public async Task SalvarAsync(string caminho, string conteudo)
        {
            await File.WriteAllTextAsync(caminho, conteudo);
        }
    }
}
