namespace Transcritor
{
    internal interface IParserQuestoes
    {
        /// <summary>
        /// Divide o texto transcrito pela IA em questões completas (enunciado + alternativas/itens),
        /// já formatadas em Markdown, sem numeração. Marcadores de imagem e de questão cortada
        /// recebem o nome do arquivo de origem.
        /// </summary>
        IReadOnlyList<string> Extrair(string textoIA, string nomeArquivo);
    }
}
