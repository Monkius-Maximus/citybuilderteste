# Linha de Base — proveniência do que foi tratado como verdade

> Este documento não decide nada. Ele diz **de onde veio** cada afirmação que
> sustenta as decisões do projeto, e com que grau de confiança.
>
> Existe porque as decisões travadas se apoiam em camadas de confiança muito
> diferentes, e tratá-las como igualmente sólidas é o caminho para descobrir
> tarde que uma fundação era palpite.
>
> **Data:** 2026-09-06 · Corresponde à revisão 4 da memória do projeto.

---

## Como ler

| Camada | Significado | O que fazer com ela |
|---|---|---|
| **A** | Você afirmou | Tratado como dado. Se mudar, avise. |
| **B** | Derivado por raciocínio | Sem autoridade externa. Contestável a qualquer momento. |
| **C** | Verificado nesta conversa | Fonte e data registradas. |
| **D** | Afirmado sem verificar | **Risco.** Leia esta seção com atenção. |
| **E** | Nunca estabelecido | Lacuna conhecida. |

---

## Camada A — o que veio de você

Base factual do projeto. Nada aqui foi verificado por mim porque não precisa
ser: é o seu contexto.

1. Não existe engine de simulação pronto. O projeto começa do zero.
2. A stack será Godot, recebendo o projeto.
3. O propósito primário é **autoria de mapas/cidades**; o jogo é um consumidor
   possível e posterior.
4. As ações desejadas na ferramenta: terraformar, editar rios, construir casas
   e bairros, definir zonas, criar objetos de mundo (estátuas, decorações),
   exportar.
5. O export existe para **alimentar outros projetos seus**, não para ser lido
   de volta pela própria ferramenta.
6. Ferramentas atuais usadas nos testes: TheoTown e Tiled, para criar as
   cidades-tema do SoccerDreamGame.
7. **Em TheoTown, uma casa ocupa um único tile, e pessoas e veículos aparecem
   em escala errada — um carro chega ao tamanho de uma casa.** Esta é a
   observação que origina o projeto inteiro. É sua, de uso direto do jogo.
8. Jogos como GTA San Andreas autoram o mapa em software externo e o jogo
   consome — é o modelo mental correto para o projeto.
9. A escala distinta desses jogos impede reuso direto do que você produz.
10. Você quer maior controle sobre todo esse processo.
11. Sobre a licença: a intenção é apenas se inspirar, não derivar código.

**Nada em A foi questionado por mim, e nada em A deveria mudar sem você dizer.**

---

## Camada B — derivado por raciocínio, não por autoridade

Isto é o grosso da arquitetura. Não veio de documentação, benchmark ou fonte
externa: veio de raciocinar sobre o problema que você descreveu. **É a camada
mais fácil de contestar, e a que mais se beneficia de contestação.**

| Decisão | Raciocínio que a sustenta |
|---|---|
| Três espaços separados (mundo/grid/tela) | Se o tile é a unidade de tudo, não existe noção de metro, e o defeito de escala não tem conserto a jusante |
| `metersPerTile = 8` | Casa unifamiliar (~8×10m) cabe em ~1 célula; quarteirão de 80m cai em 10 células |
| Documento é a verdade; projeções são unidirecionais | Rasterização é lossy; reconstruir o documento a partir dela seria adivinhação |
| Vias como grafo de polilinhas métricas | Polilinha vira GeoJSON/spline/navmesh direto; tile não vira nada sem heurística |
| Terreno amostrado nos vértices | `(cols+1)×(rows+1)` define os quatro cantos de cada quadrilátero sem interpolação, e borda é o que a validação de declive mede |
| `sampleSpacing` independente de `metersPerTile` | Não há motivo para um subsistema ditar a resolução do outro |
| Pipeline de direção única | Se a UI escreve no documento, undo e validação deixam de ter garantia |
| Contrato validate/apply | Separar "pode?" de "faça" torna a falha em `apply` um bug detectável, não um fluxo |
| Envelope fechado de eventos | Envelope instável obriga cada consumidor a inventar o próprio parsing |
| Versão como tipo literal | Quebrar no type-check é mais barato que quebrar no runtime |
| ADR-001 — cobertura fracionária 4×4 | Amostragem por centro trafica via estreita em diagonal; qualquer-sobreposição engorda a via e superestima capacidade |
| Hidrografia como camada, terreno derivado | Escavar no heightmap deixa vala antiga ao reeditar o traçado |
| Export em camadas | Quem quer só a malha viária não deveria parsear estátuas |
| Undo/redo como requisito | Ferramenta de autoria sem undo é inutilizável |
| Pré-render offline (ADR-002) | Proporção correta por construção: tudo sai da mesma cena, em metros reais |
| As três ferramentas ocupam camadas distintas | City-searcher = dados, Ferramenta de Mundo = entidades, esta = geometria |
| Sprints reordenadas para autoria-primeiro | RCI, tick e economia não servem ao propósito primário |

O supersample **4×4** especificamente é escolha de engenharia sem
calibração — não testei 2×2 nem 8×8 contra nada. É um ponto de partida
razoável, não um ótimo medido.

---

## Camada C — verificado nesta conversa, com fonte

### C1. Cities: Skylines usa células de 8×8 metros
Confirmado em documentação de modding e discussões da comunidade. Uma célula
cobre 64 m²; a via básica ocupa 2 células, 16m de largura, com 10m de asfalto.
Fonte: cslmodding.info/scale/ · consultado em 2026-09-06.

**Uso:** sustenta parcialmente a escolha de `metersPerTile = 8` — mas por
convergência, não por autoridade. A justificativa primária continua sendo B.

### C2. Sprite stacking: limitações reais
Verificado que a técnica consiste em desenhar fatias empilhadas com
deslocamento crescente, que modelos altos são custosos (uma camada por fatia),
que a prática recomenda modelos na ordem de 20×20×20, que detalhe em corte
transversal é difícil de modelar, e que animar através das fatias é inviável
— anima-se o conjunto por transformação de topo.
Consultado em 2026-09-06.

**Uso:** base da rejeição no ADR-002.

### C3. C# não reproduz exaustividade de união discriminada
Verificado que um `switch` sobre hierarquia de `record` emite CS8509 **mesmo
com todos os casos cobertos**, porque o compilador não prova a hierarquia
fechada — o construtor de cópia protegido dos `record` admite herança externa
em tese. Analisadores de terceiro existem justamente para suprimir o aviso em
hierarquias comprovadamente fechadas.
Fonte: shuebner/ClosedTypeHierarchyDiagnosticSuppressor · consultado em
2026-09-06.

**Uso:** motivou a correção da rev. 4 — a guarda passou a ser teste de
reflexão. **Esta verificação derrubou uma afirmação minha anterior.**

### C4. C# 15 / .NET 11 traz `union` com exaustividade real
Verificado que a palavra-chave `union` chega no C# 15 com .NET 11, com
exaustividade garantida pelo compilador sem braço descarte.
Consultado em 2026-09-06.

**Uso:** registra que a guarda por reflexão é contorno temporário. Não está
disponível hoje — Godot 4.x roda em .NET 8.

### C5. Modelo e esforço no Claude Code
Verificado o comportamento de `/model`, `--effort`, a keyword `ultrathink`, e a
heurística oficial: contexto completo + tentativa clara + erro → modelo mais
capaz; erro por pular arquivo ou não rodar teste → esforço maior.
Consultado em 2026-09-06.

---

## Camada D — afirmado sem verificação

**Leia esta seção antes de defender qualquer decisão que dependa dela.**

### D1. Afirmações sobre o MicropolisCore — nenhuma verificada

Ao longo da conversa eu descrevi o MicropolisCore como tendo:

- um `PieMenu.svelte` que é stub, com cursor e toolbar nunca implementados;
- nomes crus de callback do Embind expostos como eventos;
- fluxo de orçamento assíncrono com semântica duvidosa;
- todo tile de estrada idêntico, sem hierarquia viária;
- parâmetros de simulação hardcoded.

**Eu não li o código, não consultei a documentação e não verifiquei nada
disso nesta conversa.** São afirmações de memória de treino, e podem estar
erradas em detalhe, em nome de arquivo, ou por completo.

Há uma tensão concreta aqui: a decisão §4.1 estabelece que o código-fonte não
será lido. Então essas afirmações **não são verificáveis dentro das regras que
o projeto adotou** — e não deveriam continuar sendo citadas como fato sobre
outro codebase.

**Ação recomendada:** reescrever a tabela de anti-padrões da memória como
princípios em primeira pessoa, sem atribuição. "Todo tile de estrada idêntico
impede hierarquia viária" é uma afirmação verdadeira e verificável por
raciocínio. "O Micropolis faz X" é uma afirmação sobre terceiro que ninguém vai
conferir. O valor da tabela está na correção adotada, não na acusação.

### D2. SimCity 4 usa tiles de 16 metros

Afirmado por mim várias vezes e usado para justificar `metersPerTile = 8` por
contraste. **Não verificado.** Busquei e não encontrei confirmação.

Se estiver errado, o argumento de `metersPerTile = 8` não cai — ele se sustenta
sozinho pelo raciocínio da casa unifamiliar (B) e pela convergência com Cities:
Skylines (C1). Mas a frase deve sair dos documentos até ser confirmada.

### D3. SimCity 4, Diablo II, RollerCoaster Tycoon e Age of Empires usaram
pré-render 3D→2D

Afirmado no ADR-002 como precedente. É conhecimento amplamente repetido e
provavelmente correto, mas **não verificado nesta conversa**. Serve como
ilustração, não como prova. A justificativa do ADR-002 não depende dele.

### D4. Godot trata 1 unidade = 1 metro

Afirmado como se fosse garantia do engine. Na prática é **convenção**, não
regra imposta: nada no Godot obriga essa equivalência, ela é adotada por
consistência com física e assets. A consequência prática para o projeto é a
mesma, mas a frase está forte demais.

### D5. Estimativas de custo de rasterização

"16 testes ponto-em-polígono por célula candidata é ruído perto de qualquer
tick" — plausível, **não medido**. Não há benchmark, não há tamanho de cidade
alvo definido, e sem isso a afirmação é intuição.

---

## Camada E — nunca estabelecido

Lacunas conhecidas. Nenhuma bloqueia o Sprint 0; todas bloqueiam algo depois.

1. **Repositório.** Nenhum foi nomeado. Os artefatos existem só como arquivos
   soltos. Precisa ser criado antes de abrir o Claude Code.
2. **Tamanho de cidade alvo.** Nunca discutido. Sem isso não há como
   dimensionar `TerrainField`, custo de rasterização ou formato de
   `terrain.bin`.
3. **Quem são os consumidores além do SoccerDreamGame.** O plano de export em
   camadas foi desenhado para "outros projetos" genéricos.
4. **Editor em 2D ou 3D.** Em aberto por decisão sua, marcado para o Sprint 2.
5. **Direção de arte.** Nunca discutida. Afeta o pré-render (projeção, número
   de ângulos) no Sprint 6.
6. **Se a ferramenta terá usuários além de você.** Muda requisitos de UX,
   robustez e documentação.

---

## O que eu mudaria nos documentos à luz disto

Três correções concretas, em ordem de importância:

1. **Tabela de anti-padrões (memória §5):** reescrever sem atribuição ao
   MicropolisCore. O conteúdo se sustenta; a atribuição não.
2. **Remover "SimCity 4 usa 16m"** da memória (§4.4), do `city-document-v1.ts`
   e do backlog, até que seja confirmado.
3. **Suavizar "Godot trabalha em 1 unidade = 1 metro"** para "por convenção
   adotada" na memória (§4.2) e no `CLAUDE.md`.

Nenhuma das três muda uma decisão. Todas as três impedem que uma decisão boa
seja defendida depois com um argumento frágil.
