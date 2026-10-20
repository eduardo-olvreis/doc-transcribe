using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal class ArquivoScanner : IArquivoScanner
    {
        private static string[] extensoesPermitidas = [".pdf", ".png", ".jpeg", ".jpg", ".docx"];
       
        public IEnumerable<DocumentoEntrada> EscanearPasta(string caminho)
        {
            return Directory.EnumerateFiles(caminho)
                .Where(arquivo => extensoesPermitidas.Contains(Path.GetExtension(arquivo), StringComparer.OrdinalIgnoreCase))
                .Select(arquivo => new DocumentoEntrada
                {
                    Caminho = arquivo,
                    Nome = Path.GetFileName(arquivo),
                    Extensao = Path.GetExtension(arquivo),
                    TamanhoMB = new FileInfo(arquivo).Length / (1024.0 * 1024.0),
                });
        }

    }
}
