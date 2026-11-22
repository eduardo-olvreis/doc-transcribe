namespace Transcritor
{
    internal interface IParserQuestoes
    {
        /// <summary>
        /// Divide o texto transcrito pela IA em questões completas (enunciado + alternativas/itens),
        /// já formatadas em Markdown, sem numeração.
        /// </summary>
        IReadOnlyList<string> Extrair(string textoIA);
    }
}
