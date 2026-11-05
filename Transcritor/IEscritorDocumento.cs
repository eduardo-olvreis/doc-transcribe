using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal interface IEscritorDocumento
    {
        Task SalvarAsync(string caminho, string conteudo);
    }
}
