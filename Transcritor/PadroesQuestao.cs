using System.Text.RegularExpressions;

namespace Transcritor
{
    /// <summary>
    /// Padrões de alternativa e de item usados pelo <see cref="ParserQuestoes"/> e pelo
    /// <see cref="RefinadorQuestoes"/>, para os dois sempre concordarem sobre o que é uma questão objetiva.
    /// </summary>
    internal static partial class PadroesQuestao
    {
        // Alternativa no início do bloco/linha: "a)", "A)", "(a)", "a.", "a-".
        // Com "." ou "-" só minúscula, para não confundir com "E. coli".
        private const string Alternativa = @"^(\([a-eA-E]\)|[a-eA-E]\)|[a-e][\.\-])\s*\S";

        [GeneratedRegex(Alternativa)]
        private static partial Regex InicioAlternativaSimples();

        [GeneratedRegex(Alternativa, RegexOptions.Multiline)]
        private static partial Regex LinhaAlternativaSimples();

        // "A. texto" (maiúscula com ponto): só vale em sequência (ver ComecaComAlternativaMaiuscula).
        [GeneratedRegex(@"^([A-E])\.\s*\S")]
        private static partial Regex InicioAlternativaMaiuscula();

        [GeneratedRegex(@"^A\.\s*\S", RegexOptions.Multiline)]
        private static partial Regex LinhaLetraA();

        [GeneratedRegex(@"^B\.\s*\S", RegexOptions.Multiline)]
        private static partial Regex LinhaLetraB();

        // Item de enumeração/associação: "1)", "1.", "1-", "I.", "II -", "( )" ou marcador de imagem.
        private const string Item = @"^(\d{1,2}\s*[\)\.\-–]\s|\(\s*\)|(?:I{1,3}|IV|VI{0,3}|IX|X)\s*[\.\)\-–—]\s)";

        [GeneratedRegex(Item + @"|^\[IMAGEM —")]
        private static partial Regex InicioItemOuImagem();

        [GeneratedRegex(Item, RegexOptions.Multiline)]
        private static partial Regex LinhaItem();

        // Enunciado que pede escolha/julgamento: nunca é resposta escrita.
        [GeneratedRegex(@"\b(assinale|marque|alternativas?|julgue|V ou F|verdadeir\w*|fals[oa]s?|corret\w*|incorret\w*|assertivas?|afirmativas?|afirma[çc][õo]es|afirma[çc][ãa]o)\b", RegexOptions.IgnoreCase)]
        private static partial Regex EnunciadoDeEscolha();

        /// <summary>Bloco que começa como alternativa ("a)", "A)", "(a)", "a." ou "A." em sequência).</summary>
        public static bool ComecaComAlternativa(string bloco, string textoAtual)
        {
            return InicioAlternativaSimples().IsMatch(bloco) || ComecaComAlternativaMaiuscula(bloco, textoAtual);
        }

        // "A." sempre (questão nunca começa assim); "B."-"E." só se a letra anterior já apareceu na questão
        // atual (evita confundir "E. coli" com alternativa).
        private static bool ComecaComAlternativaMaiuscula(string bloco, string textoAtual)
        {
            var m = InicioAlternativaMaiuscula().Match(bloco);
            if (!m.Success)
            {
                return false;
            }

            char letra = m.Groups[1].Value[0];
            if (letra == 'A')
            {
                return true;
            }

            char anterior = (char)(letra - 1);
            return Regex.IsMatch(textoAtual, $@"^{anterior}\.\s*\S", RegexOptions.Multiline);
        }

        public static bool ComecaComItem(string bloco) => InicioItemOuImagem().IsMatch(bloco);

        public static bool TemItens(string texto) => LinhaItem().IsMatch(texto);

        /// <summary>Texto com alternativas de qualquer estilo, inclusive "A." e "B." maiúsculos.</summary>
        public static bool TemAlternativas(string texto)
        {
            return LinhaAlternativaSimples().IsMatch(texto) || (LinhaLetraA().IsMatch(texto) && LinhaLetraB().IsMatch(texto));
        }

        public static bool PedeEscolha(string texto) => EnunciadoDeEscolha().IsMatch(texto);
    }
}
