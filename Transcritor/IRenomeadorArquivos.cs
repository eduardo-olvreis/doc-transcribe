namespace Transcritor
{
    internal interface IRenomeadorArquivos
    {
        /// <summary>
        /// Renomeia os documentos para 001, 002, 003... (mantendo a extensão) na ordem recebida
        /// e registra o nome original em <c>mapa_nomes.txt</c> na mesma pasta.
        /// Não faz nada se os nomes já forem sequenciais. Retorna a quantidade de arquivos renomeados.
        /// </summary>
        int Renomear(string pasta, IReadOnlyList<DocumentoEntrada> documentos);
    }
}
