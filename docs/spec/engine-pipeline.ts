/**
 * Pipeline de mutação do engine.
 *
 * DIREÇÃO ÚNICA:
 *   UI -> Command -> validate -> apply -> CityDocument' -> EngineEvent -> UI
 *
 * Nenhum componente fora deste arquivo escreve no documento. Se a UI precisar
 * mudar algo, ela emite um Command. Não há exceção nem atalho.
 */

import type {
  CityDocument, WorldPoint, Degrees, Meters, TileIndex, WorldScale,
} from './city-document-v1.js';

// ---------------------------------------------------------------------------
// COMANDOS
// ---------------------------------------------------------------------------

/**
 * União discriminada fechada. Adicionar um comando quebra o switch de `apply`
 * em compile time — que é exatamente o comportamento desejado.
 */
export type Command =
  | { readonly kind: 'place';        readonly prototypeId: string; readonly at: WorldPoint; readonly rotationDeg: Degrees }
  | { readonly kind: 'remove';       readonly placementId: string }
  | { readonly kind: 'drawRoad';     readonly path: readonly WorldPoint[]; readonly roadTypeId: string }
  | { readonly kind: 'eraseRoad';    readonly edgeId: string }
  | { readonly kind: 'sculptTerrain';readonly at: WorldPoint; readonly radius: Meters; readonly delta: Meters }
  | { readonly kind: 'setTaxRate';   readonly category: RciCategory; readonly rate: number };

export type RciCategory = 'residential' | 'commercial' | 'industrial';

export type RejectionReason =
  | 'out-of-bounds'
  | 'unknown-prototype'
  | 'unknown-road-type'
  | 'unknown-placement'
  | 'unknown-edge'
  | 'overlaps-placement'
  | 'slope-too-steep'
  | 'below-sea-level'
  | 'path-too-short'
  | 'insufficient-funds'
  | 'tax-rate-out-of-range';

// ---------------------------------------------------------------------------
// VALIDAÇÃO E APLICAÇÃO
// ---------------------------------------------------------------------------

export interface EngineContext {
  readonly catalog: CatalogLookup;
  readonly funds: number;
}

export interface CatalogLookup {
  hasPrototype(id: string): boolean;
  hasRoadType(id: string): boolean;
  costOf(prototypeId: string): number;
}

/**
 * Retorna o motivo da rejeição, ou `null` se o comando é aplicável.
 * Não corrige, não ajusta, não sugere alternativa.
 */
export declare function validate(
  doc: CityDocument,
  ctx: EngineContext,
  cmd: Command,
): RejectionReason | null;

/**
 * Aplica um comando JÁ VALIDADO e devolve o novo documento.
 *
 * CONTRATO: se `validate` retornou `null`, esta função não falha.
 * Uma exceção aqui é bug de invariante, nunca fluxo de negócio.
 * Por isso ela revalida e lança — o custo é irrelevante e o bug aparece cedo.
 */
export declare function apply(
  doc: CityDocument,
  ctx: EngineContext,
  cmd: Command,
): CityDocument;

// ---------------------------------------------------------------------------
// EVENTOS
// ---------------------------------------------------------------------------

/**
 * Três eventos. Fechado.
 *
 * Expor nomes crus de callback do runtime como se fossem eventos de domínio
 * obriga cada consumidor a inventar o próprio parsing, e o envelope nunca
 * estabiliza. Envelope próprio e fechado desde o dia um.
 */
export type EngineEvent =
  | { readonly type: 'tick';             readonly tick: number; readonly state: GameState }
  | { readonly type: 'document.changed'; readonly revision: number; readonly changed: ChangeSet }
  | { readonly type: 'command.rejected'; readonly cmd: Command; readonly reason: RejectionReason };

export interface GameState {
  readonly funds: number;
  readonly population: number;
  readonly gameDate: string;   // ISO 8601
  readonly demand: Readonly<Record<RciCategory, number>>;
}

/** Delta mínimo para invalidação de cache do renderer. */
export interface ChangeSet {
  readonly placements: readonly string[];
  readonly edges: readonly string[];
  readonly terrainDirtyRect: TerrainRect | null;
}

export interface TerrainRect {
  readonly minX: Meters; readonly minY: Meters;
  readonly maxX: Meters; readonly maxY: Meters;
}

// ---------------------------------------------------------------------------
// BRIDGE
// ---------------------------------------------------------------------------

export type Unsubscribe = () => void;

export interface EngineBridge {
  /** Única entrada de mutação do sistema. */
  dispatch(cmd: Command): void;
  subscribe(listener: (e: EngineEvent) => void): Unsubscribe;
  /** Leitura imutável. O renderer nunca recebe a instância viva. */
  snapshot(): WorldSnapshot;
}

export interface WorldSnapshot {
  readonly revision: number;
  readonly document: CityDocument;
  readonly sim: SimSnapshot;
  readonly state: GameState;
}

// ---------------------------------------------------------------------------
// SNAPSHOT DE SIMULAÇÃO (resultado da rasterização)
// ---------------------------------------------------------------------------

/**
 * Ver ADR-001 para a decisão de cobertura fracionária das vias.
 */
export interface SimSnapshot {
  readonly cols: number;
  readonly rows: number;
  readonly scale: WorldScale;
  /** Tipo de tile por célula, row-major. */
  readonly tiles: Uint16Array;
  /** Fração da célula coberta por via, 0..255. Ver ADR-001. */
  readonly roadCoverage: Uint8Array;
  /** Edge dominante da célula. 0 = nenhuma. Índice+1 em `networks.edges`. */
  readonly roadEdge: Uint32Array;
}

export function cellIndex(sim: SimSnapshot, tx: TileIndex, ty: TileIndex): number {
  if (tx < 0 || ty < 0 || tx >= sim.cols || ty >= sim.rows) {
    throw new RangeError(`célula fora dos limites: (${tx}, ${ty})`);
  }
  return ty * sim.cols + tx;
}
