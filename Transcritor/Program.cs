string caminho = @"C:\Users\eduar\Desktop\TranscreverProvas";

string[] extensoesPermitidas = [".pdf", ".png", ".jpeg", ".jpg", ".docx"];

IEnumerable<FileInfo> arquivos = Directory.EnumerateFiles(caminho)
    .Where(arquivo => extensoesPermitidas.Contains(Path.GetExtension(arquivo), StringComparer.OrdinalIgnoreCase))
    .Select(caminho => new FileInfo(caminho));

foreach (FileInfo arquivo in arquivos)
{
    Console.WriteLine(arquivo.Name);

    double tamanoEmMb = arquivo.Length / (1024.0 * 1024.0);
    Console.WriteLine($"{tamanoEmMb:F2} MB");
}