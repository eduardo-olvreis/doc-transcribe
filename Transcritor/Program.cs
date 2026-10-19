string caminho = @"C:\Users\eduar\Desktop\TranscreverProvas";

string[] extensoesPermitidas = [".pdf", ".png", ".jpeg", ".jpg", ".docx"];

IEnumerable<string> arquivos = Directory.EnumerateFiles(caminho)
    .Where(arquivo => extensoesPermitidas.Contains(Path.GetExtension(arquivo), StringComparer.OrdinalIgnoreCase));

foreach (string arquivo in arquivos)
{
    Console.WriteLine(arquivo);
}