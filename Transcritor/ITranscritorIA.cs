using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal interface ITranscritorIA
    {
        Task<string> EnviarParaIAAsync(DocumentoEntrada documento);
    }
}
