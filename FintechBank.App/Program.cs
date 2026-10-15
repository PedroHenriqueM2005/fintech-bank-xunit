using System.Globalization;
using FintechBank.App.Services;

// ============================================================================
//  FintechBank.App — Demonstração das regras de negócio
//  As regras reais ficam em Services/ContaCorrenteService.cs e
//  Services/EmprestimoService.cs e são validadas pelo projeto FintechBank.Tests.
// ============================================================================

ContaCorrenteService contaCorrente = new();
EmprestimoService emprestimo = new();

Console.WriteLine("=======================================================");
Console.WriteLine(" FintechBank — Simulador de Regras de Negócio (.NET 10)");
Console.WriteLine("=======================================================");

// ---------------------------------------------------------------------------
// 1) Taxa de manutenção mensal
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("1) TAXA DE MANUTENÇÃO DA CONTA CORRENTE");
Console.WriteLine("-------------------------------------------------------");

(decimal saldo, bool especial)[] cenariosConta =
{
    (6_000.00m, false),
    (500.00m, true),
    (2_500.00m, false),
    (300.00m, false)
};

foreach ((decimal saldo, bool especial) cenario in cenariosConta)
{
    decimal taxa = contaCorrente.CalcularTaxaManutencao(cenario.saldo, cenario.especial);

    Console.WriteLine(
        $"   Saldo {Moeda(cenario.saldo),12} | Especial: {SimNao(cenario.especial),5} => Taxa: {Moeda(taxa)}");
}

// ---------------------------------------------------------------------------
// 2) Validação de saque (saldo + cheque especial)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("2) VALIDAÇÃO DE SAQUE (SALDO + CHEQUE ESPECIAL)");
Console.WriteLine("-------------------------------------------------------");

(decimal saldo, decimal limite, decimal saque)[] cenariosSaque =
{
    (1_000.00m, 500.00m, 1_200.00m),
    (1_000.00m, 500.00m, 1_600.00m)
};

foreach ((decimal saldo, decimal limite, decimal saque) cenario in cenariosSaque)
{
    bool permitido = contaCorrente.PodeRealizarSaque(cenario.saldo, cenario.limite, cenario.saque);

    Console.WriteLine(
        $"   Saldo {Moeda(cenario.saldo)} + Limite {Moeda(cenario.limite)} | Saque {Moeda(cenario.saque)} => {(permitido ? "AUTORIZADO" : "NEGADO")}");
}

// ---------------------------------------------------------------------------
// 3) Parcela do empréstimo conforme o score de crédito
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("3) PARCELA DO EMPRÉSTIMO POR SCORE DE CRÉDITO");
Console.WriteLine("-------------------------------------------------------");

const decimal valorTotal = 10_000.00m;
const int parcelas = 10;

int[] scores = { 800, 600, 400 };

foreach (int score in scores)
{
    decimal parcela = emprestimo.CalcularValorParcela(valorTotal, parcelas, score);

    Console.WriteLine(
        $"   Empréstimo {Moeda(valorTotal)} em {parcelas}x | Score {score,4} => Parcela: {Moeda(parcela)}");
}

// ---------------------------------------------------------------------------
// 4) Aprovação pela margem consignável (máx. 30% da renda)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("4) APROVAÇÃO DE EMPRÉSTIMO (MARGEM DE 30% DA RENDA)");
Console.WriteLine("-------------------------------------------------------");

(decimal renda, decimal parcela)[] cenariosCredito =
{
    (5_000.00m, 1_500.00m),
    (5_000.00m, 1_600.00m)
};

foreach ((decimal renda, decimal parcela) cenario in cenariosCredito)
{
    bool aprovado = emprestimo.AprovarEmprestimo(cenario.renda, cenario.parcela);

    Console.WriteLine(
        $"   Renda {Moeda(cenario.renda)} | Parcela {Moeda(cenario.parcela)} ({Percentual(cenario.parcela, cenario.renda)}) => {(aprovado ? "APROVADO" : "RECUSADO")}");
}

Console.WriteLine();
Console.WriteLine("=======================================================");
Console.WriteLine(" Execute 'dotnet test' para validar as regras com xUnit.");
Console.WriteLine("=======================================================");

// ---------------------------- Funções auxiliares ----------------------------

// Cultura pt-BR para exibição. Caso o host não possua dados de globalização
// (ex.: contêineres com InvariantGlobalization), cai para um formato manual
// equivalente, garantindo que a demonstração nunca quebre.
static NumberFormatInfo ObterFormatoBrasileiro()
{
    NumberFormatInfo formato;

    try
    {
        formato = (NumberFormatInfo)CultureInfo.GetCultureInfo("pt-BR").NumberFormat.Clone();
    }
    catch (CultureNotFoundException)
    {
        // Host sem dados de globalização (ex.: InvariantGlobalization=true):
        // monta manualmente um formato equivalente ao pt-BR.
        formato = new NumberFormatInfo
        {
            CurrencySymbol = "R$",
            CurrencyDecimalSeparator = ",",
            CurrencyGroupSeparator = ".",
            CurrencyDecimalDigits = 2,
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            PercentSymbol = "%",
            PercentDecimalSeparator = ",",
            PercentGroupSeparator = ".",
            PercentDecimalDigits = 1
        };
    }

    return formato;
}

static string Moeda(decimal valor) => valor.ToString("C", ObterFormatoBrasileiro());

static string SimNao(bool valor) => valor ? "sim" : "não";

static string Percentual(decimal parcela, decimal renda) =>
    renda == 0m
        ? "n/d"
        : (parcela / renda).ToString("P1", ObterFormatoBrasileiro());
