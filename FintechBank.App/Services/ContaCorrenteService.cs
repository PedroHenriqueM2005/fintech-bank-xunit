using System;

namespace FintechBank.App.Services;

/// <summary>
/// Regras de negócio da Conta Corrente do FintechBank.
/// Responsável pela cobrança da taxa de manutenção mensal
/// e pela validação de saques (saldo + limite de cheque especial).
/// </summary>
public class ContaCorrenteService
{
    /// <summary>Saldo a partir do qual o cliente fica isento da taxa de manutenção.</summary>
    public const decimal LimiteIsencaoTaxa = 5_000.00m;

    /// <summary>Saldo mínimo para pagar a taxa intermediária (abaixo disso, taxa cheia).</summary>
    public const decimal LimiteTaxaIntermediaria = 1_000.00m;

    /// <summary>Valor da taxa de manutenção para saldos intermediários.</summary>
    public const decimal TaxaIntermediaria = 12.50m;

    /// <summary>Valor da taxa de manutenção para saldos baixos.</summary>
    public const decimal TaxaSaldoBaixo = 25.00m;

    /// <summary>Valor da taxa quando o cliente está isento.</summary>
    public const decimal TaxaIsenta = 0.00m;

    /// <summary>
    /// Calcula a taxa de manutenção mensal da conta corrente.
    /// <list type="bullet">
    ///   <item><description>Saldo &gt;= R$ 5.000,00 <b>OU</b> clienteEspecial == true  →  R$ 0,00 (isento).</description></item>
    ///   <item><description>Saldo entre R$ 1.000,00 e R$ 4.999,99 (e não especial)      →  R$ 12,50.</description></item>
    ///   <item><description>Saldo &lt; R$ 1.000,00 (e não especial)                      →  R$ 25,00.</description></item>
    /// </list>
    /// </summary>
    /// <param name="saldo">Saldo atual da conta corrente (não pode ser negativo).</param>
    /// <param name="clienteEspecial">Indica se o cliente possui o benefício de isenção.</param>
    /// <returns>Valor da taxa de manutenção a ser debitada no mês.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando o saldo informado é negativo.</exception>
    public decimal CalcularTaxaManutencao(decimal saldo, bool clienteEspecial)
    {
        if (saldo < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saldo),
                saldo,
                "O saldo da conta corrente não pode ser negativo.");
        }

        // Regra 1: isenção por saldo alto OU por benefício de cliente especial.
        if (saldo >= LimiteIsencaoTaxa || clienteEspecial)
        {
            return TaxaIsenta;
        }

        // Regra 2: faixa intermediária [R$ 1.000,00 ; R$ 4.999,99].
        if (saldo >= LimiteTaxaIntermediaria)
        {
            return TaxaIntermediaria;
        }

        // Regra 3: saldo abaixo de R$ 1.000,00.
        return TaxaSaldoBaixo;
    }

    /// <summary>
    /// Verifica se um saque pode ser realizado.
    /// O saque é permitido apenas quando <c>valorSaque &lt;= saldo + limiteChequeEspecial</c>.
    /// </summary>
    /// <param name="saldo">Saldo disponível em conta (não pode ser negativo).</param>
    /// <param name="limiteChequeEspecial">Limite de crédito pré-aprovado (não pode ser negativo).</param>
    /// <param name="valorSaque">Valor que o cliente deseja sacar (deve ser maior que zero).</param>
    /// <returns><c>true</c> se o saque estiver dentro do limite total; caso contrário, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando algum dos valores informados é inválido.</exception>
    public bool PodeRealizarSaque(decimal saldo, decimal limiteChequeEspecial, decimal valorSaque)
    {
        if (saldo < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saldo), saldo, "O saldo da conta corrente não pode ser negativo.");
        }

        if (limiteChequeEspecial < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limiteChequeEspecial), limiteChequeEspecial, "O limite de cheque especial não pode ser negativo.");
        }

        if (valorSaque <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorSaque), valorSaque, "O valor do saque deve ser maior que zero.");
        }

        decimal limiteTotalDisponivel = saldo + limiteChequeEspecial;

        return valorSaque <= limiteTotalDisponivel;
    }
}
