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

public class ParserQuestoesFormatosTests
{
    private readonly ParserQuestoes parser = new();

    [Fact]
    public void AlternativasMaiusculasComPonto_EmBlocoSeparado_FicamNaQuestao()
    {
        var texto = "Sobre neoplasias do sistema hematopoético, assinale a alternativa verdadeira:\n\nA. os linfomas são neoplasias policlonais.\nB. a maioria dos linfomas são de células T.\nC. o linfoma de hodgkin tem prognóstico sombrio.";

        var q = Assert.Single(parser.Extrair(texto, "003.jpg"));

        Assert.StartsWith("Sobre neoplasias", q);
        Assert.Contains("  \nA. os linfomas", q);
    }

    [Fact]
    public void AlternativasMaiusculasComPonto_UmaPorBloco_FicamNaQuestao()
    {
        var texto = "Qual a patologia?\n\nA. Osteoporose\n\nB. Osteogênese Imperfeita\n\nC. Osteopetrose";

        Assert.Single(parser.Extrair(texto, "007.jpg"));
    }

    [Fact]
    public void ItensRomanos_EmBlocoSeparado_FicamNaQuestao()
    {
        var texto = "Sobre a metaplasia, podemos dizer que é um processo:\n\nI - Em que o epitélio reage.\nII - Em que há células escamosas.\nIII - Em que há maior chance.\n\nQuais estão corretas?\na) Apenas I\nb) Apenas II";

        var q = Assert.Single(parser.Extrair(texto, "003.jpg"));

        Assert.Contains("Quais estão corretas?", q);
    }

    [Fact]
    public void ItensNumeradosComTraco_FicamNaQuestao()
    {
        var texto = "Relacione os casos:\n\n1- Mulher de 70 anos\n2- Mulher no menacme\n\nA. Atrofia\nB. Normal";

        Assert.Single(parser.Extrair(texto, "025.pdf"));
    }

    [Fact]
    public void LetraEPontoSemSequencia_ComecaNovaQuestao()
    {
        // "E. coli" não é alternativa: nenhuma letra anterior apareceu.
        var texto = "Qual é a capital?\na) Rio\nb) Brasília\n\nE. coli é uma bactéria?\na) sim\nb) não";

        Assert.Equal(2, parser.Extrair(texto, "p.jpg").Count);
    }

    [Fact]
    public void QuestaoInteiraComMaiusculas_NaoQuebra()
    {
        var texto = "Qual o diagnóstico?\nA. Herpes\nB. Dermatite\nC. Candidíase\n\nOutra pergunta?\nA. sim\nB. não";

        Assert.Equal(2, parser.Extrair(texto, "004.jpg").Count);
    }
}
