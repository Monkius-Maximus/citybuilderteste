# ADR-001 — Rasterização de vias no grid de simulação

**Status:** proposto
**Contexto de escala:** `metersPerTile = 8`

## Problema

O `NetworkGraph` guarda vias como polilinhas com largura em metros. A simulação
roda sobre um grid de células de 8m. Uma edge quase nunca coincide com a grade:
ela atravessa células parcialmente, em diagonal, com largura que vai de 6m
(via local) a 24m (avenida).

Como decidir o que uma célula "é" quando uma via passa por ela?

## Opções consideradas

### A — Amostragem por centro
Célula é via se o centro dela cai dentro da faixa de rolamento.

Determinístico e trivial de implementar. Mas uma via de 6m é mais estreita que
a célula de 8m: em trechos diagonais o centro cai fora com frequência e a via
sai **tracejada** no grid. Conectividade quebra, e roteamento de tráfego sobre
grid desconexo não tem conserto a jusante.

### B — Qualquer sobreposição
Célula é via se a faixa intersecta qualquer parte dela.

Conectividade garantida. Em compensação a via **engorda**: uma local de 6m em
diagonal ocupa duas células, ou seja 16m de rasterizado. A simulação passa a
enxergar capacidade viária que não existe, e o erro é sistemático a favor do
jogador — o pior tipo de erro numa economia.

### C — Cobertura fracionária
Célula guarda a fração de sua área ocupada por via, `0..255`.

Conectividade preservada (qualquer sobreposição produz fração > 0) e capacidade
proporcional à área real de pista. Custo: um `Uint8Array` a mais e supersampling
na rasterização.

## Decisão

**Opção C.** Cobertura fracionária por supersample 4×4, mais o id da edge
dominante da célula.

```
roadCoverage: Uint8Array   // 0..255, fração de área
roadEdge:     Uint32Array  // índice+1 da edge dominante; 0 = nenhuma
```

Célula participa da rede se `roadCoverage > 0`. A capacidade de tráfego é
ponderada por `roadCoverage / 255`, não binária.

## Justificativa

O ganho decisivo não é precisão geométrica, é **capacidade diferenciada**. Com
tiles binários, avenida e ruela têm a mesma capacidade, e a única hierarquia
viária possível passa a ser o tipo de tile — o que obriga um tipo novo para
cada largura de via. A fração de cobertura dá hierarquia de graça, sem tipo de
tile novo e sem regra especial.

O custo é irrelevante no lugar certo: a rasterização roda **uma vez por revisão
do documento**, não por tick. 16 testes ponto-em-polígono por célula candidata,
limitado à bounding box da edge, é ruído perto de qualquer tick de simulação.

## Consequências

- `SimSnapshot` ganha dois arrays paralelos a `tiles`. Aceito.
- Tráfego consome `roadCoverage`, nunca um booleano de "é estrada". Nenhum
  consumidor pode tratar via como binária — isso precisa ser respeitado desde o
  primeiro consumidor, senão a decisão vira letra morta.
- `roadEdge` guarda **uma** edge por célula, a de maior cobertura. Empate é
  resolvido pelo menor índice de edge. Isso é lossy para viadutos e cruzamentos;
  aceito porque o roteamento canônico acontece no grafo, não no grid — o grid é
  para custo de travessia e para consulta espacial rápida.
- Não existe caminho inverso: nenhum código reconstrói `NetworkGraph` a partir
  de `roadCoverage`.

## Fica em aberto

Viadutos. Duas edges cruzando sem node compartilhado ocupam a mesma célula em
cotas diferentes, e `roadEdge` só guarda uma. Enquanto não houver simulação de
tráfego em múltiplos níveis, a perda é inofensiva. Quando houver, isto vira
ADR-00N — e não uma emenda neste documento.
