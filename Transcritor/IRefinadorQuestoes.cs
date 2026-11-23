namespace Transcritor
{
    /// <summary>Questão já formatada em Markdown e o arquivo de onde veio.</summary>
    internal record QuestaoExtraida(string Texto, string Arquivo);

    internal record OpcoesRefinamento(bool RemoverDissertativas, bool RemoverRepetidas, double LimiteSimilaridade);

    /// <param name="Questao">Questão removida.</param>
    /// <param name="Motivo">"dissertativa" ou "repetida".</param>
    /// <param name="IndiceMantida">Para repetida: índice (base 0) da questão mantida em <see cref="ResultadoRefinamento.Mantidas"/>; senão nulo.</param>
    /// <param name="Similaridade">Para repetida: similaridade (0 a 1) com a mantida.</param>
    internal record QuestaoRemovida(QuestaoExtraida Questao, string Motivo, int? IndiceMantida, double? Similaridade);

    internal record ResultadoRefinamento(IReadOnlyList<QuestaoExtraida> Mantidas, IReadOnlyList<QuestaoRemovida> Removidas);

    internal interface IRefinadorQuestoes
    {
        /// <summary>
        /// Remove questões dissertativas (sem alternativas nem itens) e repetidas (texto normalizado
        /// igual ou parecido), mantendo a primeira ocorrência. Questões marcadas como cortadas nunca
        /// são removidas como dissertativas.
        /// </summary>
        ResultadoRefinamento Refinar(IReadOnlyList<QuestaoExtraida> questoes, OpcoesRefinamento opcoes);
    }
}
