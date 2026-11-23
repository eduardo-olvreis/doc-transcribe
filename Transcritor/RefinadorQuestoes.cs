using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Transcritor
{
    internal partial class RefinadorQuestoes : IRefinadorQuestoes
    {
        // Marcadores com nome de arquivo ([IMAGEM — x], [QUESTÃO CORTADA — x]) não contam na comparação.
        [GeneratedRegex(@"\[(IMAGEM|QUESTÃO CORTADA)[^\]]*\]")]
        private static partial Regex Marcador();

        [GeneratedRegex(@"[^a-z0-9\s]")]
        private static partial Regex NaoAlfanumerico();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Espacos();

        public ResultadoRefinamento Refinar(IReadOnlyList<QuestaoExtraida> questoes, OpcoesRefinamento opcoes)
        {
            var mantidas = new List<QuestaoExtraida>();
            var assinaturas = new List<HashSet<string>>(); // uma por questão mantida
            var removidas = new List<QuestaoRemovida>();

            foreach (var questao in questoes)
            {
                if (opcoes.RemoverDissertativas && EhDissertativa(questao.Texto))
                {
                    removidas.Add(new QuestaoRemovida(questao, "dissertativa", null, null));
                    continue;
                }

                if (opcoes.RemoverRepetidas)
                {
                    var assinatura = Assinatura(questao.Texto);
                    var (indice, similaridade) = BuscarParecida(assinatura, assinaturas, opcoes.LimiteSimilaridade);
                    if (indice >= 0)
                    {
                        removidas.Add(new QuestaoRemovida(questao, "repetida", indice, similaridade));
                        continue;
                    }
                    assinaturas.Add(assinatura);
                }
                else
                {
                    assinaturas.Add([]);
                }

                mantidas.Add(questao);
            }

            return new ResultadoRefinamento(mantidas, removidas);
        }

        // Dissertativa = exige resposta escrita. Na dúvida, mantém: só é removida se não tiver alternativas,
        // itens/associação nem enunciado de escolha ("assinale", "marque", "V ou F"...).
        // Questão cortada pode só estar sem as alternativas: fica.
        internal static bool EhDissertativa(string texto)
        {
            if (texto.Contains("[QUESTÃO CORTADA", StringComparison.Ordinal))
            {
                return false;
            }
            return !PadroesQuestao.TemAlternativas(texto)
                && !PadroesQuestao.TemItens(texto)
                && !PadroesQuestao.PedeEscolha(texto);
        }

        private static (int Indice, double Similaridade) BuscarParecida(HashSet<string> assinatura, List<HashSet<string>> mantidas, double limite)
        {
            if (assinatura.Count == 0)
            {
                return (-1, 0);
            }

            for (int i = 0; i < mantidas.Count; i++)
            {
                double s = Jaccard(assinatura, mantidas[i]);
                if (s >= limite)
                {
                    return (i, s);
                }
            }
            return (-1, 0);
        }

        // Conjunto de bigramas de palavras do texto normalizado (enunciado + alternativas).
        internal static HashSet<string> Assinatura(string texto)
        {
            string normalizado = Normalizar(texto);
            var palavras = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var conjunto = new HashSet<string>();

            if (palavras.Length == 1)
            {
                conjunto.Add(palavras[0]);
            }
            for (int i = 0; i + 1 < palavras.Length; i++)
            {
                conjunto.Add(palavras[i] + " " + palavras[i + 1]);
            }
            return conjunto;
        }

        internal static string Normalizar(string texto)
        {
            texto = Marcador().Replace(texto, " ");

            // Remove acentos.
            var decomposto = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposto.Length);
            foreach (char c in decomposto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string baixo = sb.ToString().ToLowerInvariant();
            baixo = NaoAlfanumerico().Replace(baixo, " ");
            return Espacos().Replace(baixo, " ").Trim();
        }

        internal static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return 1;
            }
            int comum = a.Count < b.Count ? a.Count(b.Contains) : b.Count(a.Contains);
            int uniao = a.Count + b.Count - comum;
            return uniao == 0 ? 0 : (double)comum / uniao;
        }
    }
}
