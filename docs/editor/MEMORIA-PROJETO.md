# Memória do Projeto — Editor de Cidades / Mapas

> Documento de contexto durável. Serve para reabrir o projeto em qualquer
> conversa futura sem repetir o histórico. Atualizar a cada decisão travada
> ou destravada.

**Revisão:** 6 · **Data:** 2026-10-09
**Mudança desde a rev. 5:** editor e jogo separados em dois repositórios
(Opção A da auditoria). O `citybuilderteste`, simulador em tiles já existente, é
o jogo e consome o export do editor. Nenhuma decisão do editor mudou.

*Rev. 5:* auditoria de proveniência (ver `LINHA-DE-BASE.md`). Afirmações não
verificadas sobre codebases de terceiros foram removidas ou reescritas como
princípios próprios.

*Rev. 4:* correção — `TreatWarningsAsErrors` NÃO garante exaustividade de
comandos em C#. Guarda passa a ser teste de reflexão (§4.2).

*Rev. 3:* ADR-002 define o pipeline de asset (pré-render offline). O bloqueio
"2D ou 3D" se dissolve parcialmente e é rebaixado para decisão de editor.

*Rev. 2:* propósito invertido — autoria é primária, jogo é consumidor. Stack
definida (Godot / C#). Licença resolvida por processo. Hidrografia e export em
camadas entram no modelo.

---

## 1. O que é o projeto

Um **editor de cidades e mapas** proprietário. O produto é a cidade autorada,
não a partida jogada.

Ordem de prioridade, explícita:

1. **Ferramenta de autoria** — terraformar, editar rios, traçar vias, definir
   zonas, construir casas e bairros, posicionar objetos de mundo (estátuas,
   decorações), e **exportar** o resultado para alimentar outros projetos.
2. **Jogo** — city builder jogável, construído depois, como um segundo
   consumidor do mesmo documento. Não altera o modelo de dados.

O jogo é possível, não obrigatório. Se nunca for feito, a ferramenta continua
cumprindo o propósito inteiro.

### A dor de origem

Ferramentas existentes (SimCity 4, TheoTown, Cities: Skylines, Tiled) não
exportam cidades para outros projetos, têm implicações de direito autoral, e
sobretudo **erram a escala de forma irreparável**: em TheoTown uma casa ocupa um
tile, e por consequência pessoas e veículos precisam ser desenhados grandes
demais para serem visíveis — um carro acaba do tamanho de uma casa.

A causa raiz: nesses jogos **o tile é a unidade de tudo**. Nunca existiu uma
noção de metro no sistema, só de célula. Não há conserto a jusante.

O modelo mental correto é o de mapas de jogos como GTA San Andreas: o mundo é
autorado em software externo, em escala real, e depois consumido pelo jogo.
Não o contrário.

---

## 2. Onde este projeto se encaixa entre as ferramentas existentes

Registrado porque a pergunta "isto é uma terceira ferramenta redundante?" foi
levantada e respondida. Não é. As três ocupam camadas distintas:

| Ferramenta | Camada | Autora |
|---|---|---|
| City-searcher-tool | Dados | Gazetteer, dossiês, arquétipos de cidades brasileiras |
| Ferramenta de Mundo | Entidades | Clubes, elencos, geografia competitiva |
| **Este projeto** | **Geometria** | **Terreno, hidrografia, vias, zonas, volumes — em metros** |
| `citybuilderteste` | Simulação (jogo) | Tick, zoneamento, redes, tráfego, economia — em tiles. Consome o export deste projeto |

Nenhuma das outras modela geometria métrica. É exatamente o vazio que
este projeto preenche, e é o que o Tiled não entrega na escala desejada.

---

## 3. Bloqueio ativo

### 🟡 Renderização do editor — em aberto, não bloqueante

Era "2D ou 3D" e estava marcado como bloqueante. Eram **duas perguntas
empilhadas**, e o ADR-002 dissolveu a maior delas.

*Resolvido:* o export não precisa escolher. Autoria em 3D, pré-render offline
para atlas de sprites, e o consumidor recebe geometria métrica mais PNG comum.
Atende 2D e 3D ao mesmo tempo (§4.16).

*Resta:* o editor desenha em 3D ou em 2D isométrico? Como o documento e o export
são agnósticos nos dois casos, isto virou decisão de ferramenta — reversível, e
não trava o Sprint 1.

Recomendação registrada, ainda não travada: **editor em 3D**. Terraformar e
traçar rio sem ver relevo em três dimensões é trabalhar às cegas, e Godot entrega
o básico pronto.

Decidir até o Sprint 2, quando o terreno começa.

---

## 4. Decisões travadas

Estas são fundacionais. Mudá-las invalida trabalho já feito.

### 4.1 Licença — resolvida por processo

Não haverá leitura de código-fonte de projeto de referência. Só documentação
pública e comportamento observado em uso. Clean room formal pressupõe duas pessoas (uma lê, outra
escreve a spec) e o projeto é solo — então a separação vira simplesmente **não
ler**.

Custo praticamente nulo: as decisões estruturais já tomadas não vieram do código
de ninguém, vieram de raciocinar sobre o problema.

Ressalva: não é aconselhamento jurídico. É a via que dispensa precisar de um.

### 4.2 Stack — Godot 4.x, C#, .NET 8

Alinhado ao resto dos projetos. A equivalência 1 unidade = 1 metro é
**convenção adotada pelo projeto**, não regra imposta pelo engine — nada no
Godot obriga isso. Adotada, o espaço de mundo do documento mapeia direto, sem
camada de conversão.

**Consequências da tradução de TypeScript para C#:**

- `Command` como união discriminada **não existe** em C#, e o substituto óbvio
  **não funciona**. Hierarquia `abstract record` com construtor privado +
  `switch expression` emite CS8509 *mesmo com todos os casos cobertos*: o
  compilador não prova que a hierarquia é fechada, porque o construtor de cópia
  protegido dos `record` admite herança externa em tese. Com
  `TreatWarningsAsErrors`, um switch completo falharia o build.

  **Guarda adotada: teste de reflexão.** Enumera os subtipos de `Command` no
  assembly e afirma que cada um é tratado por `Validate` e `Apply`. Adicionar
  comando sem tratá-lo quebra o CI. Regra de código: **nunca usar braço `_ =>`**
  em switch sobre `Command` — o descarte esconde exatamente o erro procurado.

  `TreatWarningsAsErrors` fica ligado por higiene; só não é ele que garante
  exaustividade. Alternativa avaliada e não adotada: analisador externo
  (`ClosedTypeHierarchyDiagnosticSuppressor` e similares) daria garantia em
  compile time, ao custo de dependência de terceiro no build.

  C# 15 / .NET 11 traz `union` com exaustividade real do compilador. Não está
  disponível: Godot 4.x roda em .NET 8. Reavaliar quando mudar.
- `Meters` / `TileIndex` viram `readonly record struct`. Fica melhor que em TS:
  deixa de ser marcação de tipo e passa a ser tipo de verdade.
- `Uint8Array` / `Float32Array` viram `byte[]` / `float[]`.

Os arquivos `city-document-v1.ts` e `engine-pipeline.ts` deixam de ser
implementação e passam a ser **especificação a portar**. Continuam válidos como
contrato; nenhum deles roda.

### 4.3 Três espaços separados

O defeito de escala se corrige separando o que os jogos de referência fundem:

| Espaço | Unidade | Papel |
|---|---|---|
| **Mundo** | metros (float) | A verdade. Casa 8×10m, carro 4.5×1.8m, pessoa 0.5m |
| **Simulação / grid** | tiles | Amostragem, usada por export e (futuramente) por jogo |
| **Tela** | pixels | Apresentação pura |

Consequência: exportar deixa de ser conversão e vira leitura direta — o mundo já
está em metros.

### 4.4 `metersPerTile = 8`

Casa unifamiliar ocupa ~1 célula; quarteirão de 80m cai em 10 células — é este
o argumento, e ele se sustenta sozinho.

Convergência registrada: Cities: Skylines também usa célula de 8×8m (verificado,
ver `LINHA-DE-BASE.md` §C1). Convergência não é prova; o raciocínio acima é.

**Alterar este valor invalida o documento.** Não há migração de escala: a
rasterização é lossy e reconstruir seria adivinhação.

### 4.5 `CityDocument` é a verdade; tudo o mais é projeção

```
CityDocument --rasterize--> grid de tiles
CityDocument --resolve-->   terreno final
CityDocument --export-->    camadas de saída
```

Todas unidirecionais. **Não existe `derasterize`.** Nenhum código reconstrói o
documento a partir de uma projeção.

### 4.6 Vias como grafo de polilinhas métricas, não células pintadas

É a decisão que torna o documento exportável. Polilinha métrica vira GeoJSON,
spline ou navmesh direto. Malha viária em tiles não vira nada sem heurística de
reconstrução.

Cruzamento = node compartilhado. Duas edges cruzando sem node compartilhado são
viaduto — distinção geométrica, nunca inferida.

### 4.7 Terreno amostrado nos vértices

`(cols+1) × (rows+1)` amostras descrevem `cols × rows` quadriláteros com os
quatro cantos definidos sem ambiguidade. Amostrar no centro obrigaria a
interpolar para achar bordas — e borda é exatamente o que a validação de declive
mede.

`sampleSpacing` é **independente** de `metersPerTile`. Terreno de 4m com grid de
8m é legítimo; nenhum subsistema contamina o outro.

### 4.8 Hidrografia é camada própria, e o terreno final é derivado

Rio não é rua com textura de água. É polilinha com largura variável, direção de
fluxo, e modifica o terreno.

**Escavar o rio destrutivamente no heightmap é proibido.** Reeditar o traçado
deixaria a vala antiga para trás — dado corrompido sem conserto.

```
TerrainField (base, editado à mão)
    +
Hydrography (rios, lagos, mar)
    ↓ resolve()
Terreno final  ← derivado, nunca editado direto
```

Mesma lógica do `rasterize`: uma direção, resultado descartável, recomputado por
revisão. Mover um rio recalcula tudo e não deixa cicatriz. A hidrografia sai no
export como geometria própria — outro projeto precisa dela para desenhar água ou
bloquear navegação.

### 4.9 Export em camadas, não em bloco

O JSON do documento inteiro serve para reabrir a cidade na própria ferramenta.
Não serve para alimentar outro projeto: um consumidor que só quer a malha viária
não deveria parsear estátuas.

```
cidade-x/
  manifest.json      escala, bounds, versão, catálogo usado
  terrain.bin        heightmap resolvido, float32
  hydrography.json   rios e corpos d'água como polilinhas/polígonos
  roads.json         grafo métrico, compatível com GeoJSON
  zones.json         polígonos de zoneamento em metros
  placements.json    id, prototypeId, posição, rotação
  tiles/             rasterização opcional, para consumidores em grid
```

Cada consumidor lê só o que entende.

### 4.10 O primeiro consumidor do `rasterize` é o export, não a simulação

Mudança em relação à rev. 1. O SoccerDreamGame consome tilemap, então rasterizar
o documento para TMX/Tiled é **feature de exportação**, não de jogo.

Efeito colateral valioso: o ADR-001 ganha consumidor real dentro do MVP, em vez
de esperar por tráfego que talvez nunca chegue. O teste de guarda vira teste de
export.

### 4.11 Pipeline de direção única

```
UI → Command → validate → apply → CityDocument' → Event → snapshot → UI
```

- `Command` é união fechada. Comando novo quebra o `switch` em compile time
  (ver §4.2 sobre `TreatWarningsAsErrors`).
- `validate` retorna motivo da rejeição ou `null`. Não corrige, não ajusta, não
  sugere alternativa.
- **Contrato:** se `validate` passou, `apply` não falha. Exceção em `apply` é
  violação de invariante, nunca fluxo de negócio.
- Se um componente de UI escrever direto no documento, a arquitetura quebrou.

### 4.12 Undo/redo é requisito, não extra

Ferramenta de autoria sem undo é inutilizável. Como todo comando já é união
fechada e `apply` já é imutável, undo/redo sai quase de graça — **mas só
enquanto ninguém furar o pipeline da §4.11.**

Estava ausente do plano da rev. 1. Erro corrigido.

### 4.13 ADR-001 — cobertura fracionária de vias

Rasterização por supersample 4×4 produzindo `roadCoverage` (0..255) mais
`roadEdge` (edge dominante por célula).

Alternativas rejeitadas:
- *Amostragem por centro* — via de 6m sai tracejada em diagonal; conectividade
  quebrada não tem conserto a jusante.
- *Qualquer sobreposição* — via engorda (local de 6m vira 16m rasterizados),
  superestimando capacidade sistematicamente.

Ganho decisivo: hierarquia viária de graça, sem tipo de tile novo.

### 4.14 Catálogo de protótipos resolve escala e direito autoral

`Prototype` guarda `footprint` em metros reais e referencia assets por
`meshRef`/`spriteRef`. O renderer decide se desenha estilizado ou fiel — o dado
exportado está correto de qualquer forma.

Permite prototipar com assets de terceiros e trocá-los depois sem tocar no
modelo.

### 4.15 Versionamento por tipo literal

`version: 1` é tipo literal, não número livre. Documento v2 quebra no
type-check, não no runtime. Migração v1→v2 é função dedicada que produz **outro
tipo**. Não existe leitor tolerante aceitando as duas versões.

### 4.16 ADR-002 — pré-render offline, não sprite stacking

Autoria em 3D; uma etapa de build renderiza cada protótipo em N ângulos de yaw,
numa projeção fixa, e produz o `spriteRef`. Consumidor 2D recebe PNG comum.

**Sprite stacking foi avaliado e rejeitado como contrato de export.** Ele não é
formato, é renderizador: obriga cada consumidor a implementar o loop de
empilhamento de fatias, contradizendo a §4.9. Some-se o custo de fatias (casa de
8×10×6m em voxel de 0.25m já dá 24 fatias; prédio de 20m dá 80, quando a prática
da técnica pressupõe modelos na ordem de 20×20×20) e a inviabilidade de animar
através das fatias.

Continua permitido como estilo de renderização de um consumidor específico, para
props e veículos. Proibido como formato de saída da ferramenta.

Ganho decisivo: **proporção correta por construção**. Todos os protótipos saem da
mesma cena, na mesma projeção, a partir de `footprint` em metros reais — então
carro de 4.5m e casa de 8×10m saem proporcionais necessariamente. O defeito de
origem fica impossível de reproduzir.

Esta decisão vive inteiramente na camada de catálogo e pipeline de asset.
**`CityDocument` não muda** — por isso é reversível.

---

## 5. Anti-padrões a evitar

Formulados como princípios próprios, verificáveis por raciocínio. Não são
afirmações sobre o código de terceiros — ver `LINHA-DE-BASE.md` §D1 para por
que a atribuição foi removida.

| Anti-padrão | Correção adotada |
|---|---|
| Tile como unidade universal | Três espaços separados (§4.3) |
| Todo tile de estrada idêntico | Cobertura fracionária (§4.13) |
| Escavar rio direto no heightmap | Terreno final derivado (§4.8) |
| Export monolítico que ninguém consegue consumir parcialmente | Export em camadas (§4.9) |
| Export que exige técnica de render no consumidor | Pré-render offline; consumidor recebe PNG (§4.16) |
| Editor sem undo | Undo como requisito (§4.12) |
| Nomes crus de callback como eventos | Envelope fechado de eventos |
| Parâmetros hardcoded | Catálogo declarativo (§4.14) |

---

## 6. Em aberto — precisa de decisão

| Item | Natureza | Quando decidir |
|---|---|---|
| Editor em 3D ou 2D isométrico | Ferramenta | Sprint 2 (recomendação: 3D) |
| Formato do `terrain.bin` (raw float32 vs PNG 16-bit) | Fundacional | Sprint 1 |
| Zonas: polígono livre ou retângulo alinhado ao grid? | Fundacional | Sprint 5 |
| Rio muda largura ao longo do traçado, ou é constante por edge? | Modelo | Sprint 3 |
| Catálogo é por documento ou compartilhado entre cidades? | Modelo | Sprint 6 |
| Projeção do pré-render e número de ângulos de yaw | Asset | Sprint 6 |
| Pré-render roda dentro do Godot ou como ferramenta externa? | Asset | Sprint 6 |
| Viadutos: `roadEdge` guarda só uma edge por célula | Adiado por decisão | ADR novo quando houver tráfego multinível |

---

## 7. Riscos registrados

**ADR-001 pode virar letra morta.** Só sobrevive se o primeiro consumidor tratar
`roadCoverage` como fração. Se alguém escrever `if (roadCoverage > 0)` e usar
como booleano, o array extra vira só peso.
*Mitigação:* teste que rasteriza via de 6m e via de 24m e falha se produzirem a
mesma capacidade.

**O documento pode não ser autossuficiente e ninguém perceber até tarde.**
*Mitigação:* round-trip no Sprint 1 — monta documento em código, serializa, lê de
volta, afirma igualdade. Falhou, existe estado morando fora do documento.

**Exaustividade de comandos não sobrevive à tradução para C#.** CS8509 dispara
mesmo com switch completo, então a flag de warnings não resolve.
*Mitigação:* teste de reflexão sobre os subtipos de `Command` (§4.2) e proibição
de braço `_ =>`.

**O jogo pode canibalizar a ferramenta.** RCI, tick, economia e orçamento são
sedutores e não servem à autoria.
*Mitigação:* estão fora do MVP por decisão, não por esquecimento (§9).

---

## 8. Vocabulário do projeto

`CityDocument` · `WorldScale` · `metersPerTile` · `TerrainField` ·
`Hydrography` · `resolve` · `NetworkGraph` · `Zone` · `Placement` ·
`Prototype` · `Footprint` · `rasterize` · `Command` · `validate` · `apply` ·
`manifest.json` · camada de export · `meshRef` · `spriteRef` · pré-render ·
sprite stacking (rejeitado) · ADR · undo stack

---

## 9. Fora de escopo no MVP

Backlog explícito, não esquecimento. Tudo abaixo é **jogo**, não ferramenta — e
o jogo existe: é o `citybuilderteste`, em repositório próprio. Nada disto entra
no editor:

Zonas RCI simuladas · tick loop · economia e orçamento · demanda · tráfego ·
desastres · multiplayer · sprites animados · agentes (pessoas, veículos) ·
navegação em globo.

Também fora: colaboração multiusuário, versionamento de cidade além de
salvar/abrir, geração procedural de terreno.
