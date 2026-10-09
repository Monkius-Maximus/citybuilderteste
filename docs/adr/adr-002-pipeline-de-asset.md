# ADR-002 — Pipeline de asset e estratégia 2D a partir de mundo 3D

**Status:** proposto
**Depende de:** §4.14 (catálogo de protótipos), §4.9 (export em camadas)
**Não depende de:** decisão de renderização do editor

## Problema

O documento é métrico e agnóstico, mas os consumidores não são. O consumidor mais
concreto hoje (SoccerDreamGame) é 2D em tiles. Consumidores futuros podem ser 3D.

Como a ferramenta entrega geometria e assets utilizáveis pelos dois, sem duplicar
o trabalho de autoria e sem reintroduzir o defeito de escala que originou o
projeto?

## Opções consideradas

### A — Sprite stacking

O protótipo é um modelo voxel fatiado horizontalmente. O export entrega o atlas
de fatias; o consumidor desenha fatia sobre fatia com deslocamento crescente e
rotaciona o conjunto para produzir a ilusão de volume.

Rejeitada. Três motivos, em ordem de peso:

1. **Não é formato, é renderizador.** O consumidor precisa implementar o loop de
   empilhamento. Isso contradiz a §4.9, onde cada consumidor lê só o que entende
   sem herdar comportamento da ferramenta.
2. **Custo de fatias briga com fidelidade métrica.** Uma casa de 8×10×6m em voxel
   de 0.25m já são 24 fatias; um prédio de 20m são 80. A prática corrente da
   técnica recomenda modelos na ordem de 20×20×20 — ou seja, ela pressupõe
   objetos pequenos e de baixa resolução, exatamente o oposto do requisito.
3. **Detalhe e animação limitados.** Modelar detalhe em cortes transversais é
   difícil, e animar através das fatias é inviável na prática — só se anima o
   conjunto por transformação de topo. Agentes animados ficariam de fora.

### B — Render 3D fiel em todo consumidor

Rejeitada. Impõe pipeline 3D a consumidores que são 2D por decisão de arte, e o
SoccerDreamGame é um deles.

### C — Pré-render offline para atlas de ângulos fixos

O catálogo guarda `meshRef`. Uma etapa de build renderiza cada protótipo em N
ângulos de yaw, numa projeção fixa, e produz o `spriteRef`. O consumidor 2D
recebe PNG comum e não implementa técnica nenhuma.

É o caminho clássico de jogos isométricos pré-renderizados dos anos 1990–2000.
Ilustração, não prova: a justificativa abaixo não depende do precedente.

## Decisão

**Opção C.** Autoria em 3D, pré-render offline, consumo em 2D ou 3D conforme o
projeto.

## Justificativa

O ganho decisivo não é economia de trabalho, é **proporção correta por
construção**.

Todos os protótipos são renderizados na mesma cena, na mesma projeção, a partir
de `footprint` em metros reais. Logo o carro de 4.5m sai proporcional à casa de
8×10m necessariamente — não por disciplina de quem desenha, mas porque vieram do
mesmo render. O defeito que originou o projeto — carro do tamanho de casa —
fica impossível de reproduzir.

Segundo ganho: o consumidor não herda técnica. Recebe PNG e geometria métrica.

## Consequências

- O catálogo ganha uma etapa de build: `meshRef` → render → `spriteRef`.
  Determinística e reexecutável; o resultado é artefato descartável, nunca
  editado à mão.
- A projeção do pré-render e o número de ângulos de yaw viram parâmetros do
  catálogo, não constantes espalhadas. Mudá-los é rerodar o build.
- `CityDocument` **não muda**. Esta decisão vive inteiramente na camada de
  catálogo e pipeline de asset, e é por isso que é reversível.
- Sprite stacking continua disponível como estilo de renderização de um
  consumidor específico — para props e veículos, onde a técnica é boa. O que
  fica proibido é usá-la como contrato de export da ferramenta.

## Efeito sobre o bloqueio "2D ou 3D"

O bloqueio registrado eram duas perguntas empilhadas. Esta ADR dissolve a de
export: a saída atende 2D e 3D simultaneamente, sem escolha excludente.

Resta a pergunta menor — **o editor desenha em 3D ou em 2D isométrico?** Ela
deixa de ser fundacional e vira decisão de ferramenta, porque o documento e o
export são agnósticos nos dois casos.

Recomendação registrada, ainda não travada: editor em 3D. Terraformar e traçar
rio sem ver relevo em três dimensões é trabalhar às cegas, e Godot entrega o
básico pronto.

## Fica em aberto

- Projeção do pré-render (isométrica 2:1, dimétrica, ou outra) e número de
  ângulos de yaw. Decidir no Sprint 6, junto com o catálogo.
- Se o pré-render roda dentro do Godot ou como ferramenta externa.
