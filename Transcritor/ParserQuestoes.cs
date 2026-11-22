using System.Text.RegularExpressions;

namespace Transcritor
{
    internal partial class ParserQuestoes : IParserQuestoes
    {
        // Alternativa no início do bloco/linha: "a)", "A)", "(a)", "a.", "a-".
        // Com "." ou "-" só minúscula, para não confundir com "E. coli".
        [GeneratedRegex(@"^(\([a-eA-E]\)|[a-eA-E]\)|[a-e][\.\-])\s*\S")]
        private static partial Regex InicioAlternativa();

        // Item de enumeração/associação: "1)", "1.", "( )" ou marcador de imagem.
        [GeneratedRegex(@"^(\d{1,2}[\)\.]\s|\(\s*\)|\[IMAGEM —)", RegexOptions.IgnoreCase)]
        private static partial Regex InicioItem();

        // Bloco que complementa uma questão de associar colunas ("Resposta correta:", "Assinale a alternativa...").
        [GeneratedRegex(@"^(resposta|alternativa|assinale)\b", RegexOptions.IgnoreCase)]
        private static partial Regex InicioRespostaAssociacao();

        [GeneratedRegex(@"^(\d{1,2}[\)\.]\s|\(\s*\))", RegexOptions.Multiline)]
        private static partial Regex LinhaDeItem();

        [GeneratedRegex(@"\n\s*\n")]
        private static partial Regex LinhaEmBranco();

        // Alternativas coladas na mesma linha: "a) x b) y c) z".
        [GeneratedRegex(@"\s+(?=\([b-eB-E]\)|[b-eB-E]\)\s|[b-e][\.\-]\s)")]
        private static partial Regex AlternativaInline();

        [GeneratedRegex(@"\s(\([bB]\)|[bB]\)|[bB][\.\-])\s")]
        private static partial Regex TemSegundaAlternativa();

        // Marcador de imagem da IA, com ou sem negrito.
        [GeneratedRegex(@"\*{0,2}\[ESTA QUESTÃO CONTÉM UMA IMAGEM\]\*{0,2}", RegexOptions.IgnoreCase)]
        private static partial Regex MarcadorImagemIA();

        // Marcador de questão cortada da IA, com ou sem negrito.
        [GeneratedRegex(@"\*{0,2}\[QUESTÃO CORTADA\]\*{0,2}", RegexOptions.IgnoreCase)]
        private static partial Regex MarcadorCortadaIA();

        public IReadOnlyList<string> Extrair(string textoIA, string nomeArquivo)
        {
            string marcadorCortada = $"[QUESTÃO CORTADA — {nomeArquivo}]";

            // Troca os marcadores da IA pelos finais, já com o arquivo de origem.
            textoIA = MarcadorImagemIA().Replace(textoIA, $"[IMAGEM — {nomeArquivo}]");
            textoIA = MarcadorCortadaIA().Replace(textoIA, marcadorCortada);

            var blocos = LinhaEmBranco()
                .Split(textoIA.Replace("\r\n", "\n"))
                .Select(b => b.Trim())
                .Where(b => b.Length > 0);

            var questoes = new List<string>();
            var blocosAtuais = new List<string>();

            foreach (var bloco in blocos)
            {
                if (blocosAtuais.Count > 0 && ContinuaQuestaoAtual(bloco, blocosAtuais))
                {
                    blocosAtuais.Add(bloco);
                    continue;
                }

                if (blocosAtuais.Count > 0)
                {
                    questoes.Add(Formatar(blocosAtuais));
                }

                // Heurística: questão que começa direto em alternativa veio cortada do arquivo anterior.
                blocosAtuais = InicioAlternativa().IsMatch(bloco) && !bloco.Contains("[QUESTÃO CORTADA")
                    ? [marcadorCortada, bloco]
                    : [bloco];
            }

            if (blocosAtuais.Count > 0)
            {
                questoes.Add(Formatar(blocosAtuais));
            }

            return questoes;
        }

        private static bool ContinuaQuestaoAtual(string bloco, List<string> blocosAtuais)
        {
            if (InicioAlternativa().IsMatch(bloco) || InicioItem().IsMatch(bloco))
            {
                return true;
            }

            bool atualTemItens = LinhaDeItem().IsMatch(string.Join("\n", blocosAtuais));
            return atualTemItens && InicioRespostaAssociacao().IsMatch(bloco);
        }

        // Uma linha por quebra rígida de Markdown ("  \n"), alternativas coladas separadas.
        private static string Formatar(List<string> blocos)
        {
            var linhas = blocos
                .SelectMany(b => b.Split('\n'))
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .SelectMany(SepararAlternativasInline);

            return string.Join("  \n", linhas);
        }

        private static IEnumerable<string> SepararAlternativasInline(string linha)
        {
            bool comecaComA = Regex.IsMatch(linha, @"^(\([aA]\)|[aA]\)|a[\.\-])");
            if (comecaComA && TemSegundaAlternativa().IsMatch(linha))
            {
                return AlternativaInline().Split(linha);
            }
            return [linha];
        }
    }
}
