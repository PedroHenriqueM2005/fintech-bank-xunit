


O **FintechBank** é o núcleo (core) de um banco digital simplificado. A solução foi construída
para servir de **laboratório de Garantia da Qualidade**: as regras de negócio ficam isoladas em
classes de serviço puras (sem banco de dados, sem I/O, sem dependências externas) e são
validadas por uma suíte de **testes unitários parametrizados** escritos com **xUnit**.

A solução é composta por dois projetos:

| Projeto | Tipo | Responsabilidade |
| --- | --- | --- |
| `FintechBank.App` | Console App (`net10.0`) | Regras de negócio (`Services/`) + demonstração em console (`Program.cs`) |
| `FintechBank.Tests` | xUnit Test Project (`net10.0`) | Suítes de teste unitário das classes de serviço |

```
FintechBank/
├── FintechBank.slnx                     # Arquivo de solução (.NET 10 usa o novo formato .slnx)
├── global.json                          # Fixa a versão do SDK usada na compilação
├── .editorconfig                        # Padronização de estilo/indentação (4 espaços)
├── .gitignore                           # Ignora bin/, obj/ e artefatos de build (.NET)
├── LICENSE                              # Licença MIT
├── README.md
├── docs/
│   └── PLANO-DE-TESTES.md               # Particionamento em classes de equivalência + valores de fronteira
├── .github/
│   └── workflows/
│       └── ci.yml                       # Pipeline: restore → build → dotnet test
├── FintechBank.App/
│   ├── FintechBank.App.csproj
│   ├── Program.cs                       # Demonstração das regras no terminal
│   └── Services/
│       ├── ContaCorrenteService.cs      # Taxa de manutenção + validação de saque
│       └── EmprestimoService.cs         # Parcela por score + aprovação por margem consignável
└── FintechBank.Tests/
    ├── FintechBank.Tests.csproj
    ├── ContaCorrenteServiceTests.cs
    └── EmprestimoServiceTests.cs
```

---

## 2. Requisitos técnicos

| Item | Versão / Observação |
| --- | --- |
| **.NET SDK** | **10.0** (validado com SDK `10.0.402`) |
| **Linguagem** | C# 14 (`Nullable` e `ImplicitUsings` habilitados) |
| **Target framework** | `net10.0` |
| **Framework de testes** | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1 + xunit.runner.visualstudio 3.1.4 |
| **Cobertura** | coverlet.collector 6.0.4 (disponível via `--collect:"XPlat Code Coverage"`) |
| **IDE** | Visual Studio 2022/2026, VS Code (C# Dev Kit), JetBrains Rider ou apenas terminal |
| **Sistema operacional** | Windows, Linux ou macOS |

Para conferir a versão instalada do SDK:

```bash
dotnet --version
dotnet --list-sdks
```

Se ainda não tiver o .NET 10, baixe em <https://dotnet.microsoft.com/download/dotnet/10.0>.

---

## 3. Regras de negócio implementadas

### 3.1 `ContaCorrenteService` — Conta Corrente

Arquivo: [`FintechBank.App/Services/ContaCorrenteService.cs`](FintechBank.App/Services/ContaCorrenteService.cs)

#### Regra 1 — `CalcularTaxaManutencao(decimal saldo, bool clienteEspecial) → decimal`

Define a taxa de manutenção mensal cobrada do correntista:

| Condição | Taxa |
| --- | --- |
| `saldo >= R$ 5.000,00` **OU** `clienteEspecial == true` | **R$ 0,00** (isento) |
| `R$ 1.000,00 <= saldo <= R$ 4.999,99` e não especial | **R$ 12,50** |
| `saldo < R$ 1.000,00` e não especial | **R$ 25,00** |

A ordem das condições é importante: a isenção por **cliente especial** tem precedência sobre a
faixa de saldo, ou seja, um cliente especial **nunca** paga taxa, independentemente do saldo.

#### Regra 2 — `PodeRealizarSaque(decimal saldo, decimal limiteChequeEspecial, decimal valorSaque) → bool`

Retorna `true` **apenas** se:

```
valorSaque <= saldo + limiteChequeEspecial
```

Ou seja, o cliente pode usar o cheque especial como complemento do saldo disponível.
Qualquer centavo acima desse limite total resulta em `false` (saque negado).

### 3.2 `EmprestimoService` — Empréstimo

Arquivo: [`FintechBank.App/Services/EmprestimoService.cs`](FintechBank.App/Services/EmprestimoService.cs)

#### Regra 1 — `CalcularValorParcela(decimal valorTotal, int parcelas, int scoreCredito) → decimal`

O preço do crédito depende do **score de crédito** do cliente:

| Faixa de score | Juros totais | Fator aplicado |
| --- | --- | --- |
| `scoreCredito >= 700` | 5% | `valorTotal * 1.05m` |
| `500 <= scoreCredito <= 699` | 12% | `valorTotal * 1.12m` |
| `scoreCredito < 500` | 20% | `valorTotal * 1.20m` |

O valor da parcela é `valorComJuros / parcelas`, arredondado para **2 casas decimais** com
critério comercial (`MidpointRounding.AwayFromZero`, o padrão do mercado financeiro: `1,005 → 1,01`).

Exemplo: `R$ 10.000,00` em `10x` com score `800` → `10.000 × 1,05 = 10.500` → parcela de **R$ 1.050,00**.

#### Regra 2 — `AprovarEmprestimo(decimal rendaMensal, decimal valorParcela) → bool`

Aplica a **margem consignável** de 30%:

```
aprovado = valorParcela <= rendaMensal * 0.30m
```

Exemplo: renda de `R$ 5.000,00` comporta parcelas de até `R$ 1.500,00` (30%);
uma parcela de `R$ 1.600,00` é **recusada**.

### 3.3 Validação de entrada (contrato defensivo)

Todos os métodos públicos validam seus parâmetros e lançam `ArgumentOutOfRangeException`
para valores fora do domínio (saldo negativo, zero parcelas, score fora de 0–1000 etc.).
Essas guardas também são cobertas por testes, garantindo que entrada inválida **falhe rápido**
em vez de produzir resultado silenciosamente errado.

---

## 4. Como clonar, compilar e executar

### 4.1 Clonar o repositório

```bash
git clone https://github.com/SEU-USUARIO/fintech-bank-xunit.git
cd fintech-bank-xunit
```

### 4.2 Restaurar dependências e compilar a solução

```bash
dotnet restore
dotnet build
```

Saída esperada: `Build succeeded. 0 Error(s)`.

> **Nota:** a partir do .NET 10 o comando `dotnet new sln` gera o arquivo de solução no novo
> formato **`FintechBank.slnx`** (XML), que substitui o antigo `.sln`. O Visual Studio 2022 17.13+,
> o Visual Studio 2026 e o Rider já abrem `.slnx` nativamente.

### 4.3 Executar a demonstração no console (opcional)

```bash
dotnet run --project FintechBank.App
```

Saída esperada:

```text
=======================================================
 FintechBank — Simulador de Regras de Negócio (.NET 10)
=======================================================

1) TAXA DE MANUTENÇÃO DA CONTA CORRENTE
-------------------------------------------------------
   Saldo   R$6.000,00 | Especial:   não => Taxa: R$0,00
   Saldo     R$500,00 | Especial:   sim => Taxa: R$0,00
   Saldo   R$2.500,00 | Especial:   não => Taxa: R$12,50
   Saldo     R$300,00 | Especial:   não => Taxa: R$25,00

2) VALIDAÇÃO DE SAQUE (SALDO + CHEQUE ESPECIAL)
-------------------------------------------------------
   Saldo R$1.000,00 + Limite R$500,00 | Saque R$1.200,00 => AUTORIZADO
   Saldo R$1.000,00 + Limite R$500,00 | Saque R$1.600,00 => NEGADO

3) PARCELA DO EMPRÉSTIMO POR SCORE DE CRÉDITO
-------------------------------------------------------
   Empréstimo R$10.000,00 em 10x | Score  800 => Parcela: R$1.050,00
   Empréstimo R$10.000,00 em 10x | Score  600 => Parcela: R$1.120,00
   Empréstimo R$10.000,00 em 10x | Score  400 => Parcela: R$1.200,00

4) APROVAÇÃO DE EMPRÉSTIMO (MARGEM DE 30% DA RENDA)
-------------------------------------------------------
   Renda R$5.000,00 | Parcela R$1.500,00 (30,0 %) => APROVADO
   Renda R$5.000,00 | Parcela R$1.600,00 (32,0 %) => RECUSADO

=======================================================
 Execute 'dotnet test' para validar as regras com xUnit.
=======================================================
```

### 4.4 Comandos usados para criar a solução do zero

Caso queira reproduzir a estrutura a partir de um diretório vazio:

```bash
dotnet new sln -n FintechBank
dotnet new console -n FintechBank.App -f net10.0
dotnet new xunit -n FintechBank.Tests -f net10.0
dotnet sln add FintechBank.App/FintechBank.App.csproj
dotnet sln add FintechBank.Tests/FintechBank.Tests.csproj
dotnet add FintechBank.Tests/FintechBank.Tests.csproj reference FintechBank.App/FintechBank.App.csproj
```

---

## 5. Como executar os testes unitários

Na raiz do repositório (onde está o `FintechBank.slnx`):

```bash
dotnet test
```

Saída real obtida na validação deste projeto:

```text
Test run for FintechBank.Tests/bin/Debug/net10.0/FintechBank.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    62, Skipped:     0, Total:    62, Duration: 133 ms - FintechBank.Tests.dll (net10.0)
```

Comandos úteis:

```bash
# Executar apenas uma classe de testes
dotnet test --filter "FullyQualifiedName~ContaCorrenteServiceTests"
dotnet test --filter "FullyQualifiedName~EmprestimoServiceTests"

# Executar apenas um método (e todos os seus casos [InlineData])
dotnet test --filter "FullyQualifiedName~CalcularTaxaManutencao_DeveRetornarTaxaEsperada"

# Listar os casos de teste sem executá-los
dotnet test --list-tests

# Saída detalhada de cada caso executado
dotnet test --logger "console;verbosity=detailed"

# Gerar relatório de cobertura (coverlet)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

**Critério de aceite:** `100%` dos testes das duas suítes devem passar (`Failed: 0`).

---

## 6. Estrutura de testes parametrizados (`[Theory]` / `[InlineData]`)

### 6.1 Conceito

No xUnit, testes parametrizados permitem escrever **um único método de teste** que é executado
várias vezes, uma para cada conjunto de dados:

| Atributo | Função |
| --- | --- |
| `[Fact]` | Teste sem parâmetros — executado **uma única vez**. |
| `[Theory]` | Marca o método como **teste parametrizado** — executado **uma vez por caso de teste**. |
| `[InlineData(...)]` | Fornece **valores literais** (constantes em tempo de compilação) para cada caso. |
| `[MemberData(nameof(...))]` | Fornece casos a partir de uma **propriedade/método estático**, permitindo dados complexos (ex.: `TheoryData<decimal, bool, decimal, string>`). |

Cada `[InlineData]` vira um **caso de teste independente** no relatório: se um falhar, os demais
continuam sendo executados e o relatório aponta exatamente quais parâmetros falharam
(ex.: `CalcularTaxaManutencao_DeveRetornarTaxaEsperada(saldo: 2500, clienteEspecial: False, taxaEsperada: 12.5)`).

> **Detalhe técnico importante:** os argumentos de `[InlineData]` precisam ser constantes de
> compilação, e o C# **não** permite o literal `6000.00m` (decimal) nesse contexto. Por isso os
> parâmetros são declarados como `double` (`6000.00`) e convertidos para `decimal` dentro do teste
> com `(decimal)saldo`. É o padrão recomendado pela documentação do xUnit.

### 6.2 Padrão AAA adotado

Todos os testes seguem o padrão **Arrange–Act–Assert** e a convenção de nomes
`Metodo_Cenario_ResultadoEsperado`:

```csharp
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
```

### 6.3 `ContaCorrenteServiceTests.cs`

| Método de teste | Técnica | Casos |
| --- | --- | --- |
| `CalcularTaxaManutencao_DeveRetornarTaxaEsperada` | `[Theory]` + `[InlineData]` — casos obrigatórios da lista | 4 |
| `CalcularTaxaManutencao_DeveRespeitarAsFronteirasDeSaldo` | `[InlineData]` — valores de fronteira (`0`, `999,99`, `1.000`, `4.999,99`, `5.000`) | 7 |
| `CalcularTaxaManutencao_CenariosNomeados_DevemRetornarTaxaEsperada` | `[MemberData]` + `TheoryData` — casos com descrição textual | 4 |
| `CalcularTaxaManutencao_ComSaldoNegativo_DeveLancarArgumentOutOfRangeException` | `[Fact]` — teste de exceção | 1 |
| `PodeRealizarSaque_DeveRetornarResultadoEsperado` | `[Theory]` + `[InlineData]` — casos obrigatórios da lista | 2 |
| `PodeRealizarSaque_DeveRespeitarAsFronteirasDoLimiteTotal` | `[InlineData]` — `1.499,99` / `1.500,00` / `1.500,01`, sem limite, sem saldo | 6 |
| `PodeRealizarSaque_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException` | `[InlineData]` — entradas inválidas | 4 |

**Total: 28 casos de teste.**

### 6.4 `EmprestimoServiceTests.cs`

| Método de teste | Técnica | Casos |
| --- | --- | --- |
| `CalcularValorParcela_DeveAplicarJurosConformeScore` | `[Theory]` + `[InlineData]` — casos obrigatórios da lista (score 800/600/400) | 3 |
| `CalcularValorParcela_DeveRespeitarAsFronteirasDeScore` | `[InlineData]` — fronteiras `499/500` e `699/700`, além de `0` e `1000` | 6 |
| `CalcularValorParcela_DeveArredondarParaDuasCasasDecimais` | `[InlineData]` — divisões não exatas (`1.120 / 6 = 186,67`) | 5 |
| `CalcularValorParcela_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException` | `[InlineData]` — valor zerado, 0 parcelas, score inválido | 6 |
| `AprovarEmprestimo_DeveRespeitarMargemConsignavelDe30PorCento` | `[Theory]` + `[InlineData]` — casos obrigatórios da lista | 2 |
| `AprovarEmprestimo_DeveRespeitarAsFronteirasDaMargem` | `[InlineData]` — `1.499,99` / `1.500,00` / `1.500,01` | 6 |
| `AprovarEmprestimo_ComEntradasInvalidas_DeveLancarArgumentOutOfRangeException` | `[InlineData]` — renda/parcela inválidas | 3 |
| `FluxoCompleto_CalculoDaParcelaEAprovacao_DevemSerConsistentes` | `[InlineData]` — integração entre as duas regras do serviço | 3 |

**Total: 34 casos de teste.**

### 6.5 Rastreabilidade dos casos obrigatórios da lista

| `[InlineData]` exigido | Onde está |
| --- | --- |
| `(6000.00, false, 0.00)` | `ContaCorrenteServiceTests.CalcularTaxaManutencao_DeveRetornarTaxaEsperada` |
| `(500.00, true, 0.00)` | idem |
| `(2500.00, false, 12.50)` | idem |
| `(300.00, false, 25.00)` | idem |
| `(1000.00, 500.00, 1200.00, true)` | `ContaCorrenteServiceTests.PodeRealizarSaque_DeveRetornarResultadoEsperado` |
| `(1000.00, 500.00, 1600.00, false)` | idem |
| `(10000.00, 10, 800, 1050.00)` | `EmprestimoServiceTests.CalcularValorParcela_DeveAplicarJurosConformeScore` |
| `(10000.00, 10, 600, 1120.00)` | idem |
| `(10000.00, 10, 400, 1200.00)` | idem |
| `(5000.00, 1500.00, true)` | `EmprestimoServiceTests.AprovarEmprestimo_DeveRespeitarMargemConsignavelDe30PorCento` |
| `(5000.00, 1600.00, false)` | idem |

O detalhamento de **particionamento em classes de equivalência** e **análise de valor de fronteira**
está em [`docs/PLANO-DE-TESTES.md`](docs/PLANO-DE-TESTES.md).

---

## 7. Versionamento e integração contínua

O repositório segue o fluxo **GitHub Flow**: `main` é o branch estável; alterações entram via
*Pull Request* e o pipeline `.github/workflows/ci.yml` executa `restore → build → test`
automaticamente a cada *push*/*PR*.

```bash
# Primeiro commit e publicação no GitHub (após criar o repositório fintech-bank-xunit)
git init
git add .
git commit -m "feat: regras de negócio da conta corrente e do empréstimo com testes xUnit"
git branch -M main
git remote add origin https://github.com/SEU-USUARIO/fintech-bank-xunit.git
git push -u origin main
```

O arquivo `.gitignore` foi gerado com `dotnet new gitignore`, o padrão oficial para projetos .NET,
e garante que `bin/`, `obj/`, `TestResults/` e arquivos de usuário (`.vs/`, `*.user`) **não** sejam versionados.

---

## 8. Licença

Distribuído sob a licença **MIT**. Veja o arquivo [`LICENSE`](LICENSE) para mais informações.
---
