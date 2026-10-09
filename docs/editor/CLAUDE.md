# CLAUDE.md — Editor de Cidades

Contexto permanente do **editor métrico**. Leia antes de qualquer tarefa.

> O editor tem repositório próprio (decisão de 2026-10-09). O jogo
> `citybuilderteste`, um simulador em tiles, é um dos consumidores do export
> deste editor. Nada do jogo entra aqui, e nada daqui entra no jogo.

## O que é

Um **editor de cidades e mapas** em escala métrica. O produto é a cidade
autorada e exportada, não uma partida jogada. Um jogo pode ser construído
depois, como segundo consumidor do mesmo documento.

Stack: **Godot 4.x · C# · .NET 8**.

A equivalência 1 unidade Godot = 1 metro é convenção adotada por este projeto,
não regra do engine. Respeite-a em todo código que toca espaço de mundo.

## Arquitetura em uma frase

`CityDocument` é a única verdade. Tudo o mais — grid de tiles, terreno
resolvido, camadas de export — é projeção derivada, unidirecional e
descartável.

## Invariantes — nunca violar

1. **Toda geometria do documento está em metros.** Tiles existem apenas dentro
   do subsistema de rasterização. Nenhum campo de `CityDocument` usa tile.
2. **Não existe caminho de volta.** Nada reconstrói o documento a partir de uma
   projeção. Não escreva `Derasterize`, `Unresolve` ou equivalente.
3. **Mutação só por comando.** UI → `Command` → `Validate` → `Apply` →
   documento novo → evento. Se um nó da UI escreve no documento, a arquitetura
   quebrou.
4. **`Validate` não conserta.** Retorna o motivo da rejeição ou `null`. Não
   ajusta valores, não sugere alternativa, não escolhe o "mais próximo válido".
5. **Contrato Validate/Apply.** Se `Validate` passou, `Apply` não falha. Exceção
   em `Apply` é violação de invariante, nunca fluxo de negócio.
6. **`CityEditor.Core` não referencia Godot.** É biblioteca .NET pura e
   testável headless. Só o projeto Godot referencia Godot.
7. **Documento imutável.** `Apply` devolve um documento novo e incrementa
   `Revision`. Nunca muta no lugar — undo/redo depende disso.
8. **Rio nunca é escavado no heightmap.** `TerrainField` é base editada à mão;
   hidrografia é camada separada; o terreno final é derivado por `Resolve`.
9. **`RoadCoverage` é fração, nunca booleano.** Ver ADR-001. `if (coverage > 0)`
   usado como "é estrada" anula a decisão inteira.

## Princípios de engenharia

- Falhe rápido. Precondição não atendida lança, não retorna valor mágico.
- Sem fallback. Um caminho correto, não alternativas silenciosas.
- Sem compatibilidade retroativa preventiva. Clareza vence.
- Mudanças cirúrgicas. Não refatore o que não foi pedido.
- Uma responsabilidade por função.
- Não invente parâmetros de API. Se não tem certeza da assinatura, verifique.

## Decisões travadas — não reabrir sem ADR

| Decisão | Valor |
|---|---|
| Escala de simulação | `metersPerTile = 8` — alterar invalida o documento |
| Amostragem de terreno | Nos vértices: `(cols+1) × (rows+1)` |
| Resolução de terreno | `sampleSpacing` independente de `metersPerTile` |
| Vias | Grafo de polilinhas métricas, não células pintadas |
| Cruzamento | Node compartilhado explícito; sem node = viaduto |
| Rasterização de via | Cobertura fracionária, supersample 4×4 (ADR-001) |
| Pipeline de asset | Pré-render offline para atlas de ângulos (ADR-002) |
| Versionamento | `Version` literal; migração produz outro tipo |

## Fora de escopo — não implemente, mesmo que pareça natural

Zonas RCI simuladas, tick loop, economia, orçamento, demanda, tráfego,
desastres, agentes animados, multiplayer. Tudo isso é **jogo**, e o jogo não é
este projeto. Se uma tarefa parecer exigir simulação, pare e pergunte.

## Layout do repositório

```
src/CityEditor.Core/         biblioteca .NET pura — modelo, comandos, export
src/CityEditor.Core.Tests/   xUnit, headless
src/CityEditor.Godot/        projeto Godot, referencia Core
docs/adr/                    decisões arquiteturais
docs/spec/                   spec original em TypeScript (referência, não roda)
```

## Referências de arquitetura

O projeto não deriva de nenhum codebase existente. Por decisão de processo,
código-fonte de simuladores de cidade de terceiros **não é lido nem
consultado** — nem para inspiração, nem para conferir comportamento. Não
busque, não cite, não copie estruturas, algoritmos ou nomes de função.

Pelo mesmo motivo, não afirme no código nem em comentários o que outro projeto
faz ou deixa de fazer. Justifique cada decisão pelo raciocínio próprio.
