# Plano de Testes — FintechBank

**Disciplina:** Garantia da Qualidade de Software / Gestão e Qualidade de Software
**Prof.:** Daniel Henrique Matos de Paiva
**Ferramenta:** xUnit 2.9.3 (`[Theory]`, `[InlineData]`, `[MemberData]`, `[Fact]`)
**Técnicas aplicadas:** Particionamento em classes de equivalência + Análise de valor de fronteira + Testes de exceção

---

## 1. Objeto de teste

| Classe | Método | Saída |
| --- | --- | --- |
| `ContaCorrenteService` | `CalcularTaxaManutencao(decimal saldo, bool clienteEspecial)` | `decimal` (0,00 / 12,50 / 25,00) |
| `ContaCorrenteService` | `PodeRealizarSaque(decimal saldo, decimal limiteChequeEspecial, decimal valorSaque)` | `bool` |
| `EmprestimoService` | `CalcularValorParcela(decimal valorTotal, int parcelas, int scoreCredito)` | `decimal` |
| `EmprestimoService` | `AprovarEmprestimo(decimal rendaMensal, decimal valorParcela)` | `bool` |

---

## 2. `CalcularTaxaManutencao`

### 2.1 Classes de equivalência

| # | Classe de equivalência | `clienteEspecial` | Saída esperada | Caso representativo |
| --- | --- | --- | --- | --- |
| CE1 | `saldo >= 5.000,00` | `false` | `0,00` (isento) | `6.000,00` |
| CE2 | qualquer saldo válido | `true` | `0,00` (isento) | `500,00` |
| CE3 | `1.000,00 <= saldo <= 4.999,99` | `false` | `12,50` | `2.500,00` |
| CE4 | `saldo < 1.000,00` | `false` | `25,00` | `300,00` |
| CE5 (inválida) | `saldo < 0` | qualquer | `ArgumentOutOfRangeException` | `-1,00` |

### 2.2 Valores de fronteira

| Fronteira | Valor testado | Saída esperada | Justificativa |
| --- | --- | --- | --- |
| Início do domínio | `0,00` | `25,00` | Menor saldo válido |
| Fim da taxa cheia | `999,99` | `25,00` | Último centavo antes da faixa intermediária |
| Início da taxa intermediária | `1.000,00` | `12,50` | Limite inclusivo (`>=`) |
| Fim da taxa intermediária | `4.999,99` | `12,50` | Último centavo antes da isenção |
| Início da isenção | `5.000,00` | `0,00` | Limite inclusivo (`>=`) |
| Combinação | `0,00` + especial | `0,00` | Isenção por benefício prevalece sobre o saldo |

### 2.3 Matriz de decisão (regra composta `saldo >= 5000 OU especial`)

| `saldo >= 5.000` | `clienteEspecial` | Resultado | Coberto por |
| :---: | :---: | :---: | --- |
| V | F | Isento | `[InlineData(6000.00, false, 0.00)]` |
| F | V | Isento | `[InlineData(500.00, true, 0.00)]` |
| V | V | Isento | `[InlineData(100000.00, true, 0.00)]` |
| F | F | 12,50 ou 25,00 | `[InlineData(2500.00, false, 12.50)]` / `[InlineData(300.00, false, 25.00)]` |

---

## 3. `PodeRealizarSaque`

Regra: `valorSaque <= saldo + limiteChequeEspecial`.

### 3.1 Classes de equivalência (com saldo = 1.000,00 e limite = 500,00 → total = 1.500,00)

| # | Classe | Saída esperada | Caso |
| --- | --- | --- | --- |
| CE1 | `valorSaque < limiteTotal` | `true` | `1.200,00` |
| CE2 | `valorSaque == limiteTotal` | `true` | `1.500,00` |
| CE3 | `valorSaque > limiteTotal` | `false` | `1.600,00` |
| CE4 (inválida) | `saldo < 0` **ou** `limite < 0` **ou** `valorSaque <= 0` | `ArgumentOutOfRangeException` | `-100,00` / `0,00` |

### 3.2 Valores de fronteira

| Valor do saque | Resultado | Comentário |
| --- | --- | --- |
| `1.499,99` | `true` | Um centavo abaixo do limite |
| `1.500,00` | `true` | Exatamente no limite (inclusivo) |
| `1.500,01` | `false` | Um centavo acima do limite |

### 3.3 Cenários complementares

| Cenário | Entrada | Saída |
| --- | --- | --- |
| Sem cheque especial, saque igual ao saldo | `(1.000,00 ; 0,00 ; 1.000,00)` | `true` |
| Sem saldo, saque pago inteiramente pelo limite | `(0,00 ; 500,00 ; 500,00)` | `true` |
| Sem saldo e sem limite | `(0,00 ; 0,00 ; 0,01)` | `false` |

---

## 4. `CalcularValorParcela`

### 4.1 Classes de equivalência por score (com `valorTotal = 10.000,00` e `parcelas = 10`)

| # | Faixa de score | Fator | Valor com juros | Parcela esperada | Caso |
| --- | --- | --- | --- | --- | --- |
| CE1 | `score >= 700` | `1,05` | `10.500,00` | `1.050,00` | `score = 800` |
| CE2 | `500 <= score <= 699` | `1,12` | `11.200,00` | `1.120,00` | `score = 600` |
| CE3 | `score < 500` | `1,20` | `12.000,00` | `1.200,00` | `score = 400` |
| CE4 (inválida) | `score < 0` ou `score > 1000`, `valorTotal <= 0`, `parcelas <= 0` | — | — | `ArgumentOutOfRangeException` | `-1`, `1001`, `0`, `0` |

### 4.2 Valores de fronteira do score

| Score | Faixa aplicada | Parcela | Justificativa |
| --- | --- | --- | --- |
| `0` | 20% | `1.200,00` | Menor score do domínio |
| `499` | 20% | `1.200,00` | Último ponto antes de subir de faixa |
| `500` | 12% | `1.120,00` | Limite inferior inclusivo da faixa média |
| `699` | 12% | `1.120,00` | Limite superior inclusivo da faixa média |
| `700` | 5% | `1.050,00` | Limite inferior inclusivo da faixa alta |
| `1000` | 5% | `1.050,00` | Maior score do domínio |

### 4.3 Arredondamento (divisões não exatas)

| Entrada | Cálculo | Parcela esperada |
| --- | --- | --- |
| `1.000,00` / `6x` / score `500` | `1.120,00 / 6 = 186,666…` | `186,67` |
| `5.000,00` / `24x` / score `650` | `5.600,00 / 24 = 233,333…` | `233,33` |
| `1.000,00` / `3x` / score `800` | `1.050,00 / 3 = 350,000` | `350,00` |
| `1.000,00` / `7x` / score `800` | `1.050,00 / 7 = 150,000` | `150,00` |
| `1.000,00` / `12x` / score `400` | `1.200,00 / 12 = 100,000` | `100,00` |

Critério: `Math.Round(valor, 2, MidpointRounding.AwayFromZero)` — arredondamento comercial,
coerente com a prática do mercado financeiro.

---

## 5. `AprovarEmprestimo`

Regra: `valorParcela <= rendaMensal * 0,30`.

### 5.1 Classes de equivalência (com `rendaMensal = 5.000,00` → margem = `1.500,00`)

| # | Classe | Saída esperada | Caso |
| --- | --- | --- | --- |
| CE1 | `valorParcela < margem` | `true` | `1.499,99` |
| CE2 | `valorParcela == margem` (exatamente 30%) | `true` | `1.500,00` |
| CE3 | `valorParcela > margem` | `false` | `1.600,00` |
| CE4 (inválida) | `rendaMensal < 0` **ou** `valorParcela <= 0` | `ArgumentOutOfRangeException` | `-5.000,00` / `0,00` |

### 5.2 Valores de fronteira

| Renda | Parcela | % da renda | Resultado |
| --- | --- | --- | --- |
| `5.000,00` | `1.499,99` | 29,99% | `true` |
| `5.000,00` | `1.500,00` | 30,00% | `true` |
| `5.000,00` | `1.500,01` | 30,01% | `false` |
| `10.000,00` | `3.000,00` | 30,00% | `true` |
| `2.000,00` | `600,00` | 30,00% | `true` |
| `2.000,00` | `600,01` | 30,01% | `false` |
| `0,00` | `100,00` | — | `false` |

### 5.3 Teste de fluxo integrado

Valida as duas regras do serviço em sequência (calcular parcela → aprovar), garantindo
que não haja regressão na integração entre elas:

| `valorTotal` | `parcelas` | `score` | `rendaMensal` | Parcela calculada | Margem (30%) | Esperado |
| --- | --- | --- | --- | --- | --- | --- |
| `10.000,00` | `10` | `800` | `6.000,00` | `1.050,00` | `1.800,00` | `true` |
| `10.000,00` | `10` | `400` | `3.000,00` | `1.200,00` | `900,00` | `false` |
| `12.000,00` | `12` | `600` | `4.000,00` | `1.120,00` | `1.200,00` | `true` |

---

## 6. Resumo da cobertura de casos

| Classe de teste | Casos (`[InlineData]`/`[MemberData]`/`[Fact]`) |
| --- | --- |
| `ContaCorrenteServiceTests` | 28 |
| `EmprestimoServiceTests` | 34 |
| **Total** | **62** |

Os **11 casos `[InlineData]` exigidos pela lista de exercícios** estão implementados integralmente
(4 de taxa de manutenção, 2 de saque, 3 de parcela e 2 de aprovação) e os demais ampliam a
cobertura para fronteiras, entradas inválidas e fluxo integrado.

### Resultado da execução (`dotnet test`)

```text
Passed!  - Failed:     0, Passed:    62, Skipped:     0, Total:    62, Duration: 133 ms - FintechBank.Tests.dll (net10.0)
```

---

## 7. Critérios de aceite da qualidade

- [x] 100% dos testes unitários passando (`Failed: 0`).
- [x] Código identado com 4 espaços e sem avisos de compilação próprios (`.editorconfig`).
- [x] Métodos públicos documentados com XML Doc (`///`) em português.
- [x] Nomes de testes no padrão `Metodo_Cenario_ResultadoEsperado`.
- [x] Rastreabilidade entre requisito da lista e caso de teste (seção 6.5 do `README.md`).
- [x] Build e testes automatizados em CI (`.github/workflows/ci.yml`).
