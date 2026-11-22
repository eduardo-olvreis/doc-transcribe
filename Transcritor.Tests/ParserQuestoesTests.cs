using Transcritor;

namespace Transcritor.Tests;

public class ParserQuestoesTests
{
    private readonly ParserQuestoes parser = new();

    [Fact]
    public void MarcadorImagem_ViraMarcadorComArquivo()
    {
        var texto = "Observe a figura.\n**[ESTA QUESTÃO CONTÉM UMA IMAGEM]**\na) um\nb) dois";

        var questoes = parser.Extrair(texto, "prova1.jpg");

        var q = Assert.Single(questoes);
        Assert.Contains("[IMAGEM — prova1.jpg]", q);
        Assert.DoesNotContain("ESTA QUESTÃO", q);
        Assert.DoesNotContain("**", q);
    }

    [Fact]
    public void MarcadorImagem_EmBlocoProprio_ContinuaNaMesmaQuestao()
    {
        var texto = "Observe a figura.\n\n[ESTA QUESTÃO CONTÉM UMA IMAGEM]\n\na) um\nb) dois";

        var questoes = parser.Extrair(texto, "p.png");

        Assert.Single(questoes);
    }

    [Fact]
    public void MarcadorCortadaDaIA_ViraMarcadorComArquivo()
    {
        var texto = "**[QUESTÃO CORTADA]**\nQual é a capital?\na) Rio\nb) Brasília";

        var questoes = parser.Extrair(texto, "p2.jpg");

        var q = Assert.Single(questoes);
        Assert.Contains("[QUESTÃO CORTADA — p2.jpg]", q);
        Assert.DoesNotContain("**", q);
    }

    [Fact]
    public void MarcadorCortadaNoFim_FicaNaMesmaQuestao()
    {
        var texto = "Qual é a capital?\na) Rio\nb) Brasília\n**[QUESTÃO CORTADA]**";

        var questoes = parser.Extrair(texto, "p2.jpg");

        var q = Assert.Single(questoes);
        Assert.EndsWith("[QUESTÃO CORTADA — p2.jpg]", q);
    }

    [Fact]
    public void QuestaoQueComecaEmAlternativa_ViraCortada()
    {
        var texto = "c) terceira\nd) quarta\n\nQual é a capital?\na) Rio\nb) Brasília";

        var questoes = parser.Extrair(texto, "p3.jpg");

        Assert.Equal(2, questoes.Count);
        Assert.StartsWith("[QUESTÃO CORTADA — p3.jpg]", questoes[0]);
        Assert.DoesNotContain("CORTADA", questoes[1]);
    }

    [Fact]
    public void QuestaoJaMarcadaComecandoEmAlternativa_NaoDuplicaMarcador()
    {
        var texto = "**[QUESTÃO CORTADA]**\nc) terceira\nd) quarta";

        var questoes = parser.Extrair(texto, "p4.jpg");

        var q = Assert.Single(questoes);
        Assert.Equal(1, q.Split("QUESTÃO CORTADA").Length - 1);
    }

    [Fact]
    public void QuestaoNormal_SemMarcadores()
    {
        var texto = "Qual é a capital?\na) Rio\nb) Brasília\n\nOutra pergunta?\na) sim\nb) não";

        var questoes = parser.Extrair(texto, "p5.jpg");

        Assert.Equal(2, questoes.Count);
        Assert.All(questoes, q => Assert.DoesNotContain("CORTADA", q));
        Assert.All(questoes, q => Assert.DoesNotContain("IMAGEM", q));
    }

    [Fact]
    public void AlternativasColadas_SaoSeparadas()
    {
        var texto = "Pergunta?\na) x b) y c) z";

        var q = Assert.Single(parser.Extrair(texto, "p6.jpg"));

        Assert.Equal("Pergunta?  \na) x  \nb) y  \nc) z", q);
    }

    [Fact]
    public void Associacao_AgrupaItensEResposta()
    {
        var texto = "Associe as colunas.\n\n1) alfa\n\n2) beta\n\nResposta correta: 1-b, 2-a";

        var questoes = parser.Extrair(texto, "p7.jpg");

        Assert.Single(questoes);
    }
}
