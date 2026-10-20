using Transcritor;

string caminho = @"C:\Users\eduar\Desktop\TranscreverProvas";

IArquivoScanner scanner = new ArquivoScanner();
IEnumerable<DocumentoEntrada> documentos = scanner.EscanearPasta(caminho);

foreach (var documento in documentos)
{
    Console.WriteLine($"{documento.Caminho}\n{documento.Nome}\n{documento.TamanhoMB:F2}\n=-=-=-=-=-=-=-=-=-=-=\n");
}