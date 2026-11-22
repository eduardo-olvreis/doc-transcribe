using System;
using System.Collections.Generic;
using System.Text;

namespace Transcritor
{
    internal class ArquivoScanner : IArquivoScanner
    {
        private static string[] extensoesPermitidas = [".pdf", ".png", ".jpeg", ".jpg"];
        private static readonly StringComparer comparadorNatural =
            StringComparer.Create(System.Globalization.CultureInfo.InvariantCulture, System.Globalization.CompareOptions.NumericOrdering | System.Globalization.CompareOptions.IgnoreCase);

        public IEnumerable<DocumentoEntrada> EscanearPasta(string caminho)
        {
            if (!Directory.Exists(caminho))
            {
                throw new DirectoryNotFoundException($"Pasta de entrada não encontrada: {caminho}");
            }

            return Directory.EnumerateFiles(caminho)
                .Where(arquivo => extensoesPermitidas.Contains(Path.GetExtension(arquivo), StringComparer.OrdinalIgnoreCase))
                .OrderBy(arquivo => Path.GetFileName(arquivo), comparadorNatural)
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
