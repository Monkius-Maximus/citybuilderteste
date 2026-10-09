/**
 * CityDocument v1 — esquema canônico.
 *
 * REGRA FUNDAMENTAL:
 * Este documento é a única verdade persistida. O grid de simulação é uma
 * projeção derivada dele (ver `rasterize`), e nunca o caminho inverso.
 *
 * Toda geometria está em METROS. Nenhum campo deste arquivo usa tiles.
 * Tiles existem apenas dentro do subsistema de simulação.
 */

// ---------------------------------------------------------------------------
// UNIDADES
// ---------------------------------------------------------------------------

/** Distância no espaço de mundo. Sempre metros. */
export type Meters = number & { readonly __unit: 'm' };

/** Índice de célula no grid de simulação. Nunca sai do subsistema de simulação. */
export type TileIndex = number & { readonly __unit: 'tile' };

/** Ângulo em graus, [0, 360). */
export type Degrees = number & { readonly __unit: 'deg' };

export const meters = (n: number): Meters => n as Meters;
export const degrees = (n: number): Degrees => n as Degrees;

export interface WorldPoint {
  readonly x: Meters;
  readonly y: Meters;
  /** Cota do terreno. Derivada do TerrainField no momento da colocação. */
  readonly z: Meters;
}

export interface WorldPoint2 {
  readonly x: Meters;
  readonly y: Meters;
}

// ---------------------------------------------------------------------------
// ESCALA
// ---------------------------------------------------------------------------

/**
 * Fixada na criação do documento. Imutável.
 *
 * `metersPerTile` define quantos metros uma célula de simulação cobre.
 * Recomendação: 8. Justificativa:
 *   - uma casa unifamiliar (8x10m) ocupa ~1 célula
 *   - um quarteirão urbano (~80m) cai em 10 células — resolução suficiente
 *     para tráfego e cobertura de serviços sem explodir o custo por tick
 *   - convergência registrada: Cities: Skylines também usa célula de 8x8m.
 *     Convergência não é prova; os dois pontos acima é que sustentam a escolha
 *
 * Alterar este valor invalida o documento. Não há migração de escala:
 * a rasterização é lossy e reconstruir a partir dela seria adivinhação.
 */
export interface WorldScale {
  readonly metersPerTile: Meters;
}

// ---------------------------------------------------------------------------
// DOCUMENTO
// ---------------------------------------------------------------------------

export interface CityDocument {
  readonly version: 1;
  readonly id: string;
  readonly meta: DocumentMeta;
  readonly scale: WorldScale;
  readonly bounds: WorldBounds;
  readonly terrain: TerrainField;
  readonly networks: NetworkGraph;
  readonly placements: readonly Placement[];
  /**
   * Incrementado a cada `apply` bem-sucedido. Usado pelo bridge para
   * invalidação de snapshot e pelo renderer para descarte de cache.
   */
  readonly revision: number;
}

export interface DocumentMeta {
  readonly name: string;
  readonly createdAt: string;   // ISO 8601
  readonly modifiedAt: string;  // ISO 8601
  /** Identificador do catálogo de protótipos exigido para abrir o documento. */
  readonly catalogId: string;
}

export interface WorldBounds {
  readonly width: Meters;  // extensão em X
  readonly depth: Meters;  // extensão em Y
}

// ---------------------------------------------------------------------------
// TERRENO
// ---------------------------------------------------------------------------

/**
 * Heightmap regular em metros.
 *
 * DECISÃO: as amostras ficam nos VÉRTICES, não nos centros das células.
 * Um campo de (cols+1) x (rows+1) descreve cols x rows quadriláteros, e cada
 * quadrilátero tem seus quatro cantos definidos sem ambiguidade. Amostrar no
 * centro forçaria interpolação para achar bordas, e bordas são exatamente o
 * que a validação de declive precisa medir.
 *
 * DECISÃO: `sampleSpacing` é independente de `metersPerTile`. O terreno pode
 * ter resolução mais fina que a simulação (ex.: 4m de terreno para 8m de tile)
 * sem que um subsistema contamine o outro.
 */
export interface TerrainField {
  readonly sampleSpacing: Meters;
  readonly cols: number;   // número de colunas de QUADRILÁTEROS
  readonly rows: number;   // número de linhas de QUADRILÁTEROS
  /**
   * Altura em metros por vértice, ordem row-major.
   * Comprimento exigido: (cols + 1) * (rows + 1).
   * Um comprimento diferente é documento inválido — lance, não ajuste.
   */
  readonly heights: Float32Array;
  /** Cota do nível do mar. Vértices abaixo disso são água. */
  readonly seaLevel: Meters;
}

export function heightIndex(field: TerrainField, ix: number, iy: number): number {
  const stride = field.cols + 1;
  if (ix < 0 || iy < 0 || ix > field.cols || iy > field.rows) {
    throw new RangeError(`vértice de terreno fora dos limites: (${ix}, ${iy})`);
  }
  return iy * stride + ix;
}

// ---------------------------------------------------------------------------
// REDES (VIÁRIO E AFINS)
// ---------------------------------------------------------------------------

/**
 * Ruas como grafo geométrico, não como células pintadas.
 *
 * Esta é a decisão que torna o documento exportável. Uma malha viária em
 * polilinhas métricas se traduz direto para GeoJSON, para spline de engine 3D
 * ou para navmesh. Uma malha viária em tiles não se traduz para nada sem
 * heurística de reconstrução.
 *
 * A simulação de tráfego consome a versão rasterizada; a exportação e a edição
 * consomem o grafo. Ambos derivam da mesma fonte.
 */
export interface NetworkGraph {
  readonly nodes: readonly NetworkNode[];
  readonly edges: readonly NetworkEdge[];
}

export interface NetworkNode {
  readonly id: string;
  readonly position: WorldPoint;
  /**
   * Interseção explícita. Duas edges que se cruzam sem compartilhar um node
   * são um viaduto, não um cruzamento — a distinção é geométrica, não inferida.
   */
  readonly kind: 'intersection' | 'endpoint';
}

export interface NetworkEdge {
  readonly id: string;
  readonly fromNodeId: string;
  readonly toNodeId: string;
  /** Referência ao catálogo: define largura, faixas, velocidade. */
  readonly roadTypeId: string;
  /**
   * Geometria entre os dois nodes, exclusiva dos endpoints.
   * Vazio = segmento reto. Nunca inclui as posições dos nodes.
   */
  readonly controlPoints: readonly WorldPoint[];
}

export interface RoadType {
  readonly id: string;
  readonly width: Meters;
  readonly lanes: number;
  readonly speedLimitKph: number;
}

// ---------------------------------------------------------------------------
// COLOCAÇÕES
// ---------------------------------------------------------------------------

export interface Placement {
  readonly id: string;
  readonly prototypeId: string;
  /** Posição do ANCHOR do protótipo, não do canto do bounding box. */
  readonly position: WorldPoint;
  readonly rotationDeg: Degrees;
}

/**
 * Dimensões reais em metros. É aqui que se resolve o defeito de escala do
 * TheoTown: o carro tem 4.5m porque carros têm 4.5m, independentemente de
 * como o renderer escolhe desenhá-lo.
 */
export interface Footprint {
  readonly width: Meters;
  readonly depth: Meters;
  readonly height: Meters;
}

export interface Prototype {
  readonly id: string;
  readonly kind: PrototypeKind;
  readonly footprint: Footprint;
  readonly anchor: 'center' | 'min-corner';
  /** Declive máximo aceitável sob o footprint, em graus. */
  readonly maxSlopeDeg: Degrees;
  readonly meshRef?: string;
  readonly spriteRef?: string;
}

export type PrototypeKind = 'building' | 'prop' | 'vehicle' | 'agent' | 'terrain-feature';

// ---------------------------------------------------------------------------
// VERSIONAMENTO
// ---------------------------------------------------------------------------

/**
 * `version` é literal, não número livre. Um documento v2 falha o type-check
 * contra este esquema em compile time, não em runtime.
 *
 * Migração é sempre unidirecional e explícita: v1 -> v2 é uma função dedicada
 * que produz um tipo diferente. Não existe leitor tolerante que aceite ambas
 * as versões — isso é o caminho para código que ninguém consegue mais alterar.
 */
export interface DocumentEnvelope {
  readonly schema: 'city-document';
  readonly version: 1;
  readonly document: CityDocument;
}

export function parseDocument(raw: unknown): CityDocument {
  const env = raw as DocumentEnvelope;
  if (env?.schema !== 'city-document') {
    throw new TypeError('não é um city-document');
  }
  if (env.version !== 1) {
    throw new TypeError(`versão de esquema não suportada: ${env.version}`);
  }
  return env.document;
}

// ---------------------------------------------------------------------------
// FRONTEIRA COM A SIMULAÇÃO
// ---------------------------------------------------------------------------

export interface TileCoord {
  readonly tx: TileIndex;
  readonly ty: TileIndex;
}

export function tileOf(p: WorldPoint2, scale: WorldScale): TileCoord {
  return {
    tx: Math.floor(p.x / scale.metersPerTile) as TileIndex,
    ty: Math.floor(p.y / scale.metersPerTile) as TileIndex,
  };
}

export function tileOrigin(c: TileCoord, scale: WorldScale): WorldPoint2 {
  return {
    x: (c.tx * scale.metersPerTile) as Meters,
    y: (c.ty * scale.metersPerTile) as Meters,
  };
}

/**
 * Projeta o documento no grid de simulação.
 * Lossy e unidirecional por definição. Não existe `derasterize`.
 */
export declare function rasterize(doc: CityDocument): SimMap;

export interface SimMap {
  readonly cols: number;
  readonly rows: number;
  readonly scale: WorldScale;
  /** Tipo de tile por célula, row-major. */
  readonly tiles: Uint16Array;
}
