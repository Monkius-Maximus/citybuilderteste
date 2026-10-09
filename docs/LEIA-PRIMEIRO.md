# Leia primeiro — estado do projeto e ordem de leitura

**Atualizado em:** 2026-09-19

Esta pasta contém o planejamento de um **editor de cidades e mapas em escala
métrica**, produzido em sessões de projeto. O código que está em `src/` neste
repositório é um **engine de simulação em tiles**, construído separadamente.

**As duas coisas ainda não foram reconciliadas.** Há uma decisão pendente, e ela
está descrita em `AUDITORIA-REPOSITORIO.md`. Leia esse arquivo antes de agir
sobre qualquer outro.

---

## Ordem de leitura

| # | Arquivo | Para quê |
|---|---|---|
| 1 | `AUDITORIA-REPOSITORIO.md` | Conflito entre o código existente e o plano. **Decisão pendente.** |
| 2 | `MEMORIA-PROJETO.md` | Contexto durável: propósito, decisões travadas, riscos, fora de escopo |
| 3 | `LINHA-DE-BASE.md` | De onde veio cada afirmação e com que confiança |
| 4 | `adr/adr-001-rasterizacao-de-vias.md` | Cobertura fracionária de vias |
| 5 | `adr/adr-002-pipeline-de-asset.md` | Pré-render offline; sprite stacking rejeitado |
| 6 | `spec/city-document-v1.ts` | Esquema canônico do documento (contrato, não implementação) |
| 7 | `spec/engine-pipeline.ts` | Comandos, validate/apply, eventos, bridge |
| 8 | `backlog-sprints.html` | Backlog visual das sprints — abrir no navegador |
| 9 | `handoff/SPRINT-0-HANDOFF.md` | **Suspenso.** Briefing de implementação, inválido até a decisão |

`../CLAUDE.md` na raiz descreve invariantes do **editor**, não do código atual
em `src/`.

---

## Estado em uma tabela

### Produzido

- Esquema `CityDocument` v1 completo, verificado sob `tsc --noEmit` estrito
- Contrato de pipeline: comandos, validate/apply, eventos, bridge
- ADR-001 — rasterização de vias por cobertura fracionária
- ADR-002 — pipeline de asset por pré-render offline
- Memória do projeto (revisão 5)
- Linha de base de proveniência
- Backlog visual de 9 sprints
- Auditoria do repositório

### Implementado em código

Nada do plano acima. Os arquivos `.ts` são especificação a portar, e não rodam.

O que existe em `src/` é de outra linhagem: engine de simulação em tiles, com
tick, ECS, zoneamento, redes, pathfinding, tráfego, utilidades, economia,
persistência e replay. Ver `ARCHITECTURE.md`.

### Pendente de decisão

| Item | Natureza | Onde está descrito |
|---|---|---|
| **Reconciliar editor métrico × engine em tiles** | **Bloqueante** | `AUDITORIA-REPOSITORIO.md` §6 |
| Editor em 3D ou 2D isométrico | Ferramenta | `MEMORIA-PROJETO.md` §3 |
| Formato do `terrain.bin` | Fundacional | `MEMORIA-PROJETO.md` §6 |
| Zonas: polígono livre ou retângulo alinhado | Fundacional | `MEMORIA-PROJETO.md` §6 |
| Rio: largura variável ou constante por edge | Modelo | `MEMORIA-PROJETO.md` §6 |
| Catálogo por documento ou compartilhado | Modelo | `MEMORIA-PROJETO.md` §6 |
| Projeção e número de ângulos do pré-render | Asset | `MEMORIA-PROJETO.md` §6 |
| Pré-render dentro do Godot ou externo | Asset | `MEMORIA-PROJETO.md` §6 |
| Tamanho de cidade alvo | Lacuna | `LINHA-DE-BASE.md` §E |
| Viadutos — `roadEdge` guarda uma edge por célula | Adiado | `adr/adr-001` |

---

## Convenções destes documentos

- Decisões travadas ficam na memória do projeto, numeradas e citáveis (§4.x).
- Decisões com alternativas avaliadas viram ADR numerado, e a alternativa
  rejeitada é registrada com o mesmo peso da adotada.
- Afirmação não verificada não entra como fato: vai para a linha de base, na
  camada correspondente.
- Nenhum documento afirma o que projetos de terceiros fazem. Cada decisão se
  justifica pelo raciocínio próprio.
