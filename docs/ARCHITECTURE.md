# Arquitetura — City Builder / Tycoon Isométrico 2D

Fundação de código **C# puro** e **engine-agnostic** para um simulador de cidade nos
moldes de SimCity 4 / OpenTTD. A camada de simulação é totalmente independente de
renderização e de qualquer engine, garantindo portabilidade futura para Unity ou Godot
e execução headless em um **Console Application**.

> **Implementação clean-room.** Nenhuma linha foi derivada do OpenTTD (GPL) nem de
> qualquer outro código proprietário. Metadados de veículos/edifícios/empresas são
> estritamente genéricos (ex.: `CompactHatch_Tier1`, `Industrial_Heavy_Level2`) — sem
> marcas, logotipos ou referências a produtos reais.

---

## Princípios de projeto

| Princípio | Como é aplicado |
|-----------|-----------------|
| **Separação Simulação × Apresentação** | `CityBuilder.Core` não referencia nenhum tipo de engine. A UI apenas **observa** eventos e lê estado (`IRenderer`, `IEventBus`). |
| **Composição sobre herança** | ECS leve: entidades são só `id + geração`; comportamento vive em *sistemas*, dados em *componentes* (structs). Zero árvores de herança profundas. |
| **Orientação a dados / cache-friendly** | Camadas (`GridLayer<T>`) e componentes (`ComponentStore<T>`) são arrays planos e compactos de `struct`, varridos linearmente pelos ticks. |
| **Determinismo** | Tempo em ticks inteiros, PRNG semeável (`DeterministicRandom`), dinheiro inteiro (`Money`). Mesma semente + mesmos comandos ⇒ mesmo estado (base para multiplayer lockstep e replay). |
| **Sem acoplamento a engine** | Nada de `MonoBehaviour`/`Node`. `CityBuilder.Core` roda em `dotnet run` sem UI. |

---

## Estrutura de diretórios

```
citybuilderteste/
├── CityBuilder.sln
├── docs/
│   └── ARCHITECTURE.md
└── src/
    ├── CityBuilder.Core/                 # Biblioteca de simulação (netstandard2.1, engine-agnostic)
    │   ├── Common/                       # Primitivas transversais
    │   │   ├── DeterministicRandom.cs        # PRNG semeável (determinismo/replay)
    │   │   ├── ObjectPool.cs / IPoolable.cs   # Object Pooling (veículos/pedestres/partículas)
    │   │   └── IsExternalInit.cs              # polyfill p/ `init` em netstandard2.1
    │   ├── Grid/                         # 1) Grid & Matemática Isométrica
    │   │   ├── GridCoord.cs / ScreenPoint.cs
    │   │   ├── IsometricProjector.cs          # grid <-> tela (2:1) + elevação + depth-sort
    │   │   ├── MapLayer.cs                     # camadas: Terrain/Underground/Surface/Zoning/…
    │   │   ├── GridLayer.cs / IGridLayer.cs    # campo 2D denso de structs
    │   │   ├── TerrainCell.cs
    │   │   └── WorldMap.cs                     # container das camadas
    │   ├── Simulation/                   # 2) Simulation Tick Engine (determinístico)
    │   │   ├── SimulationClock.cs              # fixed-timestep (independe do framerate)
    │   │   ├── SimulationScheduler.cs          # dispara sistemas em cadências variadas
    │   │   ├── ISimulationSystem.cs / TickContext.cs / TickInterval.cs
    │   ├── Ecs/                          # ECS leve (composição sobre herança)
    │   │   ├── Entity.cs / EntityRegistry.cs   # handles c/ geração + reciclagem
    │   │   ├── IComponent.cs / ComponentStore.cs (sparse-set) / EcsWorld.cs
    │   │   └── Components/                     # GridPosition, Movement, Building, Vehicle
    │   ├── Zoning/                       # 3) Zoneamento & Autômatos Celulares
    │   │   ├── ZoneType.cs / ZoneCell.cs
    │   │   ├── HeatMap.cs / HeatMapRegistry.cs / IHeatMapProvider.cs  # mapas de calor
    │   │   ├── ICellularAutomatonRule.cs / CellularAutomataEngine.cs   # CA c/ double-buffer
    │   │   ├── Rules/ZoneGrowthRule.cs         # regra base (desejabilidade/poluição/crime)
    │   │   ├── Rules/DemandGrowthRule.cs       # regra de produção: desejabilidade local + demanda RCI
    │   │   └── ZoningSystem.cs
    │   ├── Networks/                     # 4) Logística & Rede de Fluxo (grafos)
    │   │   ├── NetworkType.cs / NetworkIds.cs / NetworkElements.cs
    │   │   ├── IFlowNetwork.cs / FlowNetwork.cs (lista de adjacência)
    │   │   ├── IEdgeWeightProvider.cs          # pesos DINÂMICOS (congestionamento)
    │   │   └── RoadGridBuilder.cs              # helper p/ montar grade de ruas
    │   ├── Pathfinding/                  # Heurísticas & Algoritmos
    │   │   ├── IPathGraph.cs / PathNeighbor.cs / IHeuristic.cs / MinHeap.cs
    │   │   ├── AStarPathfinder.cs              # A* c/ pesos dinâmicos
    │   │   ├── DijkstraPathfinder.cs           # Dijkstra (A* com h=0)
    │   │   └── DijkstraMap.cs                  # Flow Field (serviços/multidões)
    │   ├── Traffic/                      # Tráfego & Movimento (agentes sobre a rede)
    │   │   ├── RouteTable.cs                   # rotas por veículo, buffers POOLED
    │   │   ├── VehicleSpawner.cs               # cria veículos + rota via A* (congestion-aware)
    │   │   ├── TrafficSystem.cs                # move agentes/tick, realimenta congestionamento
    │   │   └── TrafficSpawnSystem.cs           # spawn contínuo (cadência própria)
    │   ├── Utilities/                    # Utilidades (energia/água) — cobertura por flow field
    │   │   ├── UtilityData.cs                  # UtilitySource / UtilityConsumer / UtilityReport
    │   │   ├── UtilityGrid.cs                  # Dijkstra multi-fonte + alocação de capacidade
    │   │   └── UtilitySystem.cs                # resolve por tick lento + publica relatório
    │   ├── Commands/                     # Padrão Command (undo/redo + base multiplayer)
    │   │   ├── ICommand.cs / CommandResult.cs / CommandHistory.cs
    │   │   ├── ICommandProcessor.cs / CommandProcessor.cs
    │   │   ├── ICommandRecorder.cs             # gancho de gravação do fluxo de comandos
    │   │   └── Actions/                        # BuildRoad, ZoneArea, Bulldoze, SetTaxRate
    │   ├── Persistence/                  # Save binário + Replay determinístico
    │   │   ├── SaveGame.cs                     # snapshot v2: config+METADADOS+estado
    │   │   ├── SaveMetadata.cs                 # cabeçalho barato p/ a tela Load City
    │   │   ├── ReplayLog.cs                    # log (tick, ação) + ReplayRecorder
    │   │   ├── CommandCodec.cs                 # comandos <-> bytes (replay hoje, rede depois)
    │   │   ├── ReplayPlayer.cs                 # reaplica o log na mesma cadência
    │   │   └── StateChecksum.cs                # FNV-1a do estado (verificação/desync)
    │   ├── Events/                       # Observer / Pub-Sub (Simulação -> UI)
    │   │   ├── IEvent.cs / IEventBus.cs / EventBus.cs (copy-on-write)
    │   │   └── Notifications/SimulationEvents.cs
    │   ├── Data/                         # Factory + configuração orientada a dados
    │   │   ├── IDefinition.cs / Definitions.cs (Building/Vehicle/Infrastructure)
    │   │   ├── IDefinitionSource.cs / DefinitionRegistry.cs
    │   │   └── IEntityFactory.cs / EntityFactory.cs
    │   ├── Economy/                      # Ciclo econômico sobre os contratos
    │   │   ├── Money.cs / EconomyContracts.cs   # tipo Money + interfaces (IBudget/IMarket/…)
    │   │   ├── Budget.cs / Ledger.cs / Market.cs / TaxPolicy.cs / EconomicAgent.cs
    │   │   ├── EconomySettings.cs               # constantes tunáveis (impostos/manutenção)
    │   │   ├── EconomicAgentIds.cs              # ids fixos: cidade + setores no ledger
    │   │   └── EconomySystem.cs                 # impostos + mercados + manutenção -> tesouro
    │   ├── Population/                   # População & Demanda RCI (o laço de crescimento)
    │   │   ├── DemandModel.cs                   # deriva pop/empregos, calcula demanda R/C/I
    │   │   ├── DemandSettings.cs                # constantes do modelo (tunáveis)
    │   │   ├── SectorAccounts.cs                # domicílios/comércio/indústria via EconomicAgent+Ledger
    │   │   └── PopulationSystem.cs              # atualiza demanda, circula salários, publica
    │   ├── Presentation/                 # Contratos de View (implementados pela engine)
    │   │   ├── IRenderer.cs / Color32.cs / TileVisual.cs
    │   │   ├── IProceduralSpriteFactory.cs / PlaceholderSpriteFactory.cs
    │   │   └── AegeanMarbleTheme.cs            # tokens da identidade visual aprovada (1a)
    │   ├── Shell/                        # Fluxo pré-jogo (menus) — view-models engine-agnostic
    │   │   ├── GameShell.cs                    # máquina de telas Title/New/Load/Settings/InGame
    │   │   ├── NewCityForm.cs                  # "Found a New City" (nome/tamanho/seed/terreno)
    │   │   ├── GameSettings.cs                 # Settings c/ BACK-descarta / APPLY-comita + persistência
    │   │   ├── SaveCatalog.cs                  # lista de saves p/ "Load City" + tempo relativo
    │   │   └── GameInfo.cs                     # marca/copy: THE GAME OF POLIS, §, rodapé
    │   ├── ISimulationContext.cs         # superfície de acesso p/ comandos e sistemas
    │   ├── GameConfig.cs
    │   └── GameSimulation.cs             # COMPOSITION ROOT (liga tudo, dirige os ticks)
    │
    └── CityBuilder.App/                  # Host de console (net8.0) — prova headless
        ├── Program.cs                        # roda a simulação sem nenhuma engine
        ├── DemoTaxPolicy.cs                  # stub de ITaxPolicy só p/ demonstrar comando
        └── Particle.cs                       # ator de exemplo p/ o ObjectPool
```

---

## 1) Grid & Matemática Isométrica

- **`GridCoord`** — coordenada lógica inteira (`readonly struct`); é a unidade de posição
  da simulação. `ScreenPoint` é a projeção em pixels.
- **`IsometricProjector`** — conversão grid ⇄ tela em projeção **2:1** (diamante), com
  suporte a **elevação** e a uma **chave de profundidade** (`DepthKey`) para o
  *painter's algorithm*. Matemática pura → testável no console.
- **`WorldMap`** — mesmo footprint W×H com várias **camadas** empilhadas
  (`MapLayer`: `Terrain`, `Underground`, `Surface`, `Zoning`, `Structures`, `Overlay`).
- **`GridLayer<T>`** — campo 2D denso (array plano row-major) de `struct`, com indexador
  `ref` para leitura/escrita sem cópia e `Span<T>` para varreduras vetorizáveis.

## 2) Simulation Tick Engine

- **`SimulationClock`** — *fixed timestep* com acumulador: o host informa o delta real
  variável; o clock devolve quantos ticks fixos rodar. Multiplicador de velocidade
  (pause/1×/2×/3×) sem afetar o determinismo; trava anti-"spiral of death".
- **`ISimulationSystem`** — cada subsistema (tráfego, economia, população…) escolhe seu
  `TickInterval` (cadência) e recebe um `TickContext` determinístico.
- **`SimulationScheduler`** — em cada tick, executa apenas os sistemas cujo intervalo
  divide o tick atual. `StepOnce()` avança 1 tick determinístico (headless/testes/lockstep).

## 3) Zoneamento & Autômatos Celulares

- **`ZoneCell`** (4 bytes) — tipo, densidade, nível de desenvolvimento e ocupação.
- **`HeatMap`** — campo escalar (desejabilidade, valor da terra, poluição, crime, ruído,
  cobertura de serviço) com operadores de **difusão** e **decaimento**.
- **`CellularAutomataEngine`** — passo do autômato com **double-buffer** (regras leem o
  estado estável e escrevem em buffer temporário) → determinístico e independente de ordem.
- **`ICellularAutomatonRule`** — regra pura; `ZoneGrowthRule` faz o edifício crescer quando
  `desejabilidade + valor − poluição − crime` é positivo, limitado pelo teto da densidade.

## 4) Logística & Rede de Fluxo

- **`FlowNetwork`** — grafo em lista de adjacência por `NetworkType` (Road/Rail/Water/Power/
  Sewage). Nós recebem ids densos 0-based → servem de índice direto para o pathfinding.
  Implementa `IPathGraph` sem adaptadores. Suporta *checkpoint/restore* para undo LIFO.
- **`IEdgeWeightProvider`** — custo **dinâmico** por aresta. `CongestionWeightProvider` usa
  uma curva volume/atraso (estilo BPR): quanto mais carga vs. capacidade, maior o peso —
  então o A* naturalmente desvia de congestionamentos.

## Heurísticas & Algoritmos

- **`AStarPathfinder`** — A* sobre `IPathGraph` com heurística plugável (`Manhattan`,
  `Chebyshev`, `Euclidean`, `Zero`) e pesos dinâmicos. Buffers reutilizados entre chamadas
  com *stamp* de versão → **zero alocação** e sem reset O(N) por consulta.
- **`DijkstraPathfinder`** — Dijkstra = A* com `h=0` (composição, sem duplicar o laço).
- **`DijkstraMap` (Flow Field)** — Dijkstra multi-fonte: em **uma passada** calcula, para
  todo nó, a distância à fonte mais próxima e o próximo passo em direção a ela. Substitui
  milhares de buscas individuais (multidões seguem o campo; cobertura de bombeiros/polícia/
  hospital é lida direto).

## Tráfego & Movimento (milestone atual)

Primeira camada de agentes viva sobre a arquitetura — conecta pathfinding + ECS + rede +
tick engine + pooling em um laço fechado:

- **`VehicleSpawner`** — cria um veículo (via `EntityFactory`) e calcula sua rota com **A*
  usando os pesos de congestionamento vigentes**; um veículo que nasce num congestionamento
  já é roteado por fora dele. O buffer da rota vem do pool.
- **`RouteTable`** — guarda as rotas por veículo em `List<int>` **pooled** (Object Pooling
  aplicado às rotas): tráfego sustentado não gera lixo de GC.
- **`TrafficSystem`** (`ISimulationSystem`, cadência *Fast*) — a cada tick integra o progresso
  com `TickContext.DeltaSeconds` (determinístico), passa o agente de aresta em aresta,
  **atualiza a carga de cada aresta** (que realimenta o roteamento) e, na chegada, publica
  evento e recicla id da entidade + buffer da rota. Eventos de chegada são **adiados** para
  depois da varredura (não perturbam o `Span` em iteração).
- **`TrafficSpawnSystem`** (cadência própria, mais grossa) — mantém um fluxo contínuo até um
  teto, demonstrando o escalonador rodando sistemas em frequências diferentes.

O laço de realimentação (veículos → carga nas arestas → pesos do A* → novas rotas desviam) é
totalmente determinístico: o app headless roda o mesmo cenário duas vezes com a mesma semente
e compara `(células desenvolvidas, chegadas, spawns, consumidores atendidos)`.

## Utilidades & Cobertura de Serviço (milestone atual)

Energia/água modeladas como **cobertura por flow field de Dijkstra** — o mesmo algoritmo dos
mapas de serviço (bombeiros/polícia/hospital):

- **`UtilityGrid`** — um serviço (energia OU água) sobre sua `FlowNetwork`. A cobertura é um
  **Dijkstra multi-fonte** a partir dos nós de fonte: em **uma passada** todo nó aprende o
  custo até a fonte mais próxima, então "este consumidor está conectado e no alcance?" é O(1)
  — sem uma busca por consumidor. A capacidade é então alocada **do mais próximo ao mais
  distante**, gerando *brownouts* realistas quando a demanda supera a oferta. Determinístico
  (sem RNG; ordenação estável por distância).
- **`UtilitySource` / `UtilityConsumer` / `UtilityReport`** — structs de dados: ponto de oferta
  (nó + capacidade), ponto de demanda (nó + consumo, marcado como atendido pelo solve) e o
  relatório da rede (oferta, demanda, demanda alcançável, demanda atendida, *brownout*).
- **`UtilitySystem`** (`ISimulationSystem`, cadência lenta) — resolve cada grid por tick e
  publica um `UtilityUpdatedEvent`. A UI lê para os painéis de energia/água e avisos de apagão.

Reaproveita `DijkstraMap` sem qualquer código novo de pathfinding — a prova de que a heurística
de flow field da fundação serve tanto para multidões quanto para serviços/utilidades.

## Padrões de Projeto

- **Object Pooling** — `ObjectPool<T>` (+ `IPoolable`) recicla atores de alta rotatividade
  sem pressionar o GC. Ids de entidade também são reciclados pelo `EntityRegistry`.
- **Factory** — `IEntityFactory`/`EntityFactory` montam entidades **compondo componentes** a
  partir de `IDefinition` (dados). Fonte de dados abstrata (`IDefinitionSource`): JSON,
  ScriptableObject ou catálogo em código — o core não sabe a origem.
- **Command** — todo input do jogador é um `ICommand` (Execute/Undo). Habilita **undo/redo**,
  **log de ações** e a base para **multiplayer lockstep** (serializar e reaplicar o mesmo
  fluxo ordenado de comandos). `CommandProcessor` valida, executa, registra e notifica.
- **Observer / Pub-Sub** — `EventBus` (copy-on-write, sem alocação no publish) é a única via
  da simulação para a UI. A UI **observa**; a simulação nunca chama a UI.

## Economia (milestone atual)

Ciclo econômico da cidade implementado **sobre os contratos** já existentes, amarrando os
milestones anteriores num laço financeiro:

- **`EconomySystem`** (`IEconomySystem`, cadência lenta) — a cada tick econômico: **taxa** as
  zonas desenvolvidas (base tributária por tipo × nível × alíquota), **equilibra os mercados**
  de trabalho/bens a partir da oferta/demanda das zonas, **fatura** o serviço de utilidades e
  **cobra manutenção** de utilidades + infraestrutura viária, então **liquida o tesouro** e
  publica `BudgetChangedEvent` + `MarketClearedEvent`.
- **`Budget`** (`IBudget`) — tesouro: receitas/despesas acumulam por período; `Settle()` aplica
  o líquido ao saldo e reinicia o período (totais por categoria para o painel).
- **`Market`** (`IMarket`) — preço de equilíbrio a partir da razão demanda/oferta (limitada).
- **`TaxPolicy`** (`ITaxPolicy`) — alíquotas por zona, mutáveis por **comando** do jogador
  (`SetTaxRateCommand` já opera sobre este contrato — comando → economia, com undo).
- **`Ledger`** (`ILedger`) e **`EconomicAgent`** (`IEconomicAgent`) — registro de transações e
  agente genérico portador de fundos, prontos para agentes por-empresa/domicílio.

Tudo em `Money` inteiro e **determinístico** (somas independentes de ordem; sem RNG). A
economia **observa** as utilidades via evento — não as consulta diretamente — mantendo o
acoplamento fraco. Ordem de execução no tick: utilidades resolvem antes da economia, então a
economia lê a cobertura mais recente.

## Persistência & Replay (milestone atual)

A materialização do investimento em determinismo — e a fundação direta do multiplayer lockstep:

- **`SaveGame`** — snapshot **binário** do estado persistente (config, tick, estado do RNG,
  terreno, zoneamento, mapas de calor, redes com ids preservados, grids de utilidade, tesouro,
  alíquotas e edifícios), campo a campo, sem reflexão nem dependências externas. Estado
  **transiente** (veículos em trânsito, rotas, congestionamento, preços de mercado) **não** é
  salvo por decisão de projeto: os sistemas o regeneram após o load. Carregar é **reconstruir**:
  `ReadConfig` → construir a simulação → mesmo bootstrap de um jogo novo (definições/sistemas,
  sem conteúdo de mundo) → `ReadInto`. Edifícios renascem **pela factory**, então o load emite
  os mesmos eventos de construção — a view se reconstrói observando, como sempre.
- **`ReplayLog` + `ReplayRecorder`** — o `CommandProcessor` ganhou um gancho
  (`ICommandRecorder`) que captura cada ação bem-sucedida como `(tick, ação)`; undo/redo entram
  como marcadores (o processador repõe da própria história no replay).
- **`CommandCodec`** — formato de fio dos comandos (id numérico + payload por tipo). Serve ao
  arquivo de replay hoje e a pacotes de rede (lockstep) amanhã; o leitor religa comandos aos
  serviços vivos (ex.: `SetTaxRateCommand` → política tributária da simulação alvo).
- **`ReplayPlayer`** — reaplica o log num mundo recém-bootstrapado **na mesma cadência**
  (roda ticks até o tick gravado, aplica a ação). Trocar "ler do arquivo" por "ler da rede"
  transforma esse laço num cliente lockstep.
- **`StateChecksum`** — digest FNV-1a 64-bit do estado persistente. Save/load e replay são
  verificados comparando um número; em multiplayer, é o detector de dessincronização (peers
  trocam checksums a cada N ticks).

O demo headless prova as duas pontas: snapshot → load com checksum idêntico, e sessão gravada
(zonear, subir imposto, undo) → serializar → replay com checksum idêntico.

## População & Demanda RCI (milestone atual)

O **laço central de um city builder**: zonas → pessoas → demanda → crescimento → mais pessoas.
Conecta zoneamento, economia e (indiretamente) tráfego/utilidades num ciclo que se auto-regula.

- **`DemandModel`** — deriva do zoneamento desenvolvido a **população**, a **força de trabalho**
  e os **empregos**, e calcula a demanda **Residencial/Comercial/Industrial** em `[-1,1]`:
  residencial sobe com empregos não preenchidos + desejabilidade; comercial sobe com população
  não atendida; industrial sobe com exportação-base + mão de obra ociosa. **Impostos suprimem**
  a demanda de cada categoria — então o comando de imposto passa a dirigir o **crescimento**,
  não só a receita. Tudo determinístico (quantidades inteiras, razões em ordem fixa).
- **`PopulationSystem`** (cadência lenta, roda **antes** do zoneamento) — recalcula a demanda a
  partir do estado atual, circula os salários pelos setores e publica `PopulationChangedEvent` +
  `DemandChangedEvent` (as barras RCI da UI).
- **`DemandGrowthRule`** — a regra de produção do autômato celular: uma célula cresce quando o
  **sinal local** (desejabilidade/valor − poluição/crime) **e** a **demanda RCI global** da sua
  categoria são favoráveis, e decai quando ficam negativos — limitada pelo teto da densidade.
  Combinar o espacial (heat-maps) com o global (demanda) é o que faz os bairros preencherem onde
  são ao mesmo tempo desejados e agradáveis. Substitui a `ZoneGrowthRule` na composição.
- **`SectorAccounts`** — os "agentes por-empresa/domicílio" na granularidade de pool: três
  `EconomicAgent`s (domicílios/comércio/indústria) que a cada tick **fazem o dinheiro circular**
  pelo `Ledger` compartilhado (empregadores pagam salários → domicílios consomem no comércio →
  comércio reabastece na indústria). Transferências são atômicas (débito+crédito) e puladas se o
  pagador não pode arcar, então o ledger sempre fecha.

Ordem no tick: **população → zoneamento → tráfego → utilidades → economia**, para o crescimento
ler a demanda fresca. O check de determinismo do demo agora compara também a população final.

## Shell de Jogo & Identidade Visual "Aegean Marble"

O jogo agora tem nome — **THE GAME OF POLIS** — e uma direção visual aprovada (handoff em
`docs/design/main-menu/`, opção **1a "Aegean Marble"**: pergaminho, serifadas Marcellus/Lora,
filetes dourados, azul Egeu). A integração segue a regra do handoff: *recriar no ambiente-alvo,
não copiar o HTML* — então o Core ganhou a camada engine-agnostic que qualquer frontend liga:

- **`AegeanMarbleTheme`** — todos os tokens do handoff como dados (`Color32` + escala
  tipográfica + métricas de layout). Fonte única de verdade para Unity/Godot/web reproduzirem
  os menus pixel-perfect.
- **`GameShell`** — máquina de telas do protótipo (`Title/NewCity/LoadCity/Settings/InGame`)
  com eventos de navegação; a view só renderiza `Screen` e chama os métodos dos botões.
- **`NewCityForm`** — o formulário "Found a New City": nome (padrão *Nova Polis*), cartões de
  tamanho (`Hamlet 64 / Township 128 / Metropolis 256`), seed com RANDOMIZE (6 dígitos; texto
  não-numérico vira seed por hash FNV) e preset de terreno. `CreateConfig()` → `GameConfig`
  (que agora carrega **nome da cidade** e **terreno**).
- **`TerrainGenerator`** — geração procedural **determinística** por preset (*Verdant Plains /
  River Delta / Coastal Reach / Highlands*): value-noise por hash inteiro (sem transcendentais
  → bit-idêntico em qualquer plataforma). Roda na fundação; loads restauram do snapshot.
- **`GameCalendar`** — tick → "Year 4 — Spring" (aritmética inteira), usado na tela Load e em
  modificadores sazonais futuros.
- **`SaveGame` v2 + `SaveMetadata` + `SaveCatalog`** — o save ganhou um **bloco de metadados**
  (nome, população, tesouro, tick, salvo-em) legível **sem carregar o mundo**; `SaveCatalog`
  varre a pasta de saves (`*.polis`) e entrega as linhas da tela Load já ordenadas
  ("Population 12,480 · § 45,120 · Year 4 — Spring" + tempo relativo).
- **`GameSettings`** — o modelo persistido da tela Settings (áudio/gráficos/gameplay, com os
  defaults do design) e a semântica exata BACK-descarta / APPLY-comita (`BeginEdit/Apply/Discard`).
- **`Money`** agora exibe o glifo **§** do design ("§ 45,120"); `GameInfo` centraliza a marca
  (título, tagline, versão, rodapé) para todos os frontends.

O demo headless percorre o fluxo inteiro: fundar *Nova Polis* (Township, seed 314159, Verdant
Plains) → gerar terreno (censo por preset) → crescer → salvar duas cidades → listar como a tela
Load City → Settings com BACK/APPLY + round-trip de persistência.

## Apresentação / Placeholders

- **`IRenderer`** — contrato de desenho implementado pela engine (a simulação **não** o chama).
- **`IProceduralSpriteFactory` / `PlaceholderSpriteFactory`** — geram **primitivas coloridas**
  (diamante/prisma isométrico) a partir dos dados: terreno, zonas (residencial=verde,
  comercial=azul, industrial=amarelo; prédio extruda conforme cresce), veículos e redes.
  Trocar por arte real depois = outra implementação; a simulação não muda.

---

## Como rodar (headless)

```bash
dotnet run --project src/CityBuilder.App
```

O programa exercita, sem nenhuma engine: pub/sub de eventos, comandos (zonear, construir
estrada, imposto) com undo/redo, ticks fixos determinísticos, crescimento por autômato
celular, A* + flow field de Dijkstra, **tráfego** (veículos roteados que se movem, criam
congestionamento e desviam), **utilidades** (cobertura de energia por flow field + brownout por
capacidade), **economia** (impostos → tesouro, mercados, manutenção; comando de imposto com
undo movendo a receita), **persistência** (save binário → load com checksum idêntico),
**replay** (log de comandos serializado reproduzindo o estado exato), object pooling, factory a
partir de definições e uma verificação de **determinismo** (duas execuções com a mesma semente
produzem o mesmo resultado).

> **Compatibilidade de framework.** `CityBuilder.Core` mira **`netstandard2.1`** para ser
> consumível por Unity (Mono/IL2CPP) e Godot 4 (.NET); o host de console mira `net8.0`.

## Portando para Unity / Godot

1. Referencie `CityBuilder.Core` (ou inclua o código-fonte).
2. Crie um bootstrap na engine que instancie `GameSimulation` e chame `Update(deltaReal)`
   no loop de frame — substituindo o `Program.cs`.
3. Implemente `IRenderer` com a API de desenho da engine e **assine** o `IEventBus` para
   refletir mudanças na UI. Traduza `ScreenPoint`/`Color32`/`TileVisual` para os tipos da engine.
4. A camada de simulação permanece **intacta e determinística**.

## Roadmap

- [x] **Tráfego & movimento** — agentes roteados por A*, congestionamento realimentado, pooling de rotas.
- [x] **Utilidades** (energia/água) — cobertura por flow field de Dijkstra + alocação de capacidade (brownout).
- [x] **Economia** — impostos/mercados/manutenção → tesouro, sobre os contratos; comando de imposto integrado.
- [x] **Persistência & Replay** — save binário, log de comandos serializável, replay na mesma cadência, checksum de estado.
- [x] **Shell & identidade visual** — tokens "Aegean Marble", máquina de telas, New City/Load/Settings, terreno procedural, calendário, save v2 c/ metadados.
- [x] **Crescimento populacional & demanda RCI** — modelo de demanda dirige o crescimento; setores circulam dinheiro via `EconomicAgent`/`Ledger`.
- [ ] **Gerenciamento de cidades** (biblioteca CRUD, autosave, seeds/founding codes, import/export, thumbnails) — **plano aprovável em [`docs/plans/city-management.md`](plans/city-management.md)**.
- [ ] **HUD in-game** (fase 2 do design — o handoff marca o ponto de entrega no stub in-game).
- Multiplayer lockstep: **despriorizado** por decisão de produto; codec/replay/checksum permanecem como infraestrutura de replay/verificação.
- [ ] Remoção estrutural completa em `FlowNetwork` (reciclagem de nós/arestas interiores).
