---
name: unity-rql
description: |-
  How to read and change anything in the Unity Editor with RQL (rql-query / rql-mutate / rql-schema): the query
  language, operators, sources (scene objects, assets, scenes, prefabs, console logs, Editor state), Editor operations
  (create, add components, scenes, prefabs, assets, selection, Play Mode) and what installed extensions add.
metadata:
  generated-by: mcp-plugin-dotnet
---
# RQL — query and change the Unity Editor

RQL is one pipeline language for the whole Editor: find objects, read members, filter, aggregate, and change them in one
call. Prefer one precise query over many exploratory calls.

- `rql-query` — read-only queries, compact table results.
- `rql-mutate` — queries ending with `set` / `call`; all-or-nothing, undoable (Ctrl+Z), `dry_run: true` previews.
- `rql-schema` — sources and component types in the scene; `rql-schema(type)` lists one type's members and methods.

```bash
unity-mcp-cli run-tool rql-query --input '{"query": "objects | where name contains \"Enemy\" | select $ref name"}'
unity-mcp-cli run-tool rql-mutate --input '{"query": "objects | where name == \"Player\" | set active = false"}'
```

## Language

```
RQL: source | stage | stage ...
Sources: objects (every GameObject in the loaded scenes, inactive included), roots, scenes, selection, prefab (the prefab open in prefab editing mode), console (Editor log entries), shaders, componentTypes, profiler (memory / rendering / frame snapshots; call Start / Stop / Save), tests (test methods; run a selection with tests-run query), testResults (last run), editor (Editor state and operations). Assets: unity.assets("t:Material") or unity.assets("t:Prefab", "Assets/Enemies"), unity.asset("Assets/Mats/Red.mat"). ref("Light#1234") resolves a $ref; unity.find("Player") or unity.find("Level/Pillar_1") a GameObject by unique name or path.
Stages:
  where <predicate>                  filter
  select a b.c alias=(expr)          project columns (wrap computed expressions in parentheses)
  sort <expr> [desc], ...            stable sort
  first N | skip N | count
  get <path>                         replace each item by a member (lists are flattened)
  recurse children [max-depth=N] [include-self=false]   walk the hierarchy (input items included by default)
  distinct [expr] | group-by expr | sum expr | min expr | max expr | avg expr
Expressions: == != < <= > >= and or not + - * / %; contains, starts-with, ends-with (case-insensitive); matches "regex"; x in [a, b]; x is null; x is not null; $ is Light (includes subclasses); has(member). Literals: numbers, "strings", true/false/null, lists [a, b], objects {x: 1, y: 2}.
Paths: a.b.c, list[0], map["key"]; .member on a list maps over its elements. List methods: .any(pred) .all(pred) .count([pred]) .sum(e) .min(e) .max(e) .first([pred]); inside them bare names refer to the element, falling back to the outer item.
Meta: $ (current item), $ref (stable id such as Light#1234), $type, $path, $index, $depth. Enums compare to strings (type == "Spot"); == on strings is case-sensitive.
Unity: GameObject has components (list), children (list), active, name, tag, layer, transform (position, localPosition, eulerAngles, localScale, parent). Component members are the C# members (public + [SerializeField]); find them via objects | get components | where $ is Light. Vectors/colors print as (x, y, z); write "(1, 2, 3)", [1, 2, 3] or {x: 1, y: 2, z: 3}; colors also "#ff8800". Material shader properties: properties["_Color"]. Destroyed objects read as null (missing references: where target == null). Navigate with paths or get; select only projects output columns.
Results come back as compact rows (header line, then col|col). Long results are truncated — refine with where/first or use count. Unknown members produce warnings with suggestions; rql-schema(type) lists a type's members.
```

## Changing things (rql-mutate)

```
  set path = expr, ...   assign members (paths may go through structs: set transform.position.y = transform.position.y + 1)
  call Method(args)      invoke a method the tools expose
Examples:
  objects | where name == "Player" | set transform.position = "(0, 1, 0)"
  objects | get components | where $ is Light | set intensity = intensity * 2, color = "#ffcc88"
  objects | where tag == "Enemy" | set active = false
  unity.asset("Assets/Mats/Red.mat") | set properties["_Color"] = "(1, 0, 0, 1)"
  objects | where name == "Spawner" | set targets = append(targets, ref("GameObject#1234"))
Create / add / remove (call; the receipt's result column holds the new object's $ref for follow-up queries):
  editor | call CreateGameObject("Props")                                   new root GameObject
  editor | call CreateGameObject("Crate", ref("GameObject#1234"), "Cube")    child of a parent, optional primitive
  objects | where name == "Crate" | call AddComponent("Rigidbody")           add a component by type name
  objects | where name == "Old" | call Destroy()                             remove a GameObject (components: get components | where $ is X | call Destroy())
  objects | where name == "Lamp" | set transform.parent = ref("GameObject#1234")   reparent (keeps the world position)
  objects | where name == "Crate" | call Duplicate()                          copy next to it (returns the copy)
Scenes, prefabs, assets, Editor state:
  editor | call OpenScene("Assets/Scenes/Level.unity")          editor | call NewScene("Assets/Scenes/New.unity")
  scenes | where path == "" | call SaveAs("Assets/Scenes/New.unity")   scenes | where name == "Level" | call SetActive()
  editor | call InstantiatePrefab("Assets/Prefabs/Enemy.prefab", ref("GameObject#1234"), "(0, 0, 5)")
  objects | where name == "Crate" | call SaveAsPrefab("Assets/Prefabs/Crate.prefab")
  editor | call OpenPrefab("Assets/Prefabs/Enemy.prefab")  then  prefab | recurse children | …  then  editor | call ClosePrefab()
  editor | call CreateMaterial("Assets/Mats/Glow.mat")        editor | call MoveAsset("Assets/A.mat", "Assets/Mats/A.mat")
  editor | set selection = [ref("GameObject#1234")]         editor | set isPlaying = true
  profiler | call Start()      profiler | call Save("Logs/profile.json")      (read: profiler | select memory frame)
```

## Sources

| Source | What |
|---|---|
| `componentTypes` | component types that can be added (name fullName namespace) |
| `console` | captured Editor log entries, oldest first (last 1000): logType message timestamp stackTrace sequence |
| `editor` | Editor state (isPlaying, isPaused, selection …) and operations: CreateGameObject, OpenScene, NewScene, SaveScenes, InstantiatePrefab, OpenPrefab / SavePrefab / ClosePrefab, CopyAsset / MoveAsset / DeleteAsset / CreateFolder, CreateMaterial, SaveAssets, RefreshAssets, ClearConsole, ExecuteMenuItem (rql-schema "editor" for signatures) |
| `objects` | every GameObject in the loaded scenes, depth-first, inactive ones included |
| `prefab` | root GameObject of the prefab open in prefab editing mode (none otherwise) |
| `profiler` | Unity profiler: enabled supported memory rendering script frame modules (live snapshots, MB / ms); calls Start, Stop, ClearFrames, EnableModule, Save, Load (rql-schema "profiler" for signatures) |
| `roots` | root GameObjects of the loaded scenes |
| `scenes` | loaded scenes |
| `selection` | objects selected in the Editor |
| `shaders` | every shader the Editor knows (built-in and project): name supported hasErrors |
| `testResults` | results of the last tests-run: name status durationMs message stackTrace |
| `tests` | test methods (fullName name fixture namespace assembly mode categories parameterized explicit ignored); run a selection with tests-run query, e.g. tests \| where fixture == "PlayerTests" |
| `tilemap` | tilemap operations without an owner: CreateTilemap (Grid + Tilemap GameObject), CreateTile, CreateRuleTile (tile assets) |
| `tilemap.cell(…)` | tilemap.cell(tilemap, cell): {cell, tile, path, sprite, color, colliderType, flags, transform} of one cell (tile null when empty) |
| `tilemap.cellToWorld(…)` | tilemap.cellToWorld(tilemap, cell): the world position of a cell's center |
| `tilemap.cells(…)` | tilemap.cells(tilemap, tile?): the first 20 occupied cells (optionally of one tile asset, path or name), bottom-left first |
| `tilemap.count(…)` | tilemap.count(tilemap, tile?, minX?, minY?, maxX?, maxY?, z?): occupied cells, optionally of one tile (asset, path or name) and inside a region |
| `tilemap.tileAt(…)` | tilemap.tileAt(tilemap, cell): the tile asset at a cell "(x, y, z)", or null |
| `tilemap.tiles(…)` | tilemap.tiles(tilemap, minX, minY, maxX, maxY, z?): tile names of a region as rows, top row (maxY) first, null = empty (at most 20 × 20) |
| `tilemap.worldToCell(…)` | tilemap.worldToCell(tilemap, "(x, y, z)"): the cell containing a world position |
| `unity.asset(…)` | unity.asset(path): the main asset at a path, e.g. "Assets/Mats/Red.mat" |
| `unity.assets(…)` | unity.assets(filter, folder?): AssetDatabase search, e.g. "t:Material", "t:Prefab Enemy" (at most 200, loaded as needed) |
| `unity.find(…)` | unity.find(name): the GameObject with that unique name or hierarchy path ("Level/Pillar_1"), e.g. set Follow = unity.find("Player") |

## Tilemap

Tilemaps are components on children of a `Grid`: `objects | get components | where $ is Tilemap`. Cells are `"(x, y, z)"` (z = 0 for 2D);
tile arguments take an asset path (`"Assets/Tiles/Grass.asset"`), a ref or `unity.asset(…)`; sprite arguments also take
the texture (`unity.asset("Assets/Art/grass.png")`), its path or `"Assets/Art/sheet.png#grass_3"` (one sprite of a sheet).

```
objects | get components | where $ is Tilemap | select $ref gameObject.name tileCount usedTiles cellBounds orientation
tilemap | call CreateTilemap("Ground")                                    name, parent?, cellSize?, gridName? (new Grid + Tilemap)
tilemap | call CreateTilemap("Walls", ref("GameObject#…"))                  parent with a Grid: another layer of that Grid
tilemap | call CreateTile("Assets/Tiles/Grass.asset", "Assets/Art/grass.png", "#FFFFFF", "Grid")   path, sprite?, color?, colliderType?
tilemap | call CreateRuleTile("Assets/Tiles/Ground.asset", "Assets/Art/ground.png#ground_4")        path, defaultSprite?, colliderType?
```

Painting (`call` needs rql-mutate; undoable):

```
objects | get components | where $ is Tilemap | where gameObject.name == "Ground" | call SetTile("(0, 0, 0)", "Assets/Tiles/Grass.asset")
objects | get components | where $ is Tilemap | call SetTile("(3, 1, 0)", null)                                erase one cell
objects | get components | where $ is Tilemap | call SetTiles(["(0, 0, 0)", "(1, 0, 0)", "(2, 0, 0)"], "Assets/Tiles/Grass.asset")
objects | get components | where $ is Tilemap | call BoxFill("Assets/Tiles/Grass.asset", -5, -3, 5, 3)          tile, minX, minY, maxX, maxY, z? (corners inclusive)
objects | get components | where $ is Tilemap | call ClearBox(0, 0, 2, 2)                                       returns how many tiles were removed
objects | get components | where $ is Tilemap | call FloodFill("(1, 1, 0)", "Assets/Tiles/Water.asset")
objects | get components | where $ is Tilemap | call ClearAllTiles()
objects | get components | where $ is Tilemap | call SetColor("(0, 0, 0)", "#FF8080")                           tint one painted cell
objects | get components | where $ is Tilemap | call SetTransform("(0, 0, 0)", 90, "(1, 1, 1)", true, false)     rotation°, scale?, flipX?, flipY?
objects | get components | where $ is Tilemap | call SetColliderType("(0, 0, 0)", "None")                       None / Sprite / Grid
objects | get components | where $ is Tilemap | set orientation = "XZ", tileAnchor = "(0.5, 0.5, 0)", color = "(1, 1, 1, 0.5)"
objects | get components | where $ is Tilemap | call CompressBounds()                                           shrink cellBounds to the painted cells
objects | get components | where $ is Grid | set cellSize = "(1, 0.5, 0)", cellLayout = "Isometric"
objects | get components | where $ is TilemapRenderer | set sortingOrder = 2, mode = "Individual"
```

Reading cells (functions work in rql-query):

```
objects | get components | where $ is Tilemap | select t=(tilemap.tileAt($, "(0, 0, 0)"))                     the tile asset or null
objects | get components | where $ is Tilemap | select c=(tilemap.cell($, "(0, 0, 0)"))                        {cell, tile, path, sprite, color, colliderType, flags, transform}
objects | get components | where $ is Tilemap | select rows=(tilemap.tiles($, 0, 0, 9, 9))                       tile names, top row (maxY) first, at most 20 × 20
objects | get components | where $ is Tilemap | select n=(tilemap.count($, "Grass"))                            occupied cells of a tile (name, path or asset); region: ($, tile, minX, minY, maxX, maxY)
objects | get components | where $ is Tilemap | select cells=(tilemap.cells($, "Assets/Tiles/Grass.asset"))      first 20 cells holding that tile
objects | get components | where $ is Tilemap | select c=(tilemap.worldToCell($, "(2.5, 1.2, 0)")) w=(tilemap.cellToWorld($, "(2, 1, 0)"))
```

Tile assets are ordinary objects: `unity.assets("t:TileBase") | select $ref name`,
`unity.asset("Assets/Tiles/Grass.asset") | set sprite = "Assets/Art/grass2.png", colliderType = "Grid"`. RuleTile rules:

```
unity.asset("Assets/Tiles/Ground.asset") | call AddRule("Assets/Art/ground.png#ground_1", "NNN/T.T/TTT")   sprite, pattern, colliderType?
unity.asset("Assets/Tiles/Ground.asset") | call ClearRules()
objects | get components | where $ is Tilemap | call RefreshAllTiles()                                          repaint after editing rules
```

A pattern lists the 8 neighbors in rows top to bottom (center ignored): `T` = this tile, `N` = not this tile, `.` = any;
rules are checked in order and the first match wins (`"..."` = always). Methods on empty cells (SetColor, SetTransform,
SetColliderType), regions over 1 048 576 cells and bad paths fail without changing anything. `cellBounds.max` is exclusive.
Created assets are not removed by Undo.

## Installed RQL modules

- `com.IvanMurzak.Unity.MCP.Editor.Rql.Modules.UnityCoreModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.ToolTypesModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics.MathematicsModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D.PhysicsModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps.TilemapModule`
