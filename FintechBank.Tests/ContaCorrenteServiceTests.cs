using FintechBank.App.Services;

namespace FintechBank.Tests;

/// <summary>
/// Testes unitários parametrizados da <see cref="ContaCorrenteService"/>.
/// Utiliza [Theory] + [InlineData] para cobrir as classes de equivalência
/// e os valores de fronteira das regras de taxa de manutenção e de saque.
/// </summary>
public class ContaCorrenteServiceTests
{
    private readonly ContaCorrenteService _service = new();

    // ========================================================================
    // Teste 1 — CalcularTaxaManutencao(saldo, clienteEspecial)
    // ========================================================================

    /// <summary>
    /// Cenários obrigatórios da lista de exercícios:
    /// saldo alto (isento), cliente especial (isento),
    /// saldo intermediário (R$ 12,50) e saldo baixo (R$ 25,00).
    /// </summary>
    [Theory]
    [InlineData(6000.00, false, 0.00)]   // Saldo alto          -> Isento
    [InlineData(500.00, true, 0.00)]     // Cliente especial    -> Isento
    [InlineData(2500.00, false, 12.50)]  // Saldo intermediário -> R$ 12,50
    [InlineData(300.00, false, 25.00)]   // Saldo baixo         -> R$ 25,00
    public void CalcularTaxaManutencao_DeveRetornarTaxaEsperada(
        double saldo, bool clienteEspecial, double taxaEsperada)
    {
        // Arrange & Act
        decimal taxaCalculada = _service.CalcularTaxaManutencao((decimal)saldo, clienteEspecial);

        // Assert
        Assert.Equal((decimal)taxaEsperada, taxaCalculada);
    }

    /// <summary>
    /// Valores de fronteira (boundary value analysis) das faixas de saldo:
    /// R$ 0,00 / R$ 999,99 / R$ 1.000,00 / R$ 4.999,99 / R$ 5.000,00.
    /// </summary>
    [Theory]
    [InlineData(0.00, false, 25.00)]      // Fronteira inferior da taxa cheia
    [InlineData(999.99, false, 25.00)]    // Último centavo antes da faixa intermediária
    [InlineData(1000.00, false, 12.50)]   // Primeiro valor da faixa intermediária
    [InlineData(4999.99, false, 12.50)]   // Último centavo antes da isenção
    [InlineData(5000.00, false, 0.00)]    // Primeiro valor isento por saldo
    [InlineData(100000.00, true, 0.00)]   // Cliente especial com saldo alto
    [InlineData(0.00, true, 0.00)]        // Cliente especial sem saldo algum
    public void CalcularTaxaManutencao_DeveRespeitarAsFronteirasDeSaldo(
        double saldo, bool clienteEspecial, double taxaEsperada)
    {
        // Arrange & Act
        decimal taxaCalculada = _service.CalcularTaxaManutencao((decimal)saldo, clienteEspecial);

        // Assert
        Assert.Equal((decimal)taxaEsperada, taxaCalculada);
    }

    /// <summary>
    /// Parâmetros complexos via [MemberData]: demonstra a alternativa ao [InlineData]
    /// quando o caso de teste precisa de um nome descritivo (útil no relatório do dotnet test).
    /// </summary>
    public static TheoryData<decimal, bool, decimal, string> CenariosTaxaManutencao => new()
    {
        { 6000.00m, false, 0.00m, "Saldo alto sem benefício" },
        { 500.00m, true, 0.00m, "Cliente especial com saldo baixo" },
        { 2500.00m, false, 12.50m, "Saldo na faixa intermediária" },
        { 300.00m, false, 25.00m, "Saldo abaixo de mil reais" }
    };

    [Theory]
    [MemberData(nameof(CenariosTaxaManutencao))]
    public void CalcularTaxaManutencao_CenariosNomeados_DevemRetornarTaxaEsperada(
        decimal saldo, bool clienteEspecial, decimal taxaEsperada, string descricaoCenario)
    {
        // Act
        decimal taxaCalculada = _service.CalcularTaxaManutencao(saldo, clienteEspecial);

        // Assert
        Assert.True(
            taxaCalculada == taxaEsperada,
            $"Cenário '{descricaoCenario}' esperava {taxaEsperada} e obteve {taxaCalculada}.");
    }

    [Fact]
    public void CalcularTaxaManutencao_ComSaldoNegativo_DeveLancarArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.CalcularTaxaManutencao(-1.00m, clienteEspecial: false));
    }

    // ========================================================================
    // Teste 2 — PodeRealizarSaque(saldo, limiteChequeEspecial, valorSaque)
    // ========================================================================

    /// <summary>
    /// Cenários obrigatórios da lista de exercícios:
    /// saque dentro do limite total e saque que excede saldo + limite.
    /// </summary>
    [Theory]
    [InlineData(1000.00, 500.00, 1200.00, true)]   // Dentro do limite total  -> true
    [InlineData(1000.00, 500.00, 1600.00, false)]  // Excede saldo + limite   -> false
    public void PodeRealizarSaque_DeveRetornarResultadoEsperado(
        double saldo, double limiteChequeEspecial, double valorSaque, bool resultadoEsperado)
    {
        // Arrange & Act
        bool resultado = _service.PodeRealizarSaque(
            (decimal)saldo, (decimal)limiteChequeEspecial, (decimal)valorSaque);

        // Assert
        Assert.Equal(resultadoEsperado, resultado);
    }

    /// <summary>
    /// Fronteiras do limite total (saldo + cheque especial): R$ 1.499,99 / R$ 1.500,00 / R$ 1.500,01.
    /// </summary>
    [Theory]
    [InlineData(1000.00, 500.00, 1500.00, true)]   // Exatamente o limite     -> true
    [InlineData(1000.00, 500.00, 1500.01, false)]  // Um centavo acima        -> false
    [InlineData(1000.00, 500.00, 1499.99, true)]   // Um centavo abaixo       -> true
    [InlineData(1000.00, 0.00, 1000.00, true)]     // Sem cheque especial     -> true
    [InlineData(0.00, 500.00, 500.00, true)]       // Saque só com o limite   -> true
    [InlineData(0.00, 0.00, 0.01, false)]          // Sem saldo e sem limite  -> false
    public void PodeRealizarSaque_DeveRespeitarAsFronteirasDoLimiteTotal(
        double saldo, double limiteChequeEspecial, double valorSaque, bool resultadoEsperado)
    {
        // Arrange & Act
        bool resultado = _service.PodeRealizarSaque(
            (decimal)saldo, (decimal)limiteChequeEspecial, (decimal)valorSaque);

        // Assert
        Assert.Equal(resultadoEsperado, resultado);
    }

    [Theory]
    [InlineData(-100.00, 500.00, 50.00)]   // Saldo negativo
    [InlineData(1000.00, -500.00, 50.00)]  // Limite negativo
    [InlineData(1000.00, 500.00, 0.00)]    // Saque zerado
    [InlineData(1000.00, 500.00, -50.00)]  // Saque negativo
    public void PodeRealizarSaque_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException(
        double saldo, double limiteChequeEspecial, double valorSaque)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.PodeRealizarSaque(
                (decimal)saldo, (decimal)limiteChequeEspecial, (decimal)valorSaque));
    }
}
