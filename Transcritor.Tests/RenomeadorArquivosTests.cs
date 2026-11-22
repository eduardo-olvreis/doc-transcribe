using Transcritor;

namespace Transcritor.Tests;

public class RenomeadorArquivosTests : IDisposable
{
    private readonly string pasta = Path.Combine(Path.GetTempPath(), "transcritor_test_" + Guid.NewGuid().ToString("N"));
    private readonly RenomeadorArquivos renomeador = new();
    private readonly ArquivoScanner scanner = new();

    public RenomeadorArquivosTests() => Directory.CreateDirectory(pasta);

    public void Dispose() => Directory.Delete(pasta, recursive: true);

    private void Criar(params string[] nomes)
    {
        foreach (var n in nomes)
        {
            File.WriteAllText(Path.Combine(pasta, n), n);
        }
    }

    private List<DocumentoEntrada> Escanear() => scanner.EscanearPasta(pasta).ToList();

    private string[] Nomes() => Directory.GetFiles(pasta).Select(Path.GetFileName).Order().ToArray()!;

    [Fact]
    public void RenomeiaEmSequenciaMantendoExtensaoEConteudo()
    {
        Criar("IMG_9.jpg", "foto.png", "scan.pdf");

        int n = renomeador.Renomear(pasta, Escanear());

        Assert.Equal(3, n);
        Assert.Equal(["001.png", "002.jpg", "003.pdf", "mapa_nomes.txt"], Nomes());
        // ordem natural (ignora maiúsculas): foto.png, IMG_9.jpg, scan.pdf
        Assert.Equal("foto.png", File.ReadAllText(Path.Combine(pasta, "001.png")));
        Assert.Equal("scan.pdf", File.ReadAllText(Path.Combine(pasta, "003.pdf")));
    }

    [Fact]
    public void MapaGuardaNomeOriginal()
    {
        Criar("a.jpg", "b.jpg");

        renomeador.Renomear(pasta, Escanear());

        var mapa = File.ReadAllText(Path.Combine(pasta, "mapa_nomes.txt"));
        Assert.Contains("001.jpg <- a.jpg", mapa);
        Assert.Contains("002.jpg <- b.jpg", mapa);
    }

    [Fact]
    public void JaSequencial_NaoFazNada()
    {
        Criar("001.jpg", "002.jpg", "003.png");

        int n = renomeador.Renomear(pasta, Escanear());

        Assert.Equal(0, n);
        Assert.Equal(["001.jpg", "002.jpg", "003.png"], Nomes());
    }

    [Fact]
    public void RodarDuasVezes_SegundaNaoRenomeia()
    {
        Criar("x.jpg", "y.jpg");

        renomeador.Renomear(pasta, Escanear());
        int n = renomeador.Renomear(pasta, Escanear());

        Assert.Equal(0, n);
    }

    [Fact]
    public void DestinoQueEhNomeDeOutroArquivo_NaoColide()
    {
        // ordem natural: 002.jpg, 003.jpg, a.jpg -> 001, 002, 003 (002 e 003 são destino e origem ao mesmo tempo)
        Criar("002.jpg", "003.jpg", "a.jpg");

        renomeador.Renomear(pasta, Escanear());

        Assert.Equal(["001.jpg", "002.jpg", "003.jpg", "mapa_nomes.txt"], Nomes());
        Assert.Equal("002.jpg", File.ReadAllText(Path.Combine(pasta, "001.jpg")));
        Assert.Equal("003.jpg", File.ReadAllText(Path.Combine(pasta, "002.jpg")));
        Assert.Equal("a.jpg", File.ReadAllText(Path.Combine(pasta, "003.jpg")));
    }

    [Fact]
    public void ArquivoNovoEmPastaSequencial_RecebeProximoNumero()
    {
        Criar("001.jpg", "002.jpg", "nova.jpg");

        int n = renomeador.Renomear(pasta, Escanear());

        Assert.Equal(1, n);
        Assert.Equal(["001.jpg", "002.jpg", "003.jpg", "mapa_nomes.txt"], Nomes());
    }

    [Fact]
    public void IgnoraArquivosNaoSuportados()
    {
        Criar("a.jpg", "notas.txt");

        renomeador.Renomear(pasta, Escanear());

        Assert.Contains("notas.txt", Nomes());
        Assert.Contains("001.jpg", Nomes());
    }
}
