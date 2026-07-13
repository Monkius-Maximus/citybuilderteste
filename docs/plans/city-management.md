# Plano — Gerenciamento de Cidades: Biblioteca, Seeds, Import/Export

> **Status:** proposta aprovável · **Escopo:** single-player · **Referências:** TheoTown (lista de
> cidades, arquivos portáteis, compartilhamento), SimCity 4 (identidade de cidade, re-fundação,
> visão de região — esta última só como direção futura).
>
> Multiplayer lockstep foi **despriorizado** por decisão do product owner; os blocos já criados
> (codec/replay/checksum) permanecem no core como infraestrutura de replay/verificação, sem
> custo de manutenção adicional.

## 1. Objetivo

Dar ao jogador um ciclo de vida completo para suas cidades, fora da simulação:

1. **Biblioteca de cidades** — listar, criar, renomear, duplicar e excluir cidades salvas
   (a tela *Load City* vira um gerenciador, como no TheoTown).
2. **Seeds como cidadãos de primeira classe** — ver a seed de qualquer cidade, re-fundar um
   mundo a partir dela e compartilhar "códigos de fundação" em texto.
3. **Import/Export** — levar cidades entre máquinas/pessoas como um arquivo único, com
   verificação de integridade e compatibilidade de versão.

## 2. Fundação existente (por que isso é barato agora)

| Já temos | Onde | O que habilita |
|---|---|---|
| Save binário autocontido `.polis` (v2) | `Persistence/SaveGame` | O arquivo **já é** o formato portátil de export |
| Bloco de metadados barato (nome, pop, tesouro, tick, salvo-em) | `SaveGame.ReadMetadata` / `SaveMetadata` | Listagens instantâneas sem carregar mundos |
| Varredura de diretório ordenada | `Shell/SaveCatalog.Scan` | Embrião da biblioteca (hoje é só leitura) |
| Checksum determinístico de estado | `Persistence/StateChecksum` | Verificação de integridade no import |
| Terreno 100% regenerável por seed | `Grid/TerrainGenerator` | Preview/compartilhamento de mundo só com a seed |
| Config carrega nome + terreno + seed | `GameConfig` | Identidade da cidade viaja dentro do save |
| Máquina de telas + eventos | `Shell/GameShell` | Ponto único para plugar as novas ações de UI |
| `AutosaveInterval` já no Settings | `Shell/GameSettings` | Falta só o executor de autosave (entra na M1) |
| Handoff pede rename/delete + thumbnails | `docs/design/main-menu/README.md` §3 | O design já reservou espaço para isso |

## 3. Escopo

**Dentro:** biblioteca CRUD, autosave com rotação, códigos de fundação (seed sharing),
export/import com integridade, thumbnails embutidas no save, migração v2→v3.

**Fora (registrado como direção, sem design agora):**
- **Regiões estilo SC4** (várias cidades num mapa regional, vizinhos, deals de energia/água).
  Nota de arquitetura na §9 para não fecharmos portas — mas nenhum código nesta fase.
- Compartilhamento online (workshop/galeria). O export em arquivo já cobre o caso manual.
- Multiplayer.

## 4. Arquitetura proposta — namespace `CityBuilder.Library`

Novo módulo no Core (engine-agnostic como tudo), consumindo `Persistence` e exposto ao `Shell`:

```
src/CityBuilder.Core/Library/
├── CityLibrary.cs        # o gerenciador: CRUD sobre um diretório de saves
├── CitySlot.cs           # entrada da biblioteca (caminho + SaveMetadata + flags)
├── AutosaveService.cs    # executor da política AutosaveInterval (rotação de slots)
├── FoundingCode.cs       # seed+tamanho+terreno+nome <-> código de texto compartilhável
├── CityPackage.cs        # export/import: contêiner .polispack com manifesto+integridade
└── ThumbnailRenderer.cs  # minimapa RGBA a partir de terreno+zoneamento (paleta placeholder)
```

### 4.1 `CityLibrary` (M1)

Evolução do `SaveCatalog` (que passa a delegar para cá; API antiga mantida como fachada):

```csharp
public sealed class CityLibrary
{
    public CityLibrary(string directory);                    // cria o diretório se preciso

    public IReadOnlyList<CitySlot> Refresh();                // varre + ordena (recente primeiro)
    public CitySlot Save(GameSimulation sim, string? slotName = null);   // grava (atômico)
    public GameSimulation Load(CitySlot slot, Action<GameSimulation> bootstrap);
    public CitySlot Rename(CitySlot slot, string newCityName);           // regrava metadados
    public CitySlot Duplicate(CitySlot slot, string? copyName = null);   // "Nova Polis (2)"
    public void Delete(CitySlot slot);                       // move p/ .trash/ (não destrutivo)
    public event Action? LibraryChanged;                     // UI observa e re-renderiza
}
```

Decisões de design:

- **Escrita atômica:** grava em `nome.polis.tmp` e faz `File.Replace/Move` — um crash no meio
  de um save nunca corrompe o slot anterior (regra de ouro de TheoTown/SC4).
- **Nome de arquivo ≠ nome da cidade:** arquivo é *slug* estável (`nova-polis-3f2a.polis`,
  sufixo curto do hash da seed) e o nome exibido vive nos metadados. Renomear cidade não
  renomeia arquivo ⇒ sem quebra de referências, colisões triviais de resolver.
- **Delete não destrutivo:** move para subpasta `.trash/` (ignorada pelo `Refresh`). Purga
  manual/automática fica para depois; recuperação barata contra mis-click.
- **Rename sem carregar o mundo:** reescreve só o campo de nome no bloco de config/metadados
  (offset conhecido do formato) ou, mais simples e seguro, load→config alterada→save. Fase 1
  usa o caminho simples; a otimização de offset só se o custo incomodar em mapas 256².
- **`Load` recebe o bootstrap do host** (definições/sistemas), mantendo a regra existente do
  `SaveGame`: carregar = reconstruir com o mesmo bootstrap de jogo novo.

### 4.2 `AutosaveService` (M1)

Liga o `AutosaveInterval` (já configurável na tela Settings) ao relógio da simulação:

```csharp
public sealed class AutosaveService
{
    public AutosaveService(CityLibrary library, GameSettings settings, int rotationSlots = 3);
    public void Update(GameSimulation sim, TimeSpan realElapsed);  // host chama por frame
}
```

- Rotação `cidade.auto1/2/3.polis` (mais antigo é sobrescrito), flag `IsAutosave` no `CitySlot`
  para a UI agrupar/estilizar. Intervalo em **tempo real** (como no design: "Every 10 min"),
  não em ticks — autosave é conforto do jogador, não estado da simulação.

### 4.3 `FoundingCode` (M2) — seeds compartilháveis

Um código de texto curto que recria o **setup de fundação** (não o mundo construído):

```
POLIS-T128-VP-314159        (legível)      tamanho, terreno, seed
ou forma compacta base32 c/ dígito verificador para colar em chat
```

```csharp
public static class FoundingCode
{
    public static string Encode(in GameConfig config);        // nome fica de fora por padrão
    public static bool TryDecode(string code, out GameConfig config, out string? error);
}
```

- Round-trip garantido com `TerrainGenerator` (mesmo código ⇒ mesmo mundo, bit-idêntico).
- Entra no `NewCityForm` como campo opcional "founding code" que preenche os controles —
  zero UI nova além de um input (o design da tela New City comporta).
- Dígito verificador (CRC curto) para rejeitar códigos digitados errado com mensagem clara.

### 4.4 `CityPackage` (M2) — export/import

O `.polis` já é portátil; o pacote adiciona **integridade e contexto** para troca entre pessoas:

```
.polispack (binário):
┌ magic "CBPK" + versão do pacote
├ manifesto: versão do save contido, versão do jogo, SaveMetadata copiado,
│            checksum FNV-1a dos bytes do payload
└ payload: bytes do .polis, verbatim
```

```csharp
public static class CityPackage
{
    public static void Export(CitySlot slot, Stream destination);
    public static ImportResult Import(Stream source, CityLibrary destination);
    // ImportResult: Ok(slot) | ChecksumMismatch | UnsupportedVersion(found, supported) | Corrupt
}
```

- **Import seguro por construção:** verifica magic → versão → checksum **antes** de tocar a
  biblioteca; nome em colisão ganha sufixo (`Nova Polis (importada)`); nunca sobrescreve.
- **Compatibilidade:** manifesto permite mensagem exata ("save v2, este jogo lê v3+") em vez
  de erro genérico. Política pré-1.0: ler `N-1` com migração (§4.6).
- **CLI headless no App** (prova engine-agnostic e vira ferramenta de dev):
  `CityBuilder.App export <slot> <arquivo>` · `import <arquivo>` · `list`.

### 4.5 `ThumbnailRenderer` (M3) — minimapas da tela Load

O handoff pede "real minimap thumbnails". Geramos no **save** (não no load, que precisa ser
instantâneo):

```csharp
public static class ThumbnailRenderer
{
    // Downsample de terreno+zoneamento p/ RGBA usando PlaceholderSpriteFactory/paleta
    public static byte[] Render(GameSimulation sim, int width = 64, int height = 44);
}
```

- **Save v3:** bloco de metadados ganha `thumbWidth, thumbHeight, rgba[]` (64×44×4 ≈ 11 KB —
  irrelevante no tamanho do arquivo). `SaveMetadata` expõe os bytes; cada engine converte para
  textura do seu jeito (é só RGBA cru — zero dependência de PNG/encoder).
- Enquanto o v3 não chega, a UI usa o glifo de 3 losangos do design (já especificado).

### 4.6 Versionamento & migração (M3)

- `SaveGame` passa a ler **v2 e v3**: leitor despacha por versão; v2 → thumbnail ausente.
- Regra escrita no código e no doc: **pré-1.0 lemos N-1**; escrever é sempre na versão atual.
- `CityPackage.Import` reaproveita a mesma tabela de compatibilidade.

### 4.7 Integração com o Shell (transversal)

`GameShell` ganha as ações que o design já previa (kebab/contexto na linha do save):

```csharp
public event Action<CitySlot>? RenameRequested;    // UI abre input inline
public event Action<CitySlot>? DeleteRequested;    // UI confirma → library.Delete
public event Action<CitySlot>? ExportRequested;    // host abre file picker
public event Action?           ImportRequested;    // host abre file picker → CityPackage.Import
```

A tela *Load City* atual (linhas + LOAD) não muda de layout; ações extras entram no menu de
contexto conforme a nota do próprio handoff.

## 5. Fases de entrega (cada uma shippável e testável no console)

| Fase | Conteúdo | Critério de aceite (demo headless) |
|---|---|---|
| **M1 — Biblioteca** | `CityLibrary` CRUD + escrita atômica + `.trash/` + `AutosaveService` + fachada `SaveCatalog` | criar 3 cidades → listar → renomear → duplicar → excluir → lixeira; autosave rotaciona 3 slots |
| **M2 — Portabilidade** | `FoundingCode` + `CityPackage` + verbos CLI no App | export → corromper 1 byte → import recusa por checksum; código de fundação re-gera mundo bit-idêntico (censo igual) |
| **M3 — Polimento** | `ThumbnailRenderer` + save v3 + leitor v2/v3 | save v3 carrega thumb; save v2 antigo ainda abre; thumbnail bate com censo do terreno |
| **M4 — Shell** | eventos de rename/delete/export/import no `GameShell` + fluxo completo no demo | demo percorre: fundar → autosave → exportar → importar → listar com thumbs |

Sem dependências externas novas em nenhuma fase; tudo `netstandard2.1` + IO já usado.

## 6. Decisões em aberto (respostas suas destravam M1)

1. **Lixeira:** mover para `.trash/` (proposto) ou apagar direto com confirmação na UI?
2. **Rotação de autosave:** 3 slots é bom padrão? (TheoTown usa 1–3; SC4 um único "auto").
3. **Founding code:** forma legível (`POLIS-T128-VP-314159`), compacta (base32), ou as duas?
4. **Thumbnail:** 64×44 (proporção do glifo do design) está bom, ou prefere maior (96×66)?
5. **Extensão do pacote:** `.polispack` dedicado (proposto) ou exportar o próprio `.polis`?
   (Dedicado permite manifesto/checksum sem inflar todo save do dia a dia.)

## 7. Riscos & mitigação

- **Corrupção em disco** → escrita atômica (M1) + checksum no pacote (M2) + skip-and-report na
  listagem (já existe no `SaveCatalog`).
- **Evolução do formato** → versionamento explícito + tabela de migração única (§4.6);
  testes de round-trip por versão quando o projeto de testes nascer.
- **IO em engines com sandbox** (mobile/console futuros) → `CityLibrary` recebe o diretório
  raiz de fora; nada de caminhos absolutos no Core. (Se um dia precisarmos de storage abstrato,
  extraímos `IFileStore` — o desenho atual não bloqueia.)

## 8. O que explicitamente NÃO muda

Simulação, determinismo, replay, formato dos comandos e o fluxo de load (= reconstruir com o
mesmo bootstrap). A biblioteca é uma casca de gestão **em volta** do que já existe.

## 9. Nota de direção — Regiões (futuro, estilo SC4)

Quando (se) formos para regiões: uma região é um diretório de cidades + um manifesto
(`region.polisregion`) com o grid de lotes e, por lote, o slug do save. Conexões de vizinhança
viram nós de borda nas `FlowNetwork`s (a estrutura de grafo atual já suporta nós "fantasma" de
fronteira). Nada disso é necessário agora; o desenho da biblioteca (slug estável por cidade,
metadados baratos) foi escolhido para **não conflitar** com esse futuro.
