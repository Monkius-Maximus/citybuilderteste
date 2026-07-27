# Lista de Assets — THE GAME OF POLIS (Etapa 1)

> **Objetivo:** substituir os *placeholders* procedurais (`PlaceholderSpriteFactory`) por arte real,
> cobrindo o **fluxo pré-jogo** (já implementado em `Shell/`) e a **primeira vista jogável da cidade**.
>
> **Todos os números aqui derivam do código existente** — tile 64×32 (`IsometricProjector`),
> `elevationStep` 16 px, paleta de `PlaceholderSpriteFactory`, tokens de `AegeanMarbleTheme`,
> tamanhos de mapa 64/128/256 (`MapSizePreset`) e as 4 estações de `GameCalendar`.
>
> ⚖️ **Restrição legal do projeto (vale para TODO asset):** nomes, formas e marcações devem ser
> **genéricos**. Nada de logotipos, marcas, modelos de veículos reais ou fachadas reconhecíveis de
> empresas existentes. Identificadores abstratos: `CompactHatch_Tier1`, `Industrial_Heavy_Level2`.

---

## 1. Premissas técnicas

### 1.1 Resolução alvo (base: Steam Hardware Survey, meados de 2026)

| Resolução | Share aprox. | Fator de escala | Tile efetivo |
|---|---|---|---|
| **1920×1080** (1080p) | **~51%** | 1× | 64×32 |
| **2560×1440** (1440p) | **~21%** | 1,333× | 85,3×42,7 |
| **3840×2160** (4K) | **~5%** | 2× | 128×64 |

**Decisão:** como 1440p cai em fator **não inteiro**, e a direção "Aegean Marble" é **pintada/renderizada
(não pixel-art)**, autoramos **tudo em 2× (mestre 4K)** e o engine faz *downscale* bilinear + mipmaps.
Isso serve as três resoluções com um único conjunto de arquivos e sem *shimmering*.

- **Mestre de produção (o que a equipe de arte entrega): @2x**
- **@1x é gerado no build** (script de export), não desenhado à mão.
- Se um dia adotarmos pixel-art puro, a regra muda para escala inteira apenas — **não é o caso agora**.

### 1.2 Grade isométrica (do `IsometricProjector`)

- Projeção **2:1 dimétrica** (diamante). Tile @1x = **64×32**; @2x = **128×64**.
- **Elevação:** 1 unidade = **16 px** @1x / **32 px** @2x (deslocamento vertical).
- **Ponto de ancoragem de TODO sprite de mundo:** centro da base do losango (*bottom-center*).
  O renderizador posiciona por esse ponto; sprites fora do padrão desalinham.
- **Ordenação:** *painter's algorithm* via `DepthKey` — a arte não precisa fazer nada, mas **não pode**
  ter elementos que "vazem" para trás do tile de origem além do especificado em cada canvas.

### 1.3 Fórmula de canvas (footprint W×H tiles)

```
largura_canvas  = (W + H) × 64 px        (@2x)
altura_da_base  = (W + H) × 32 px        (@2x)
altura_canvas   = altura_da_base + (andares × 32 px) + folga
```

| Footprint | Canvas @2x | Canvas @1x | Uso |
|---|---|---|---|
| 1×1 | **128 × 192** | 64 × 96 | casa, loja pequena, prop |
| 2×2 | **256 × 320** | 128 × 160 | prédio médio, serviço |
| 3×3 | **384 × 512** | 192 × 256 | torre, indústria média |
| 4×4 | **512 × 768** | 256 × 384 | marco cívico, usina |

### 1.4 Convenções gerais

| Item | Especificação |
|---|---|
| **Formato de sprite** | **PNG-24 + alfa (RGBA8)**, alfa **direto** (não pré-multiplicado), sRGB |
| **Atlas** | Empacotados no build em **2048×2048** (ou 4096² se necessário), *padding* 4 px |
| **Nomenclatura** | `snake_case` com prefixo de categoria + `@2x` — ex.: `bld_res_low_l2_1x1@2x.png` |
| **Cor** | Seguir a paleta de `PlaceholderSpriteFactory` (§3.1) como **referência de matiz**, não literalmente |
| **Entrega** | PNGs soltos + arquivo-fonte editável (`.psd`/`.kra`/`.blend`) em pasta paralela |
| **Licença** | Só assets originais ou licença comercial explícita (documentar no `CREDITS.md`) |

---

## 2. Fontes tipográficas

Já definidas no handoff e em `AegeanMarbleTheme` — **licença OFL, redistribuíveis**.

| Fonte | Pesos | Formato | Uso |
|---|---|---|---|
| **Marcellus** | 400 | `.ttf` (+ `.woff2` se web) | Título, cabeçalhos de painel, nomes de cidade |
| **Lora** | 400, 500, 600 + itálicos | `.ttf` | Corpo, menus, rótulos, rodapé |
| *EB Garamond* (fallback) | 400 | `.ttf` | Substituto se as acima não puderem ser embarcadas |

**Escala de referência (px @1x):** título 104 · cabeçalho de painel 30 · seção 17 · nome de save 19 ·
corpo 14–16 · rótulos 11 (maiúsculas) · itens de menu 14 (maiúsculas) · rodapé 10.

> ⚠️ Verificar suporte a **acentuação PT-BR** (ã, õ, ç, é...) nos glifos antes de fechar.

---

## 3. Mundo — sprites isométricos

### 3.1 Terreno (`TerrainKind` — 5 tipos)

Matiz de referência do placeholder atual:

| Tipo | RGB de referência | Variações | Canvas @2x |
|---|---|---|---|
| **Grass** | `96,168,88` | 4 | 128×64 |
| **Water** | `48,96,200` | 4 (animadas) | 128×64 |
| **Sand** | `214,198,140` | 3 | 128×64 |
| **Rock** | `120,120,128` | 3 | 128×64 |
| **Forest** | `30,110,60` | 4 (com copa) | 128×**128** |

**Além dos tiles-base:**
- **Transições de borda** (grass↔sand, sand↔water, grass↔rock): **12 máscaras** de canto/lado,
  128×64 @2x. *Alternativa mais barata:* borda por *shader*/blend — decidir com a engenharia.
- **Encostas/elevação:** o terreno usa `Elevation` (short). Mínimo: **4 sprites de rampa**
  (NE/SE/SW/NW), 128×96 @2x.
- **Água animada:** **8 quadros** por variação, 12 fps, em *sprite sheet* horizontal.

### 3.2 Zonas e edifícios (`ZoneType` × `ZoneDensity` × nível)

O crescimento é dirigido por `DevelopmentLevel` (0–255) com tetos por densidade
(Low 64 / Medium 160 / High 255). Traduzimos isso em **3 níveis visuais por densidade**.

| Zona | RGB de referência | Densidades (Etapa 1) | Níveis | Footprint | Sprites |
|---|---|---|---|---|---|
| **Residential** | `64,180,96` | Low, Medium | 3 cada | 1×1 / 2×2 | 6 |
| **Commercial** | `72,128,220` | Low, Medium | 3 cada | 1×1 / 2×2 | 6 |
| **Industrial** | `220,196,72` | Low, Medium | 3 cada | 2×2 | 6 |
| **Civic** | `180,90,200` | — | — | 2×2 / 3×3 | 4 (ver §3.5) |

- **+ Estado "terreno zoneado vazio"**: 4 tiles de *overlay* semitransparente (1 por zona), 128×64 @2x —
  hoje o código desenha um losango com alfa 120.
- **+ Canteiro de obras**: 2 sprites (1×1 e 2×2) para a transição de crescimento.
- **Alta densidade (High)** fica para a **Etapa 2**.

### 3.3 Redes (`NetworkType`)

Grid 4-conectado ⇒ **16 variantes de conexão** (bitmask N/E/S/W) por rede.

| Rede | RGB de referência | Variantes | Canvas @2x | Etapa |
|---|---|---|---|---|
| **Road** | `60,60,66` | **16** | 128×96 | **1** |
| **Rail** | `90,90,96` | 16 | 128×96 | 2 |
| **PowerLine** | `230,210,70` | 16 + 4 postes | 128×160 | 2 |
| **WaterPipe** | `40,130,210` | 16 (subterrâneo/overlay) | 128×64 | 2 |
| **Sewage** | `110,80,50` | 16 (subterrâneo/overlay) | 128×64 | 2 |

**Etapa 1 = só estradas** (16 variantes) + **4 sprites de calçada/meio-fio** opcionais.

### 3.4 Veículos (`VehicleClass`)

Movimento é sobre grafo 4-conectado ⇒ em tela são **4 direções** (as diagonais isométricas).
Especificar **8** já prepara vias diagonais futuras.

| Classe | RGB ref. | Modelos (Etapa 1) | Direções | Canvas @2x |
|---|---|---|---|---|
| **Passenger** | `230,230,230` | 2 (`CompactHatch_Tier1`, `Sedan_Tier1`) | 4 (ideal 8) | 64×64 |
| **Freight** | `200,120,40` | 1 (`BoxHauler_Tier1`) | 4 | 96×72 |
| **Transit** | `220,60,60` | — (Etapa 2) | — | 128×80 |
| **Service** | `240,220,60` | — (Etapa 2) | — | 96×72 |

- Entregar como *sprite sheet* (1 linha = 1 modelo, 1 coluna = 1 direção).
- **Sem** marcas, emblemas ou silhuetas de modelos reais.

### 3.5 Serviços públicos (mínimo para a Etapa 1)

Já há `ZoneType.Civic` e as redes de utilidade no core:

| Elemento | Footprint | Canvas @2x | Nota |
|---|---|---|---|
| Usina de energia | 3×3 | 384×512 | fonte de `UtilityGrid` (PowerLine) |
| Estação de água | 2×2 | 256×320 | fonte de `UtilityGrid` (WaterPipe) |
| Parque pequeno | 1×1 | 128×192 | alimenta desejabilidade |
| Praça cívica | 2×2 | 256×320 | marco visual |

---

## 4. Interface (UI) — telas do handoff

Layout, cores e tipografia **já estão especificados** em `docs/design/main-menu/README.md` e
codificados em `AegeanMarbleTheme`. O que falta são **assets gráficos**:

### 4.1 Identidade

| Asset | Formato | Tamanho | Nota |
|---|---|---|---|
| **Wordmark "POLIS"** | SVG + PNG @2x | ~1200×320 | Marcellus 104px com o par de filetes+losangos dourados |
| **Losango ornamental** | SVG | 7×7 base | Quadrado 45° com `scaleY .6`, cor `#A08339` |
| **Ícone do app** | `.ico` (16/32/48/256) + `.icns` + PNG 512 | — | Windows / macOS / Linux |
| **Textura de pergaminho** | PNG *tileable* | 512×512 @2x | Base `#F7F3E9`, grão sutil, *seamless* |

### 4.2 Controles

Cores exatas em `AegeanMarbleTheme` (borda `#C9BD9E`, primário `#24506E`, ouro `#A08339`).

| Controle | Estados | Tamanho @2x | Formato |
|---|---|---|---|
| Botão primário / secundário | normal, hover, press, disabled | 9-slice, 8 px de borda | PNG 9-slice ou SVG |
| Campo de texto | normal, focus | 9-slice | idem |
| Checkbox | off, on, hover | 36×36 | PNG |
| Slider (trilho + *thumb*) | normal, hover, drag | trilho 400×8 · thumb 32×32 | PNG |
| Dropdown (seta) | normal, aberto | 24×24 | SVG |
| Cartão de tamanho de mapa | normal, selecionado | 9-slice | PNG |
| Painel modal | — | 9-slice, raio 3 px | PNG |

### 4.3 Ícones (linha, 48×48 @2x / 24×24 @1x, SVG preferencial)

**Ferramentas:** zonear R, zonear C, zonear I, estrada, demolir, energia, água, parque, consultar.
**Overlays (`HeatMapKind`):** desejabilidade, valor da terra, poluição, criminalidade, ruído de
tráfego, cobertura de serviço. **HUD:** população, tesouro (`§`), demanda R/C/I, velocidade
(pausa/1×/2×/3×), estação (4 ícones — `Season`), alerta de *brownout*.
**Total: ~24 ícones.**

### 4.4 Cursores

| Cursor | Tamanho @2x | Hotspot |
|---|---|---|
| Padrão | 64×64 | (2,2) |
| Construir / pincel | 64×64 | centro |
| Demolir | 64×64 | centro |
| Proibido | 64×64 | centro |

### 4.5 Não precisam de asset (gerados por código)

- **Miniatura de save** na tela Load City: `ThumbnailRenderer` gera **RGBA 64×44** e grava no save v3.
- **Glifo de 3 losangos** (fallback de miniatura): desenhado por código.
- **Barras de demanda RCI**: primitivas.

---

## 5. Áudio

O `GameSettings` expõe **3 barramentos**: `MasterVolume`, `MusicVolume`, `AmbienceVolume`
("Ambience & traffic").
> ⚠️ **Lacuna de design a confirmar:** não há barramento separado de **SFX/UI**. Hoje eles cairiam no
> Master. Recomendo **adicionar um 4º slider "Efeitos"** — é uma linha em `GameSettings` e uma linha
> na tela de Settings.

| Categoria | Itens (Etapa 1) | Formato | Especificação |
|---|---|---|---|
| **Música — menu** | 1 faixa, 2–3 min, *loop* | **OGG Vorbis** q6 (~160 kbps), estéreo, 44,1 kHz | Clássico/orquestral leve, coerente com "Aegean Marble" |
| **Música — jogo** | **4 faixas** (uma por estação: `Season`) | OGG q6 estéreo | Transição suave na virada de estação |
| **Ambiência** | 3 *loops* (campo, cidade pequena, cidade densa) | OGG q5 estéreo, **loop perfeito** ≥60 s | Cruzam por densidade populacional |
| **Ambiência — tráfego** | 2 *loops* (fluxo leve/pesado) | OGG q5 estéreo | Ligado à carga das arestas |
| **SFX — UI** | ~10: hover, clique, confirmar, cancelar, abrir/fechar painel, erro, *toggle*, slider | **WAV 16-bit 44,1 kHz mono** | Curtos (<1 s), baixa latência |
| **SFX — mundo** | ~12: zonear, construir estrada, demolir, prédio concluído, veículo, energia ligada, *brownout*, notificação | WAV 16-bit mono | 2–3 variações nos repetitivos |

**Normalização:** música ≈ **-16 LUFS**; SFX pico ≤ **-1 dBTP**.

---

## 6. Vídeo (opcional na Etapa 1)

| Asset | Formato | Resolução | Nota |
|---|---|---|---|
| Trailer / teaser | **H.264 MP4** (+ WebM VP9) | 1920×1080, 30 fps, ~10 Mbps | Para loja e site |
| Fundo animado do menu | **Não é vídeo** | — | O handoff pede a **cidade viva renderizada pelo engine** atrás do menu — usar o renderizador real, não um clipe |

---

## 7. Assets de loja (Steam) — quando houver página

Formatos oficiais do Steamworks (**reconfirmar na documentação no momento da submissão**):

| Asset | Tamanho |
|---|---|
| Header capsule | 460×215 |
| Small capsule | 231×87 |
| Main capsule | 616×353 |
| Vertical capsule | 374×448 |
| Page background | 1438×810 |
| Library capsule | 600×900 |
| Library header | 920×430 |
| Library hero | 3840×1240 |
| Library logo (PNG transparente) | 1280×720 |
| Screenshots | **1920×1080** (mín. 5) |

---

## 8. Lote mínimo da Etapa 1 (o que destrava o jogo jogável)

| # | Bloco | Qtde | Prioridade |
|---|---|---|---|
| 1 | Fontes (Marcellus + Lora) | 2 famílias | 🔴 Bloqueante |
| 2 | Wordmark + losango + textura de pergaminho | 3 | 🔴 Bloqueante |
| 3 | Controles de UI (9-slice + checkbox + slider + dropdown) | ~8 conjuntos | 🔴 Bloqueante |
| 4 | Tiles de terreno (5 tipos, com variações) | ~18 | 🔴 Bloqueante |
| 5 | Estradas (16 variantes) | 16 | 🔴 Bloqueante |
| 6 | Edifícios R/C/I (Low+Medium, 3 níveis) | 18 | 🟠 Alta |
| 7 | Overlays de zona vazia + obra | 6 | 🟠 Alta |
| 8 | Veículos (3 modelos × 4 direções) | 12 | 🟠 Alta |
| 9 | Ícones (ferramentas + overlays + HUD) | ~24 | 🟠 Alta |
| 10 | Cursores | 4 | 🟡 Média |
| 11 | Serviços (usina, água, parque, praça) | 4 | 🟡 Média |
| 12 | Água animada (8 quadros) | 1 sheet | 🟡 Média |
| 13 | Música (menu + 4 estações) | 5 faixas | 🟡 Média |
| 14 | Ambiência (5 loops) | 5 | 🟡 Média |
| 15 | SFX (UI + mundo) | ~22 | 🟡 Média |
| 16 | Ícone do app | 1 conjunto | 🟢 Baixa |
| 17 | Assets de loja | 10 | 🟢 Depois |

**Totais aproximados — Etapa 1:** ~**110 sprites/gráficos**, ~**27 arquivos de áudio**,
**2 famílias tipográficas**.

---

## 9. Checklist de aceite (por asset)

- [ ] Entregue em **@2x**, PNG-24 RGBA, alfa direto, sRGB
- [ ] **Ancoragem** no centro da base do losango (sprites de mundo)
- [ ] Respeita o **canvas** da tabela do footprint correspondente
- [ ] Alinha na grade **2:1** (teste sobrepondo o tile 128×64)
- [ ] Coerente com a paleta/direção **"Aegean Marble"**
- [ ] **Nomenclatura genérica** — sem marcas ou referências reais ✅ *(revisão obrigatória)*
- [ ] Arquivo-fonte editável entregue junto
- [ ] Licença/autoria registrada no `CREDITS.md`

---

## 10. Perguntas em aberto (destravam a produção)

1. **Estilo de renderização:** pintado à mão, 3D pré-renderizado em iso, ou vetorial? *(Recomendo
   **3D pré-renderizado** — mantém consistência de luz/perspectiva e escala para 8 direções e
   variações de nível com muito menos retrabalho.)*
2. **Direção de luz:** fixa (ex.: noroeste) para todos os sprites — confirmar antes do 1º lote.
3. **Ciclo dia/noite / estações visuais:** só música muda, ou terreno também? *(Afeta ×4 o volume de
   tiles — sugiro adiar as variações visuais para a Etapa 2.)*
4. **Sombras:** embutidas no sprite ou dinâmicas pelo engine?
5. **4º barramento de áudio (Efeitos):** aprovar a adição em `GameSettings`?
6. **Engine alvo** (Unity / Godot): muda formato de atlas e *import settings* — não muda a arte.

---

### Fontes de referência de resolução

- [Steam Hardware & Software Survey](https://store.steampowered.com/hwsurvey/Steam-Hardware-Software-Survey-Welcome-to-Steam) (dados de share consultados em 2026)
- [Trends from the April 2026 Steam Hardware Survey — The FPS Review](https://www.thefpsreview.com/2026/05/02/trends-from-the-april-2026-steam-hardware-survey/)
- [Steam Hardware Survey June 2026 — TechTimes](https://www.techtimes.com/articles/319581/20260703/steam-hardware-survey-june-2026-windows-11-tops-70-amd-closes-intel.htm)
