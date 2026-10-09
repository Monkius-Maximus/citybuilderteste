# CLAUDE.md — citybuilderteste (o jogo)

Contexto permanente deste repositório. Leia antes de qualquer tarefa.

## O que é este repositório

O **jogo**: um simulador de cidade 2D isométrico em C# puro, engine-agnostic,
com a simulação separada da apresentação e rodando headless.

A arquitetura está descrita em `docs/ARCHITECTURE.md`. Ela é a referência para
qualquer trabalho em `src/`.

## O que este repositório NÃO é

Ele **não** é o editor de cidades em escala métrica. O editor é outro projeto,
com repositório próprio, e este jogo é um dos consumidores do que ele exporta.
A separação foi decidida em 2026-10-09 — ver `docs/AUDITORIA-REPOSITORIO.md` §9.

Consequências práticas:

- `docs/editor/` contém a documentação do editor **em trânsito**. Ela será
  movida para o repositório do editor e removida daqui. **Não é instrução para
  o código deste repositório.** As regras de lá (documento métrico imutável,
  comandos como dados, "tick e economia fora de escopo") valem para o editor,
  não para o jogo.
- Não implemente aqui ferramentas de autoria métrica: terreno em metros,
  hidrografia, vias como polilinhas métricas, export em camadas. Isso é do
  editor.

## Fronteira com o editor — contrato em aberto

O editor exporta, entre outras camadas, uma rasterização opcional em grid
(`tiles/`) com escala declarada no `manifest.json` (`metersPerTile = 8` no
editor). Este jogo usa `GridCoord` inteiro como unidade de posição e hoje não
tem noção de metro.

**Ainda não foi decidido** como o jogo vai importar esse grid nem se o tile do
jogo passa a significar 8 m. Não tome essa decisão implicitamente em código —
ela precisa de registro próprio antes.
