# Project Handoff Documentation: Jelly Field Clone

This document serves as a complete developer guide for continuing the development of the **Jelly Field Clone** project. It outlines the project's goal, architecture, current implementation state, key files, guidelines, and the immediate next steps.

---

## 1. Project Goal & Target Resolution
- **Goal**: Recreate the core gameplay of *Jelly Field*—a grid-based puzzle game with soft-jelly blocks, color combinations, block morphing, and cascading matches.
- **Aspect Ratio**: Optimized exclusively for **1080x1920 mobile portrait** viewports. All UI layers and layouts must scale correctly and respect notch/safe-areas.
- **Visuals**: Premium, clean look. 
  - **Background**: Solid dark brown camera background (`#1F1A14`).
  - **Grid Slots**: Rounded-corner dark purple-grey square slots (#201E26) with a thick lighter border outline (#322D3E).

---

## 2. Core Architecture & Frameworks

### NFramework (UI & Data Management)
The project utilizes `NFramework` (located in `Assets/ThirdParty/nframework`) for UI loading and game managers:
1. **Views/UI Panels**: All UI screens inherit from `BaseUIView` (which provides life-cycle methods like `OnOpen()` and `OnClose()`).
   - `EUILayer.Menu` (e.g., [HomeMenu.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/UI/Menu/HomeMenu.cs), [GamePlayMenu.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/UI/Menu/GamePlayMenu.cs))
   - `EUILayer.Popup` (e.g., [LoadingPopup.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/UI/Popup/LoadingPopup.cs), [SettingPopup.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/UI/Popup/SettingPopup.cs), [ResultPopup.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/UI/Popup/ResultPopup.cs))
2. **UIManager**: A global singleton `UIManager.I` used to load and open/close Views by string ID.
3. **UIPrefabGenerator**: An Editor menu script ([UIPrefabGenerator.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Editor/UIPrefabGenerator.cs)) generates UI prefabs programmatically under `Assets/Game/Resources/UI/`.
   - **Important**: Any modification to UI structure should be done by updating this generator script and executing the menu item `Game/Generate UI Prefabs` in Unity, rather than editing prefabs directly in the scene.
4. **UserData**: Singleton handling save state, best scores, and coins. Integrates with `SaveManager` for automatic local persistence.

### Manager Singletons
- **GameManager**: Located in [GameManager.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Manager/GameManager.cs). Manages high-level state flow:
  - `LOADING` -> `FIRST` -> `HOME` -> `INGAME` -> `RESET`
  - Handles additive loading and unloading of the gameplay scene (`Game`).
- **GameplayManager**: Located in [GameplayManager.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Manager/GameplayManager.cs). Manages local gameplay states:
  - `NONE` -> `BEGIN` -> `PLAYING` -> `CHECK` -> `LOSE` -> `RESULT` -> `REVIVE`

---

## 3. Gameplay Mechanics & Implementation Details

### A. The 4x4 Grid Matrix ([JellyGrid.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Gameplay/JellyGrid.cs))
- **Scale**: The grid slot GameObjects are scaled to `0.45f` (visual size: `0.9` Unity units).
- **Spacing**: `slotStep` is `1.05f`, leaving an exact `0.15f` spacing gap between cells.
- **Visuals**: Rounded purple square sprites with a thick `8f` border outline are dynamically drawn on a `128x128` texture at runtime.
- **Initialization**: Starts completely empty on level launch.
- **Match Logic**: Checks boundaries between adjacent cells orthogonally. If touching cells share the same color, they match and are queued for elimination.
- **Cascading check**: A recursive loop keeps checking for new matches as empty cells fill and blocks morph.

### B. The 2x2 Cell Blocks ([JellyBlock.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Gameplay/JellyBlock.cs))
- **Representation**: Blocks are a 2x2 grid of sub-cells, each having a string color ID (`blue`, `red`, `yellow`, `green`, `purple`) or null.
- **Scale**: Root block GameObject is scaled to `0.45f` to match the background grid cells perfectly.
- **Morphing Rules**:
  1. If colors are eliminated from the block:
  2. If **1 color** is left, the block morphs into a solid block (all 4 cells take that color).
  3. If **2 or more colors** are left, empty cells are filled by copying the color of an adjacent cell in the block.
  4. If **0 colors** are left, the block is destroyed.
- **Visual Tweens**: Custom spring-mass feel bounce on landing, elastic breathing wiggles while idle, and stretch distortion while dragging.

### C. The Launcher & Drag Snapping ([JellyLauncher.cs](file:///d:/unity/MyGame/Jelly%20Field%20Clone/Assets/Game/Scripts/Gameplay/JellyLauncher.cs))
- **Randomizer**: Spawns block configurations with randomized sub-cell colors (1 solid color: 40%, 2 split colors: 40%, 4 split colors: 20%).
- **Drag Interaction**: Mouse-to-world conversion. Drag selection radius is set to `1.0f`.
- **Drag Scaling**: Swells to `1.15x` of current scale (`0.45f * 1.15f = 0.5175f`) when held, and shrinks back to `0.45f` when snapped or returned.
- **Replenish Logic**: When a block is successfully placed on the grid, a replacement spawns in the launcher slot immediately.

---

## 4. Current Progress & Setup Status
- The visual scaling has been successfully modified to fit a `1080x1920` layout cleanly.
- The solid camera background has been styled to a warm dark brown `#1F1A14`.
- Grid cell outlines have been thickened (`8f`) and adjusted to high contrast.
- The `GamePlayMenu` HUD (scores, coins, booster, pause) has been completely stripped to provide a clean canvas for coding gameplay incrementally.
- **The codebase currently compiles 100% cleanly (0 compile errors, 0 warnings).**

---

## 5. Immediate Next Steps for Claude Code

### Step 1: Consolidate to a Single Centered Spawn Point
The gameplay scene `Game.unity` currently has two spawn slots: `SpawnSlot_0` (at `x: -1.5, y: -4.5`) and `SpawnSlot_1` (at `x: 1.5, y: -4.5`). The goal is to reduce this to only **one** centered spawn slot.

#### Action Items in `Game.unity` (YAML Scene File):
1. **Remove `SpawnSlot_1` GameObject**: Locate and delete the GameObject named `SpawnSlot_1` (fileID: `1865688191` / transform fileID: `1865688192`) and its reference in the scene root roots list.
2. **Reposition and Rename `SpawnSlot_0`**:
   - Rename `SpawnSlot_0` (fileID: `96466897` / transform fileID: `96466898`) to `SpawnSlot`.
   - Change its `m_LocalPosition` to `{x: 0, y: -3.5, z: 0}` (or `{x: 0, y: -4.5, z: 0}`) so it sits centered below the 4x4 grid.
3. **Update `JellyLauncher` Inspector references**:
   - Locate the `JellyLauncher` component (fileID: `1648872170` on GameObject `1648872169`).
   - Find the `_spawnSlots` array serialization.
   - Resize the serialized array to **1 element** and set its first element target to point to `SpawnSlot`'s Transform (fileID: `96466898`).

### Step 2: Verify Single Block Snapping Flow
1. Open the project in Unity and enter Play Mode.
2. Verify that only a single block spawns at the bottom center.
3. Drag the block:
   - If released over a valid grid slot, it snaps, and a new block spawns immediately at the spawn slot.
   - If released over an invalid slot/empty space, it returns to the centered spawn slot.
