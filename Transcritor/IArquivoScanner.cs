using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal interface IArquivoScanner
    {
        IEnumerable<DocumentoEntrada> EscanearPasta(string caminhoPasta);
    }
}
