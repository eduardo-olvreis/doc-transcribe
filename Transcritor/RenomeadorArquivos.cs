using System.Text;

namespace Transcritor
{
    internal class RenomeadorArquivos : IRenomeadorArquivos
    {
        public const string NomeMapa = "mapa_nomes.txt";

        public int Renomear(string pasta, IReadOnlyList<DocumentoEntrada> documentos)
        {
            int largura = Math.Max(3, documentos.Count.ToString().Length);

            var plano = documentos
                .Select((doc, i) => (
                    Doc: doc,
                    NomeNovo: (i + 1).ToString().PadLeft(largura, '0') + doc.Extensao))
                .Where(p => !string.Equals(p.Doc.Nome, p.NomeNovo, StringComparison.Ordinal))
                .ToList();

            if (plano.Count == 0)
            {
                return 0;
            }

            // Fase 1: nomes temporários, para não colidir com um destino que ainda é nome de outro arquivo.
            string prefixoTemp = $"__ren_{Guid.NewGuid():N}_";
            var temporarios = new List<(string Temp, string Final, string Original)>();
            foreach (var (doc, nomeNovo) in plano)
            {
                string temp = Path.Combine(pasta, prefixoTemp + nomeNovo);
                File.Move(doc.Caminho, temp);
                temporarios.Add((temp, Path.Combine(pasta, nomeNovo), doc.Nome));
            }

            // Fase 2: nomes finais.
            var mapa = new StringBuilder();
            mapa.AppendLine($"# {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            foreach (var (temp, final, original) in temporarios)
            {
                File.Move(temp, final);
                mapa.AppendLine($"{Path.GetFileName(final)} <- {original}");
            }
            mapa.AppendLine();

            // Acrescenta ao mapa existente: preserva o histórico de renomeações anteriores.
            File.AppendAllText(Path.Combine(pasta, NomeMapa), mapa.ToString(), Encoding.UTF8);

            return plano.Count;
        }
    }
}
