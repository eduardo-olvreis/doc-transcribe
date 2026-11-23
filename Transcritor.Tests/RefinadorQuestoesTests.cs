using Transcritor;

namespace Transcritor.Tests;

public class RefinadorQuestoesTests
{
    private readonly RefinadorQuestoes refinador = new();
    private static readonly OpcoesRefinamento Padrao = new(true, true, 0.9);

    private static QuestaoExtraida Q(string texto, string arquivo = "001.jpg") => new(texto, arquivo);

    private const string Capital =
        "Qual é a capital do Brasil?  \na) Rio de Janeiro  \nb) Brasília  \nc) São Paulo  \nd) Salvador";

    [Fact]
    public void RemoveDissertativa()
    {
        var r = refinador.Refinar([Q("Explique a fisiopatologia da asma e suas complicações."), Q(Capital)], Padrao);

        Assert.Single(r.Mantidas);
        var rem = Assert.Single(r.Removidas);
        Assert.Equal("dissertativa", rem.Motivo);
    }

    [Fact]
    public void QuestaoCortadaSemAlternativas_NaoEhDissertativa()
    {
        var r = refinador.Refinar([Q("RN cinco dias de vida apresenta lesões. O diagnóstico é: [QUESTÃO CORTADA — 004.jpg]")], Padrao);

        Assert.Single(r.Mantidas);
        Assert.Empty(r.Removidas);
    }

    [Fact]
    public void Associacao_NaoEhDissertativa()
    {
        var r = refinador.Refinar([Q("Associe as colunas:  \n1. Bossa  \n2. Cefalo  \n( ) x  \n( ) y")], Padrao);

        Assert.Single(r.Mantidas);
    }

    [Fact]
    public void RemoveRepetidaExata_MantemPrimeira()
    {
        var r = refinador.Refinar([Q(Capital, "001.jpg"), Q(Capital, "002.jpg")], Padrao);

        Assert.Equal("001.jpg", Assert.Single(r.Mantidas).Arquivo);
        var rem = Assert.Single(r.Removidas);
        Assert.Equal("repetida", rem.Motivo);
        Assert.Equal(0, rem.IndiceMantida);
        Assert.Equal("002.jpg", rem.Questao.Arquivo);
    }

    [Fact]
    public void RepetidaIgnoraAcentoMaiusculaPontuacaoEMarcadores()
    {
        var parecida = "QUAL E A CAPITAL DO BRASIL  \nA) Rio de Janeiro  \nB) Brasilia  \nC) Sao Paulo  \nD) Salvador  \n[IMAGEM — 002.jpg]";

        var r = refinador.Refinar([Q(Capital), Q(parecida, "002.jpg")], Padrao);

        Assert.Single(r.Mantidas);
        Assert.Single(r.Removidas);
    }

    [Fact]
    public void RepetidaComPequenoRuidoDeOcr_EhRemovida()
    {
        var enunciado = "Paciente de 5 anos com febre alta há três dias, tosse produtiva e taquipneia, apresenta estertores em base direita ao exame físico e radiografia com consolidação lobar. Qual o diagnóstico mais provável e a conduta inicial adequada?";
        var a = $"{enunciado}  \na) Pneumonia bacteriana  \nb) Bronquiolite  \nc) Asma  \nd) Laringite";
        var b = a.Replace("estertores", "estertorss");

        var r = refinador.Refinar([Q(a), Q(b, "002.jpg")], Padrao);

        Assert.Single(r.Mantidas);
    }

    [Fact]
    public void QuestoesDiferentes_SaoMantidas()
    {
        var outra = "Qual é a capital da Argentina?  \na) Buenos Aires  \nb) Córdoba  \nc) Rosário  \nd) Mendoza";

        var r = refinador.Refinar([Q(Capital), Q(outra)], Padrao);

        Assert.Equal(2, r.Mantidas.Count);
        Assert.Empty(r.Removidas);
    }

    [Fact]
    public void FlagsDesligadas_NaoRemovemNada()
    {
        var r = refinador.Refinar(
            [Q(Capital), Q(Capital, "002.jpg"), Q("Explique a asma em detalhes.")],
            new OpcoesRefinamento(false, false, 0.9));

        Assert.Equal(3, r.Mantidas.Count);
        Assert.Empty(r.Removidas);
    }

    [Fact]
    public void SoRepetidasLigado_MantemDissertativa()
    {
        var r = refinador.Refinar(
            [Q(Capital), Q(Capital, "002.jpg"), Q("Explique a asma em detalhes.")],
            new OpcoesRefinamento(false, true, 0.9));

        Assert.Equal(2, r.Mantidas.Count);
        Assert.Equal("repetida", Assert.Single(r.Removidas).Motivo);
    }

    [Fact]
    public void IndiceMantida_ApontaParaPosicaoNaListaFinal()
    {
        var outra = "Qual é a capital da Argentina?  \na) Buenos Aires  \nb) Córdoba  \nc) Rosário  \nd) Mendoza";

        var r = refinador.Refinar([Q("Explique a asma."), Q(outra), Q(Capital), Q(Capital, "003.jpg")], Padrao);

        Assert.Equal(2, r.Mantidas.Count);
        var rep = Assert.Single(r.Removidas, x => x.Motivo == "repetida");
        Assert.Equal(1, rep.IndiceMantida); // Capital é a 2ª mantida
    }
}

public class RefinadorDissertativasTests
{
    private readonly RefinadorQuestoes refinador = new();
    private static readonly OpcoesRefinamento Padrao = new(true, false, 0.9);

    [Theory]
    [InlineData("Paciente de 67 anos com dor em queimação. Como hipótese diagnóstica provável temos?  \nA. Herpes-zóster  \nB. Dermatite  \nC. Candidíase  \nD. Raiva  \nE. Fratura")]
    [InlineData("Assinale a correta alternativa correspondente.  \nA. Todas corretas.  \nB. Somente a I.")]
    [InlineData("Relacione:  \n1- Mulher de 70 anos  \n2- Mulher no menacme")]
    [InlineData("Em relação ao carcinoma de vulva, considere as assertivas abaixo:  \nI. O diagnóstico tardio é incomum.  \nII. Prurido é comum.")]
    [InlineData("Sobre os divertículos do trato gastrointestinal, assinale V ou F:")]
    [InlineData("Dentre as alternativas abaixo, identifique a que se aproxima da ilustração:  \n[IMAGEM — 004.jpg]")]
    public void QuestaoComAlternativasOuEscolha_NaoEhRemovida(string texto)
    {
        var r = refinador.Refinar([new QuestaoExtraida(texto, "x.jpg")], Padrao);

        Assert.Single(r.Mantidas);
        Assert.Empty(r.Removidas);
    }

    [Theory]
    [InlineData("Você é um endoscopista que recebe um paciente. Na requisição do exame, o que você escreve?")]
    [InlineData("Defina criptorquidia, explicando sua principal consequência.")]
    [InlineData("Explique a fisiopatologia da asma.")]
    public void QuestaoQueExigeEscrita_EhRemovida(string texto)
    {
        var r = refinador.Refinar([new QuestaoExtraida(texto, "x.jpg")], Padrao);

        Assert.Empty(r.Mantidas);
        Assert.Equal("dissertativa", Assert.Single(r.Removidas).Motivo);
    }
}
