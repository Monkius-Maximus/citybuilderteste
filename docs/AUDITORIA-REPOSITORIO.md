# Auditoria do repositório — o código existente versus o plano documentado

**Data:** 2026-09-19
**Repositório auditado:** `Monkius-Maximus/citybuilderteste` (8 commits, branch `main`)
**Fontes lidas:** `README.md` e `docs/ARCHITECTURE.md`. O código-fonte em `src/`
não foi lido — as conclusões abaixo vêm da documentação do próprio repositório.

> **Status: decidido em 2026-10-09 — Opção A (dois repositórios).** Ver §9.
> Este repositório é o jogo. A documentação do editor está em `docs/editor/`
> apenas em trânsito, até ser movida para o repositório próprio do editor.

---

## 1. O que existe no repositório

Não é um esqueleto. É um engine de simulação com escopo avançado:

| Subsistema | Estado declarado |
|---|---|
| Grid e matemática isométrica (projeção 2:1, elevação, depth-sort) | Implementado |
| Tick engine determinístico de passo fixo, escalonador por cadência | Implementado |
| ECS leve (entidade com geração, componentes em sparse-set) | Implementado |
| Zoneamento com autômatos celulares e mapas de calor | Implementado |
| Redes de fluxo (Road/Rail/Water/Power/Sewage) com pesos dinâmicos | Implementado |
| Pathfinding: A*, Dijkstra, flow fields | Implementado |
| Tráfego com congestionamento realimentado | Concluído no roadmap |
| Utilidades com alocação de capacidade e brownout | Concluído no roadmap |
| Economia: impostos, mercados, manutenção, tesouro | Concluído no roadmap |
| Persistência binária, replay de comandos, checksum de estado | Concluído no roadmap |
| Padrões: Object Pool, Factory por dados, Command com undo/redo, Pub-Sub | Implementado |

Estrutura: `src/CityBuilder.Core` (`netstandard2.1`, sem dependência de engine) e
`src/CityBuilder.App` (`net8.0`, host de console headless).

O repositório declara implementação clean-room, sem derivação de código GPL ou
proprietário, e metadados genéricos de veículos e edifícios.

**Avaliação:** é trabalho bem estruturado. Determinismo levado a sério,
separação simulação×apresentação respeitada, escolhas de performance
conscientes. O problema apontado a seguir não é de qualidade.

---

## 2. A contradição central

Durante o planejamento, a pergunta de inventário — "existe engine de simulação
pronto?" — foi respondida com **"não temos nada de engine pronto"**. As Sprints 0
e 1 foram desenhadas a partir dessa resposta, do zero.

Existe um engine pronto, e ele está adiantado.

Mais: ele implementa justamente o conjunto que a memória do projeto (§9) lista
como **fora de escopo por decisão** — zonas simuladas, tick loop, economia,
orçamento, demanda, tráfego — além da fundação de multiplayer lockstep, que
estava fora do MVP inclusive na revisão 1.

---

## 3. Colisões concretas com o plano documentado

### 3.1 O tile é a unidade de posição

`GridCoord` é descrito como coordenada lógica inteira e **a unidade de posição da
simulação**. A projeção isométrica é 2:1 com elevação. Não há metro em nenhuma
parte da arquitetura descrita.

Isto é exatamente o defeito que originou o projeto (memória §1): quando o tile é
a unidade de tudo, não existe noção de metro, e a escala relativa entre casa,
carro e pessoa não tem conserto a jusante.

Colide com: §4.3 (três espaços separados), §4.4 (`metersPerTile = 8`), §4.6
(vias como polilinhas métricas), §4.7 (terreno amostrado nos vértices).

### 3.2 A stack não é Godot

`netstandard2.1` no Core, `net8.0` no host de console, com portabilidade futura
para Unity ou Godot declarada como intenção. Não há projeto Godot.

Colide com: §4.2, que trava Godot 4.x / C# / .NET 8 e assume a convenção
1 unidade = 1 metro. O `netstandard2.1` também restringe recursos de linguagem —
o repositório já precisou de um polyfill `IsExternalInit.cs` para `init`.

### 3.3 O padrão de comando é incompatível — conflito mais profundo

| | Repositório | Spec documentada |
|---|---|---|
| Forma | `ICommand` com `Execute`/`Undo` | União fechada de dados |
| Comportamento | Vive dentro do comando | Vive em `validate` / `apply` externos |
| Estado | Mutação no lugar | Documento imutável, novo a cada `apply` |
| Undo | Operação inversa | Documento anterior |

As duas abordagens são legítimas. Misturá-las não é: undo por inverso e undo por
snapshot fazem suposições opostas sobre quem é dono do estado.

Colide com: §4.11 (pipeline de direção única), §4.12 (undo de graça pela
imutabilidade), e com a guarda de exaustividade da §4.2.

---

## 4. O que converge

Nem tudo colide. Partes do repositório já são o que o plano pediria:

- `EventBus` copy-on-write como única via simulação→UI — é a §4.11 na direção
  certa, e o envelope fechado de eventos é uma evolução natural dele.
- `IDefinition` / `IEntityFactory` com fonte de dados abstrata — é praticamente o
  catálogo declarativo da §4.14. Falta o `footprint` em metros.
- Separação simulação×apresentação com `IRenderer` implementado pela engine — é a
  §4.3 no que toca ao espaço de tela.
- Determinismo, PRNG semeável, dinheiro inteiro, checksum de estado — nada disso
  precisa ser refeito, e sustenta o round-trip de persistência.
- Clean-room declarado — coerente com a §4.1.

---

## 5. O encaixe possível

A §4.5 já estabelece:

```
CityDocument --rasterize--> grid de tiles
```

O engine do repositório **consome grid de tiles**. Ele não precisa virar métrico:
precisa receber a saída do `rasterize`.

Sob essa leitura, as duas coisas compõem em camadas em vez de competir:

```
Editor (metros)  →  CityDocument  →  rasterize  →  grid  →  CityBuilder.Core
     autoria           verdade        projeção            simulação/jogo
```

Se isso for aceito, nenhum código do repositório é desperdício e nenhuma decisão
do plano precisa ser revogada. O que muda é o escopo de cada um: o repositório
deixa de ser "o projeto" e passa a ser **o jogo**, um dos consumidores.

---

## 6. Opções avaliadas

Três caminhos. Nenhum é obviamente certo, e a escolha muda tudo o que vem depois.

### Opção A — dois repositórios
`citybuilderteste` é o jogo. O editor métrico nasce em repositório próprio, e o
jogo passa a consumir o export.
*A favor:* fronteira limpa, cada projeto com sua stack e seu ritmo.
*Contra:* dois codebases para manter sozinho.

### Opção B — um repositório, projetos separados
Entram `CityBuilder.Document` (modelo métrico) e `CityBuilder.Editor` (Godot) no
mesmo repositório. `CityBuilder.Core` fica intocado, recebendo grid rasterizado.
*A favor:* uma história de commits, reaproveita `.sln` e CI.
*Contra:* o repositório passa a hospedar duas filosofias de comando convivendo.

### Opção C — abandonar o modelo métrico
Seguir com o engine em tiles como está.
*A favor:* mais barato, e há muito pronto.
*Contra:* devolve o defeito de escala que originou o projeto. A queixa do carro
do tamanho da casa volta inteira.

**Recomendação registrada:** Opção B, se o editor for prioridade agora; Opção A,
se o jogo for. A Opção C só faz sentido se o propósito de autoria tiver mudado.

---

## 7. Efeito sobre os documentos existentes

| Documento | Situação |
|---|---|
| `handoff/SPRINT-0-HANDOFF.md` | **Suspenso.** Manda criar solução e projetos que já existem, com outro nome e outro padrão de comando. |
| `CLAUDE.md` | Válido como descrição do **editor**. Não descreve o repositório como ele está hoje. |
| `MEMORIA-PROJETO.md` | Válido, exceto §9 — o que está listado como fora de escopo já foi construído. |
| `LINHA-DE-BASE.md` | Camada A, item 1 ("não existe engine de simulação pronto") está **falsificado**. Corrigir na próxima revisão. |
| `spec/*.ts` | Válidos como contrato do documento métrico. Independentes desta decisão. |
| `adr/adr-001`, `adr/adr-002` | Válidos. Nenhum depende do engine. |

---

## 8. Pergunta em aberto

Não foi respondido se este repositório foi construído antes desta conversa ou em
paralelo a ela. A resposta decide se o que existe é **uma alternativa a
considerar** ou **uma duplicação a reconciliar** — e isso pesa na escolha entre
A, B e C.

*Nota de 2026-10-09:* o merge que trouxe o engine veio do branch
`claude/simcity-game-architecture`, o que indica uma sessão de Claude Code em
paralelo ao planejamento. A pergunta perdeu peso com a escolha da Opção A, que
mantém os dois trabalhos.

---

## 9. Decisão — Opção A, dois repositórios

**Data:** 2026-10-09 · **Decidido por:** Diogo

`citybuilderteste` é **o jogo**: simulador em tiles, com todo o escopo de
simulação que ele já tem. O **editor métrico** ganha repositório próprio, e o
jogo passa a ser um dos consumidores do export do editor.

Nenhuma decisão do plano do editor foi revogada. A §9 da memória do projeto
("tick, economia e tráfego fora de escopo") continua valendo, **para o editor**:
esse escopo não foi descartado, ele mora no jogo.

### Consequências imediatas

| Item | Situação |
|---|---|
| `CLAUDE.md` na raiz | Reescrito para descrever o jogo e a fronteira com o editor |
| Documentação do editor | Movida para `docs/editor/`, em trânsito |
| `docs/editor/handoff/SPRINT-0-HANDOFF.md` | Volta a ser válido, **somente** no repositório do editor |
| `src/` | Intocado |

### Próximos passos

1. Criar o repositório do editor (vazio).
2. Mover `docs/editor/*` para lá: `docs/editor/CLAUDE.md` vira o `CLAUDE.md` da
   raiz, o restante vai para `docs/`.
3. Remover `docs/editor/` daqui.
4. Executar o Sprint 0 do editor no repositório novo.

### Contrato entre os dois — em aberto

O editor exporta `tiles/` com escala declarada no `manifest.json`
(`metersPerTile = 8`). O jogo usa `GridCoord` inteiro sem noção de metro. Falta
decidir como o jogo importa esse grid e se o tile do jogo passa a significar
8 m. Não é bloqueante para nenhum dos dois agora — vira bloqueante quando o
Sprint 7 do editor (export) começar.
