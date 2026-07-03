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
    │   │   ├── Rules/ZoneGrowthRule.cs         # crescimento por desejabilidade/poluição/crime
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
    │   │   └── Actions/                        # BuildRoad, ZoneArea, Bulldoze, SetTaxRate
    │   ├── Events/                       # Observer / Pub-Sub (Simulação -> UI)
    │   │   ├── IEvent.cs / IEventBus.cs / EventBus.cs (copy-on-write)
    │   │   └── Notifications/SimulationEvents.cs
    │   ├── Data/                         # Factory + configuração orientada a dados
    │   │   ├── IDefinition.cs / Definitions.cs (Building/Vehicle/Infrastructure)
    │   │   ├── IDefinitionSource.cs / DefinitionRegistry.cs
    │   │   └── IEntityFactory.cs / EntityFactory.cs
    │   ├── Economy/                      # SOMENTE contratos (interfaces) nesta etapa
    │   │   ├── Money.cs
    │   │   └── EconomyContracts.cs             # IBudget/IMarket/ITaxPolicy/IEconomySystem/…
    │   ├── Presentation/                 # Contratos de View (implementados pela engine)
    │   │   ├── IRenderer.cs / Color32.cs / TileVisual.cs
    │   │   └── IProceduralSpriteFactory.cs / PlaceholderSpriteFactory.cs
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

## Economia (somente contratos)

Nada de lógica econômica nesta etapa — apenas as **interfaces** e os **structs de dados**
onde a matemática se conectará: `IEconomicAgent`, `IBudget`, `ILedger`, `IMarket`,
`ITaxPolicy`, `IEconomySystem` e o tipo `Money` (inteiro, determinístico). Um sistema
econômico futuro implementa esses contratos sem tocar no resto do core.

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
capacidade), object pooling, factory a partir de definições e uma verificação de **determinismo**
(duas execuções com a mesma semente produzem o mesmo resultado).

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
- [ ] **Crescimento populacional** e implementação da **economia** sobre os contratos existentes.
- [ ] **Serialização** de save/replay (o estado já é orientado a dados e determinístico).
- [ ] **Multiplayer lockstep** sobre o fluxo de comandos.
- [ ] Remoção estrutural completa em `FlowNetwork` (reciclagem de nós/arestas interiores).
