string caminho = @"C:\Users\eduar\Desktop\TranscreverProvas";

IEnumerable<string> arquivos = Directory.EnumerateFiles(caminho);
Console.WriteLine(arquivos);

foreach (string arquivo in arquivos)
{
    Console.WriteLine(arquivo);
}