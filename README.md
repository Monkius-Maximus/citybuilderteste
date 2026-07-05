# THE GAME OF POLIS (citybuilderteste)

Fundação de arquitetura em **C# puro**, **engine-agnostic**, para **THE GAME OF POLIS** — um
jogo 2D isométrico do tipo *city builder / tycoon* (inspiração: SimCity 4, OpenTTD). A lógica
de simulação é estritamente separada da apresentação, roda headless em um Console Application e
foi pensada para portabilidade futura para **Unity** ou **Godot**.

Identidade visual aprovada: **"Aegean Marble"** — handoff completo em
[`docs/design/main-menu/`](docs/design/main-menu/README.md), tokens integrados ao core em
`Presentation/AegeanMarbleTheme` e fluxo de menus em `Shell/`.

Implementação **clean-room** (nada derivado de código GPL/proprietário) e com metadados
estritamente genéricos de veículos/edifícios (ex.: `CompactHatch_Tier1`).

## Solução

| Projeto | TFM | Papel |
|---------|-----|-------|
| `src/CityBuilder.Core` | `netstandard2.1` | Biblioteca de simulação (sem dependência de engine) |
| `src/CityBuilder.App`  | `net8.0` | Host de console que roda a simulação headless |

## Rodar

```bash
dotnet run --project src/CityBuilder.App
```

## Sistemas incluídos nesta etapa

- **Grid & Matemática Isométrica** — coordenadas, camadas e projeção 2:1 com elevação.
- **Simulation Tick Engine** — loop determinístico *fixed-timestep*, sistemas em cadências variadas.
- **ECS leve** — entidades com geração + componentes em *sparse-set* (composição sobre herança).
- **Zoneamento & Autômatos Celulares** — mapas de calor (desejabilidade/poluição/crime) + CA com double-buffer.
- **Logística & Rede de Fluxo** — grafos de rodovia/ferrovia/água/energia com pesos dinâmicos.
- **Pathfinding** — A* (pesos dinâmicos de congestionamento), Dijkstra e *flow fields* (Dijkstra maps).
- **Tráfego & Movimento** — veículos roteados por A* que se movem, criam congestionamento e desviam; buffers de rota *pooled*.
- **Utilidades** — energia/água por cobertura de flow field de Dijkstra + alocação de capacidade (brownout).
- **Padrões** — Object Pooling, Factory (orientado a dados), Command (undo/redo + base multiplayer) e Observer/Pub-Sub.
- **Economia** — ciclo sobre os contratos: impostos das zonas + faturamento de utilidades − manutenção → tesouro, com mercados de trabalho/bens (comando de imposto integrado, com undo).
- **Persistência & Replay** — save binário do estado persistente, log de comandos serializável e replay determinístico verificado por checksum (fundação do multiplayer lockstep).
- **Shell & Identidade Visual** — tokens do design "Aegean Marble", máquina de telas do menu (New City/Load/Settings), terreno procedural determinístico por preset, calendário Ano/Estação e saves `.polis` com metadados para a tela Load.
- **Apresentação** — contratos de view + geração procedural de *placeholders* (polígonos coloridos isométricos).

Detalhes completos, decisões de projeto e diretório comentado: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).
