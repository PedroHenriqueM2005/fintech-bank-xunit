using FintechBank.App.Services;

namespace FintechBank.Tests;

/// <summary>
/// Testes unitários parametrizados da <see cref="EmprestimoService"/>.
/// Utiliza [Theory] + [InlineData] para cobrir as faixas de score de crédito
/// e a margem consignável de 30% da renda mensal.
/// </summary>
public class EmprestimoServiceTests
{
    private readonly EmprestimoService _service = new();

    // ========================================================================
    // Teste 1 — CalcularValorParcela(valorTotal, parcelas, scoreCredito)
    // ========================================================================

    /// <summary>
    /// Cenários obrigatórios da lista de exercícios:
    /// score alto (5%), score médio (12%) e score baixo (20%).
    /// </summary>
    [Theory]
    [InlineData(10000.00, 10, 800, 1050.00)]  // Score alto: 10.000 * 1,05 = 10.500 / 10 = 1.050,00
    [InlineData(10000.00, 10, 600, 1120.00)]  // Score médio: 10.000 * 1,12 = 11.200 / 10 = 1.120,00
    [InlineData(10000.00, 10, 400, 1200.00)]  // Score baixo: 10.000 * 1,20 = 12.000 / 10 = 1.200,00
    public void CalcularValorParcela_DeveAplicarJurosConformeScore(
        double valorTotal, int parcelas, int scoreCredito, double parcelaEsperada)
    {
        // Arrange & Act
        decimal parcelaCalculada = _service.CalcularValorParcela(
            (decimal)valorTotal, parcelas, scoreCredito);

        // Assert
        Assert.Equal((decimal)parcelaEsperada, parcelaCalculada);
    }

    /// <summary>
    /// Valores de fronteira das faixas de score: 499/500 (12% x 20%) e 699/700 (5% x 12%).
    /// </summary>
    [Theory]
    [InlineData(10000.00, 10, 499, 1200.00)]  // Último score da faixa de 20%
    [InlineData(10000.00, 10, 500, 1120.00)]  // Primeiro score da faixa de 12%
    [InlineData(10000.00, 10, 699, 1120.00)]  // Último score da faixa de 12%
    [InlineData(10000.00, 10, 700, 1050.00)]  // Primeiro score da faixa de 5%
    [InlineData(10000.00, 10, 0, 1200.00)]    // Score mínimo possível
    [InlineData(10000.00, 10, 1000, 1050.00)] // Score máximo possível
    public void CalcularValorParcela_DeveRespeitarAsFronteirasDeScore(
        double valorTotal, int parcelas, int scoreCredito, double parcelaEsperada)
    {
        // Arrange & Act
        decimal parcelaCalculada = _service.CalcularValorParcela(
            (decimal)valorTotal, parcelas, scoreCredito);

        // Assert
        Assert.Equal((decimal)parcelaEsperada, parcelaCalculada);
    }

    /// <summary>
    /// Divisões que não fecham em centavos exatos: valida o arredondamento
    /// comercial (AwayFromZero) com 2 casas decimais.
    /// </summary>
    [Theory]
    [InlineData(1000.00, 3, 800, 350.00)]    // 1.050 / 3 = 350,00
    [InlineData(1000.00, 7, 800, 150.00)]    // 1.050 / 7 = 150,00
    [InlineData(1000.00, 6, 500, 186.67)]    // 1.120 / 6 = 186,666... -> 186,67
    [InlineData(1000.00, 12, 400, 100.00)]   // 1.200 / 12 = 100,00
    [InlineData(5000.00, 24, 650, 233.33)]   // 5.600 / 24 = 233,333... -> 233,33
    public void CalcularValorParcela_DeveArredondarParaDuasCasasDecimais(
        double valorTotal, int parcelas, int scoreCredito, double parcelaEsperada)
    {
        // Arrange & Act
        decimal parcelaCalculada = _service.CalcularValorParcela(
            (decimal)valorTotal, parcelas, scoreCredito);

        // Assert
        Assert.Equal((decimal)parcelaEsperada, parcelaCalculada);
    }

    [Theory]
    [InlineData(0.00, 10, 800)]      // Valor total zerado
    [InlineData(-5000.00, 10, 800)]  // Valor total negativo
    [InlineData(10000.00, 0, 800)]   // Zero parcelas (divisão inválida)
    [InlineData(10000.00, -10, 800)] // Parcelas negativas
    [InlineData(10000.00, 10, -1)]   // Score negativo
    [InlineData(10000.00, 10, 1001)] // Score acima do domínio
    public void CalcularValorParcela_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException(
        double valorTotal, int parcelas, int scoreCredito)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.CalcularValorParcela((decimal)valorTotal, parcelas, scoreCredito));
    }

    // ========================================================================
    // Teste 2 — AprovarEmprestimo(rendaMensal, valorParcela)
    // ========================================================================

    /// <summary>
    /// Cenários obrigatórios da lista de exercícios:
    /// parcela igual a 30% da renda (aprovado) e parcela acima de 30% (recusado).
    /// </summary>
    [Theory]
    [InlineData(5000.00, 1500.00, true)]   // Parcela = 30% da renda  -> Aprovado
    [InlineData(5000.00, 1600.00, false)]  // Parcela > 30% da renda  -> Recusado
    public void AprovarEmprestimo_DeveRespeitarMargemConsignavelDe30PorCento(
        double rendaMensal, double valorParcela, bool resultadoEsperado)
    {
        // Arrange & Act
        bool resultado = _service.AprovarEmprestimo((decimal)rendaMensal, (decimal)valorParcela);

        // Assert
        Assert.Equal(resultadoEsperado, resultado);
    }

    /// <summary>
    /// Fronteiras da margem consignável: um centavo abaixo, exatamente 30% e um centavo acima.
    /// </summary>
    [Theory]
    [InlineData(5000.00, 1499.99, true)]   // 29,99% da renda  -> Aprovado
    [InlineData(5000.00, 1500.01, false)]  // 30,01% da renda  -> Recusado
    [InlineData(10000.00, 3000.00, true)]  // Exatamente 30%   -> Aprovado
    [InlineData(2000.00, 600.00, true)]    // Exatamente 30%   -> Aprovado
    [InlineData(2000.00, 600.01, false)]   // Um centavo acima -> Recusado
    [InlineData(0.00, 100.00, false)]      // Sem renda        -> Recusado
    public void AprovarEmprestimo_DeveRespeitarAsFronteirasDaMargem(
        double rendaMensal, double valorParcela, bool resultadoEsperado)
    {
        // Arrange & Act
        bool resultado = _service.AprovarEmprestimo((decimal)rendaMensal, (decimal)valorParcela);

        // Assert
        Assert.Equal(resultadoEsperado, resultado);
    }

    [Theory]
    [InlineData(-5000.00, 1500.00)]  // Renda negativa
    [InlineData(5000.00, 0.00)]      // Parcela zerada
    [InlineData(5000.00, -1500.00)]  // Parcela negativa
    public void AprovarEmprestimo_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException(
        double rendaMensal, double valorParcela)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.AprovarEmprestimo((decimal)rendaMensal, (decimal)valorParcela));
    }

    // ========================================================================
    // Teste de integração entre as duas regras do serviço de empréstimo
    // ========================================================================

    /// <summary>
    /// Fluxo completo: calcula a parcela pelo score e em seguida avalia a aprovação,
    /// garantindo que as duas regras conversem entre si sem regressão.
    /// </summary>
    [Theory]
    [InlineData(10000.00, 10, 800, 6000.00, true)]   // Parcela 1.050,00 <= 30% de 6.000,00 (1.800,00)
    [InlineData(10000.00, 10, 400, 3000.00, false)]  // Parcela 1.200,00 >  30% de 3.000,00 (900,00)
    [InlineData(12000.00, 12, 600, 4000.00, true)]   // Parcela 1.120,00 <= 30% de 4.000,00 (1.200,00)
    public void FluxoCompleto_CalculoDaParcelaEAprovacao_DevemSerConsistentes(
        double valorTotal, int parcelas, int scoreCredito, double rendaMensal, bool aprovacaoEsperada)
    {
        // Arrange
        decimal valorParcela = _service.CalcularValorParcela((decimal)valorTotal, parcelas, scoreCredito);

        // Act
        bool aprovado = _service.AprovarEmprestimo((decimal)rendaMensal, valorParcela);

        // Assert
        Assert.Equal(aprovacaoEsperada, aprovado);
    }
}
