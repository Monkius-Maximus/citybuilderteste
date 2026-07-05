# Handoff: THE GAME OF POLIS — Main Menu Suite ("Aegean Marble" direction, option 1a)

## Overview
Main-menu UI for **THE GAME OF POLIS**, an isometric city builder (repo: `Monkius-Maximus/citybuilderteste`, C# / CityBuilder.Core). This package covers the full pre-game flow: **Title screen → New City setup → Load City → Settings → (stub) in-game handoff point**. The chosen visual direction is **"Aegean Marble"** — a classical, light, editorial look: warm parchment surfaces, Marcellus/Lora serif typography, gold hairline accents, over a live isometric city rendered with the engine's placeholder palette.

## About the Design Files
The files in this bundle are **design references created in HTML** — interactive prototypes showing intended look and behavior, **not production code to copy directly**. The task is to **recreate these designs in the game's target environment** (the C# game client / its UI framework — e.g. MonoGame/ImGui/custom UI layer, whatever the project uses or adopts). Match the visuals and behavior described below using that environment's idioms.

- `Polis Menus.dc.html` — the prototype. **Only option `1a` (the first frame, "AEGEAN MARBLE") is the approved design.** Options 1b and 1c in the same file were rejected explorations; ignore them.
- `iso-city.js` — canvas renderer used as the animated menu background. It is a *reference implementation* of "render the actual game world behind the menu"; in the real game, use the live engine renderer instead.
- `support.js` — prototype runtime only. Ignore.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and copy are final intent. Recreate pixel-perfectly (allowing for font-stack substitutions if Marcellus/Lora can't be shipped — see Typography).

## Global Design Language

### Colors (design tokens)
- Parchment surface: `rgba(247,243,233,0.96)` (`#F7F3E9` at 96% over the scene)
- Title-screen wash: radial gradient, center 50%/42%: `rgba(246,242,231,.95)` → `.8` at 46% → `rgba(240,234,218,.4)` at 76% → `.12` at edge
- Ink (headings): `#232A35`
- Body/menu text: `#4A4331`; hover ink: `#1D2531`
- Muted/label gold-brown: `#8A7A4D`; footer/meta: `#97896B`; secondary text: `#6B6250`, `#6D6449`
- Accent gold: `#A08339` (rules, diamonds, underlines)
- Hairline gold: `#B9A878` (hover borders); panel border: `#CBBF9F`; row divider: `#DDD3B8`
- Input border: `#C9BD9E`; input background: `#FFFDF6`; input text: `#2B3140`
- Secondary button border: `#A5966D`, text `#6B6046`; hover bg `rgba(160,131,57,.12)`, text `#4A4030`
- Primary action (deep Aegean blue): `#24506E`, text `#F2EEE1`; hover `#1B3E57`
- Modal scrim: `rgba(38,34,24,0.45)`
- Panel shadow: `0 20px 50px rgba(30,25,10,0.3)`
- Engine placeholder palette (background city — from `PlaceholderSpriteFactory`): grass `rgb(96,168,88)`, water `rgb(48,96,200)`, road `rgb(60,60,66)`, residential `rgb(64,180,96)`, commercial `rgb(72,128,220)`, industrial `rgb(220,196,72)`, civic `rgb(180,90,200)`

### Typography
- Display serif: **Marcellus** (400 only) — game title, panel headings, save names
- Body serif: **Lora** (400/500/600 + italic) — everything else
- Both are Google Fonts (OFL licensed, shippable). Fallback: any classical serif (e.g. EB Garamond) keeping the same sizes/tracking.
- Scale: title 104px (tracking .12em); panel heading 30px (tracking .04em); section heading 17px; save name 19px; body 14–16px; labels 11px uppercase (tracking .18em); menu items 14px uppercase (tracking .22em); footer 10px uppercase (tracking .24em)

### Shape & spacing
- Panels: 2–4px corner radius (nearly square), 1px `#CBBF9F` border, 36px top/bottom × 42px side padding
- Inputs/buttons: 2px radius; inputs 12px×14px padding; buttons 12px×24–28px padding
- Gold divider under panel headings: 56px × 2px `#A08339`, 10px below heading
- Modal panels are centered; widths: New City 560px, Load 640px, Settings 620px

## Screens

### 1. Title screen (main menu)
- Full-screen live isometric city (day theme, animated vehicles) behind a warm radial parchment wash (see tokens) — strongest at center, fading to ~12% at edges so the city stays visible around the border.
- Centered stack:
  - Kicker: "THE GAME OF" — Lora 13px, tracking .5em, `#8A7A4D`
  - Title row: "POLIS" — Marcellus 104px `#232A35`, flanked each side by a 90×1px gold rule + 7px gold diamond (square rotated 45°, scaleY .6), 20px gaps
  - Tagline: "an isometric city builder" — Lora italic 16px `#6D6449`, 14px below
  - 44px below: vertical menu — CONTINUE / NEW CITY / LOAD CITY / SETTINGS / EXIT. Each item: Lora 14px uppercase, tracking .22em, `#4A4331`, 280px wide, 11px vertical padding, centered.
    - Hover: ink darkens to `#1D2531`, 1px `#B9A878` rules appear above and below, background `rgba(255,255,255,.5)`
    - EXIT is shown at 40% opacity (disabled in prototype)
- Footer (bottom center, 22px up): "PRE-ALPHA 0.1 · DETERMINISTIC SIMULATION CORE · 10 TICKS / S" — Lora 10px, tracking .24em, `#97896B`
- Navigation: CONTINUE → in-game; NEW CITY → New City modal; LOAD CITY → Load modal; SETTINGS → Settings modal

### 2. New City ("Found a New City")
Modal panel (560px) over scrim; menu screen remains behind.
- Heading "Found a New City" (Marcellus 30px) + gold divider
- Fields (label style: Lora 11px uppercase `#8A7A4D`, 24/20px above, 8px below):
  - **CITY NAME** — text input, default "Nova Polis"
  - **MAP SIZE** — 3 equal-width segmented cards: `64 × 64 / HAMLET`, `128 × 128 / TOWNSHIP`, `256 × 256 / METROPOLIS`. Card: 11px×8px padding, 1px `#B9AB80` border, bg `rgba(255,253,246,.6)`, label 14px/600 + sublabel 11px tracking .12em. Selected: bg `#24506E`, text `#F4EFE4`. Default: 128×128.
  - **WORLD SEED** — numeric text input + "RANDOMIZE" secondary button (sets a random 6-digit seed)
  - **TERRAIN** — select: Verdant Plains (default) / River Delta / Coastal Reach / Highlands
- Footer row (30px above): "BACK" secondary button (left) ↔ "FOUND CITY" primary blue button (right). FOUND CITY → starts game with chosen params.

### 3. Load City
Modal panel (640px). Heading "Load City" + gold divider.
- One row per save, divided by 1px `#DDD3B8` rules; row hover bg `rgba(255,255,255,.55)`. Row anatomy (16px gap):
  - 56×40px minimap glyph: three overlapping isometric diamonds colored by the save's dominant zone colors (from the engine palette)
  - Name (Marcellus 19px) over metadata line (Lora 13px `#6B6250`): "Population 12,480 · § 45,120 · Year 4 — Spring" (`§` is the in-game currency glyph)
  - Relative timestamp (Lora 12px `#97896B`)
  - "LOAD" outline button: 1px `#24506E` border, `#24506E` text, 11px uppercase tracking .16em; hover inverts to filled blue
- Sample data: Nova Polis (2 h ago), Porto Verde (yesterday), Ferrum Vale (last week)
- "BACK" secondary button bottom-left. LOAD → in-game.
- Real implementation needs: real minimap thumbnails, delete/rename affordances (not designed yet — keep row layout, add a kebab/context menu).

### 4. Settings
Modal panel (620px). Heading "Settings" + gold divider. Three sections, each with a Marcellus 17px section heading over a 1px `#CBBF9F` rule (24/20px above, 4px below). Each row: label (Lora 14px `#4A4331`) left, control right, 9px vertical padding; sliders 200px wide, accent color `#24506E`; checkboxes 18px, same accent.
- **Audio**: Master volume (80), Music (60), Ambience & traffic (70) — sliders 0–100
- **Graphics**: UI scale (slider 75–150, default 100), V-Sync (on), Building animations (on)
- **Gameplay**: Autosave (select: Off / Every 5 min / **Every 10 min** / Every 30 min), Edge scrolling (on), Tutorial tips (on)
- Footer: "BACK" (left, discards) / "APPLY" primary (right, saves) → both return to menu

### 5. In-game (stub)
The prototype shows a small parchment toast at bottom-center ("Simulation running…", § balance, BACK TO MENU). This is **only a placeholder** marking the handoff point to the phase-2 in-game HUD — not a design to implement.

## Interactions & Behavior
- Screen transitions: fade + 10px rise, ~300–400ms ease (`opacity 0→1`, `translateY(10px)→0`)
- All hover states listed per component above; use pointer cursor on all interactive elements
- Keyboard (implement even though prototype is mouse-only): ↑/↓ menu navigation, Enter select, Esc closes modal back to menu
- Touch: all targets ≥ 44px tall (menu items are 40px + spacing — pad to 44px minimum on touch)
- Menu background: live city with animated vehicle dots; menus must never block the simulation render

## State Management
- `screen`: `menu | new | load | settings | game`
- New City form: `{ name: string, mapSize: 64|128|256, seed: string, terrain: enum }` — seed randomizer generates 6-digit int
- Settings: persisted config `{ masterVol, musicVol, ambienceVol, uiScale, vsync, buildingAnims, autosaveInterval, edgeScroll, tutorialTips }`; BACK discards, APPLY commits
- Save list: `{ name, population, treasury, year, season, lastPlayed, thumbnail }[]`

## Assets
No bitmap assets. Fonts from Google Fonts (Marcellus, Lora). The `§` currency glyph is plain text. Background city and minimap glyphs are rendered from engine data with the existing placeholder palette.

## Files
- `Polis Menus.dc.html` — interactive prototype; **frame 1a only** (first frame; badge "1a"). Click through all four screens there.
- `iso-city.js` — reference background renderer (palette + iso projection math)
- `support.js` — prototype runtime, ignore
