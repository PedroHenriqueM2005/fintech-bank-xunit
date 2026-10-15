using System;

namespace FintechBank.App.Services;

/// <summary>
/// Regras de negócio de Empréstimo do FintechBank.
/// Responsável pelo cálculo da parcela (com juros definidos pelo score de crédito)
/// e pela aprovação do crédito com base na margem consignável de 30% da renda.
/// </summary>
public class EmprestimoService
{
    /// <summary>Score mínimo para enquadrar o cliente na faixa de juros reduzida (5%).</summary>
    public const int ScoreAlto = 700;

    /// <summary>Score mínimo para enquadrar o cliente na faixa de juros intermediária (12%).</summary>
    public const int ScoreMedio = 500;

    /// <summary>Fator de juros total aplicado ao score alto (>= 700): +5%.</summary>
    public const decimal FatorJurosScoreAlto = 1.05m;

    /// <summary>Fator de juros total aplicado ao score médio (500 a 699): +12%.</summary>
    public const decimal FatorJurosScoreMedio = 1.12m;

    /// <summary>Fator de juros total aplicado ao score baixo (&lt; 500): +20%.</summary>
    public const decimal FatorJurosScoreBaixo = 1.20m;

    /// <summary>Percentual máximo da renda mensal que pode ser comprometido com a parcela (30%).</summary>
    public const decimal PercentualMaximoRenda = 0.30m;

    /// <summary>
    /// Calcula o valor da parcela individual de um empréstimo.
    /// <list type="bullet">
    ///   <item><description>scoreCredito &gt;= 700        →  juros totais de 5%  (valorTotal * 1.05m).</description></item>
    ///   <item><description>scoreCredito entre 500 e 699 →  juros totais de 12% (valorTotal * 1.12m).</description></item>
    ///   <item><description>scoreCredito &lt; 500         →  juros totais de 20% (valorTotal * 1.20m).</description></item>
    /// </list>
    /// A parcela é obtida pela divisão do valor com juros pelo número de parcelas.
    /// </summary>
    /// <param name="valorTotal">Valor principal solicitado (deve ser maior que zero).</param>
    /// <param name="parcelas">Quantidade de parcelas (deve ser maior que zero).</param>
    /// <param name="scoreCredito">Pontuação de crédito do cliente (deve estar entre 0 e 1000).</param>
    /// <returns>Valor de cada parcela, com 2 casas decimais (arredondamento bancário).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando algum parâmetro está fora do domínio válido.</exception>
    public decimal CalcularValorParcela(decimal valorTotal, int parcelas, int scoreCredito)
    {
        if (valorTotal <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorTotal), valorTotal, "O valor total do empréstimo deve ser maior que zero.");
        }

        if (parcelas <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parcelas), parcelas, "A quantidade de parcelas deve ser maior que zero.");
        }

        if (scoreCredito < 0 || scoreCredito > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scoreCredito), scoreCredito, "O score de crédito deve estar entre 0 e 1000.");
        }

        decimal fatorJuros = ObterFatorJuros(scoreCredito);
        decimal valorComJuros = valorTotal * fatorJuros;

        // Arredondamento "AwayFromZero" (padrão comercial/bancário): 1.005 -> 1.01.
        return Math.Round(valorComJuros / parcelas, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Aprova ou recusa o empréstimo conforme a margem consignável.
    /// A parcela só é aceita quando compromete no máximo 30% da renda mensal,
    /// ou seja, quando <c>valorParcela &lt;= rendaMensal * 0.30m</c>.
    /// </summary>
    /// <param name="rendaMensal">Renda mensal bruta declarada (não pode ser negativa).</param>
    /// <param name="valorParcela">Valor da parcela calculada (deve ser maior que zero).</param>
    /// <returns><c>true</c> se o empréstimo for aprovado; <c>false</c> caso contrário.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando algum parâmetro está fora do domínio válido.</exception>
    public bool AprovarEmprestimo(decimal rendaMensal, decimal valorParcela)
    {
        if (rendaMensal < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rendaMensal), rendaMensal, "A renda mensal não pode ser negativa.");
        }

        if (valorParcela <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorParcela), valorParcela, "O valor da parcela deve ser maior que zero.");
        }

        decimal margemConsignavel = rendaMensal * PercentualMaximoRenda;

        return valorParcela <= margemConsignavel;
    }

    /// <summary>
    /// Retorna o fator de juros total aplicável de acordo com a faixa de score de crédito.
    /// Método privado de apoio que isola a tabela de precificação (facilita manutenção e testes).
    /// </summary>
    /// <param name="scoreCredito">Pontuação de crédito já validada.</param>
    /// <returns>Fator multiplicador aplicado sobre o valor total do empréstimo.</returns>
    private static decimal ObterFatorJuros(int scoreCredito)
    {
        if (scoreCredito >= ScoreAlto)
        {
            return FatorJurosScoreAlto;
        }

        if (scoreCredito >= ScoreMedio)
        {
            return FatorJurosScoreMedio;
        }

        return FatorJurosScoreBaixo;
    }
}
