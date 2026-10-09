# Sprint 0 — Handoff para o Claude Code

> ⚠️ **Executar somente no repositório próprio do editor.** Nunca no
> `citybuilderteste`, que é o jogo e já contém `CityBuilder.Core` com outro
> padrão de comando. Decisão de 2026-10-09 (Opção A, dois repositórios).

Tarefa única: **portar a especificação TypeScript para C#** e deixar o
esqueleto do repositório compilando e testando. Sem UI, sem Godot rodando,
sem lógica de negócio implementada.

## Contexto a anexar

| Arquivo | Papel |
|---|---|
| `CLAUDE.md` | Invariantes e princípios. Leitura obrigatória. |
| `docs/spec/city-document-v1.ts` | Esquema canônico a portar. Contrato, não código. |
| `docs/spec/engine-pipeline.ts` | Comandos, validate/apply, eventos, bridge. |
| `docs/adr/adr-001-rasterizacao-de-vias.md` | Justifica `RoadCoverage` e `RoadEdge`. |
| `docs/adr/adr-002-pipeline-de-asset.md` | Justifica `MeshRef` / `SpriteRef`. |

**Não anexar** `MEMORIA-PROJETO.md` nem `backlog-sprints.html`. São documentos
de estratégia; poluem o contexto sem informar o port.

## Entregáveis

### 1. Solução e projetos

```
CityEditor.sln
src/CityEditor.Core/          net8.0, sem referência a Godot
src/CityEditor.Core.Tests/    net8.0, xUnit
src/CityEditor.Godot/         projeto Godot 4.x, referencia Core
```

`CityEditor.Core.csproj` com `Nullable=enable`, `LangVersion=latest`,
`TreatWarningsAsErrors=true`.

### 2. Tipos de unidade

`Meters`, `TileIndex`, `Degrees` como `readonly record struct` sobre `float` /
`int`.

- Operadores aritméticos e de comparação onde fizerem sentido.
- **Sem conversão implícita de/para `float`.** O ponto do tipo é impedir que
  metro e tile se misturem; conversão implícita anula isso. Conversão explícita
  é aceitável.
- `JsonConverter` para cada um, serializando como número puro (`8.0`), não como
  objeto (`{"Value": 8.0}`).

### 3. Modelo do documento

Portar de `city-document-v1.ts`: `WorldPoint`, `WorldScale`, `WorldBounds`,
`CityDocument`, `DocumentMeta`, `TerrainField`, `NetworkGraph`, `NetworkNode`,
`NetworkEdge`, `RoadType`, `Placement`, `Footprint`, `Prototype`,
`PrototypeKind`, `TileCoord`, `SimMap`.

- `record` com propriedades `init`.
- `Float32Array` → `float[]`; `Uint8Array` → `byte[]`; `Uint16Array` →
  `ushort[]`; `Uint32Array` → `uint[]`.
- `readonly T[]` do TS → `IReadOnlyList<T>`.
- `Version` como propriedade `init` com valor 1 e validação na desserialização.

**Armadilha conhecida:** a igualdade sintetizada de `record` compara membros de
array **por referência**. Um `CityDocument` desserializado nunca será igual ao
original enquanto `TerrainField.Heights` for `float[]`. Implemente `Equals` e
`GetHashCode` manuais em `TerrainField` (e em qualquer tipo com array),
comparando conteúdo. O round-trip do Sprint 1 depende disso.

### 4. Comandos

Hierarquia fechada:

```csharp
public abstract record Command
{
    private Command() { }              // impede herança externa

    public sealed record Place(...)         : Command;
    public sealed record Remove(...)        : Command;
    public sealed record DrawRoad(...)      : Command;
    public sealed record EraseRoad(...)     : Command;
    public sealed record SculptTerrain(...) : Command;
}
```

`SetTaxRate` **não** é portado — imposto é jogo, não autoria.

`RejectionReason` como `enum`. Remover `InsufficientFunds` e
`TaxRateOutOfRange` pelo mesmo motivo.

### 5. Guarda de exaustividade

Ver a seção "Exaustividade" abaixo — é a parte não óbvia deste sprint.

### 6. Assinaturas do engine

`Validate` e `Apply` declarados com a assinatura correta, corpo lançando
`NotImplementedException`. Implementação é Sprint 1.

Mesma coisa para `EngineEvent`, `ChangeSet`, `IEngineBridge`, `WorldSnapshot`,
`SimSnapshot`, `CellIndex`.

`GameState` **não** é portado — é jogo.

### 7. Testes

- Ida e volta de cada tipo de unidade pelo `JsonConverter`.
- `HeightIndex` lança `ArgumentOutOfRangeException` fora dos limites.
- `CellIndex` idem.
- `TileOf` e `TileOrigin` são consistentes entre si.
- O teste de exaustividade descrito abaixo.

## Exaustividade — leia com atenção

O desenho original em TypeScript dependia de união discriminada: adicionar um
comando quebrava o `switch` em compile time.

**C# não reproduz isso, e a solução que parece óbvia não funciona.** Um `switch
expression` sobre hierarquia de `record` emite CS8509 *mesmo quando todos os
casos estão cobertos* — o compilador não consegue provar que a hierarquia é
fechada, porque o construtor de cópia protegido dos `record` permite herança
externa em tese. Com `TreatWarningsAsErrors`, um switch completo falharia o
build. Ligar a flag e considerar o problema resolvido é armadilha.

**Solução adotada: teste de reflexão.** Um teste que enumera todos os subtipos
de `Command` no assembly e afirma que cada um é tratado por `Validate` e por
`Apply`. Adicionar um comando sem tratá-lo quebra o CI.

Nos `switch` sobre `Command`: **nunca use braço `_ =>`**. Cubra cada tipo
explicitamente e deixe o caso `null` explícito se necessário. O braço descarte
esconde exatamente o erro que se quer pegar.

`TreatWarningsAsErrors` continua ligado — só não é ele que garante
exaustividade.

## Critérios de aceite

- `dotnet build` verde, sem warnings, com `TreatWarningsAsErrors`.
- `dotnet test` verde.
- O projeto Godot abre e compila referenciando o Core.
- Nenhum `using Godot` em `CityEditor.Core`.
- Nenhum braço `_ =>` em switch sobre `Command`.
- Nenhuma conversão implícita entre `Meters` e `float`.

## Fora do escopo deste sprint

Implementar `Validate`, `Apply`, `Rasterize` ou `Resolve`. Qualquer cena ou nó
Godot além do projeto vazio. Qualquer UI. Serialização do documento completo —
isso é Sprint 1.
