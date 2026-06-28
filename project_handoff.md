# Project Handoff — Jelly Field Clone (Unity)

> **Đây là project CLONE game mobile "Jelly Field"** (match-color puzzle với khối thạch).
> Bản gốc tham chiếu: `D:\APK\Jelly` (dump AssetRipper — script đã decompile thành **stub rỗng**,
> nhưng **material / level / prefab / mesh dùng được**) + video gameplay gốc ở
> `Assets\Video\YTDown_YouTube_Jelly-Field-Android-Gameplay_Media_bZssNybANA4_001_1080p.mp4`.
>
> ✅ **GAMEPLAY + VISUAL + FEEL ĐÃ ỔN ĐỊNH và được người dùng duyệt.** Việc TIẾP THEO là **chỉnh sửa
> prefab UI** — KHÔNG cần đụng lại logic gameplay (chỉ chỉnh nếu UI yêu cầu). Giữ nguyên các hệ thống dưới.

Unity URP, **color space = Linear**, mobile portrait. Mở scene `Main.unity` để chạy.

---

## 1. Luật chơi (đã clone)
Match-color: board gồm các **ô (cell)**; mỗi ô chứa **1 khối jelly 2×2 sub-cell** (mỗi sub-cell 1 màu).
Kéo khối từ **pickup slot** dưới đáy lên đặt vào ô board. Khi sub-cell **cùng màu của 2 khối kề nhau chạm
nhau qua mép** → khớp và **triệt tiêu**. Đạt đủ số lượng từng màu mục tiêu (targets) thì **thắng**; board đầy
mà chưa đạt thì **thua**. 20 level thiết kế tay, sau đó **chơi vô hạn (endless)**.

## 2. Kiến trúc & luồng
- **NFramework** (`Assets/ThirdParty/nframework`): UI (`BaseUIView`, `UIManager.I`), `UserData`, `SaveManager`, `SingletonMono`.
- **GameManager** (Main scene): `LOADING → FIRST → HOME → INGAME → RESET`. Lúc INGAME **load additively scene `Game`**.
- **GameplayManager**: `NONE → BEGIN → PLAYING → CHECK → LOSE → RESULT → REVIVE`. Vào BEGIN thì spawn board.
- **Test nhanh INGAME qua MCP** (vì phải qua HOME): reflection gọi `GameManager.EnterInGame()` rồi ẩn canvas tên `Menu`.
  Board load **bất đồng bộ** (~1–1.5s) → query sau khi `SkinnedMeshRenderer` xuất hiện.

## 3. Hệ thống gameplay (ĐÃ ỔN ĐỊNH — đừng phá)
- **Board / match / eliminate** — `Assets/Game/Scripts/Gameplay/JellyGrid.cs`
  - Sinh ô từ `LevelData.ParseBoard()` (`0`=ô, `-`=lỗ); fill khối theo `fillSeed/fillRate/weights`.
  - `slotStep=1.0`. Ô = prefab `Assets/PrefabInstance/Cell.prefab` (ô **xám sẫm bo góc**, màu sprite `Board (1)` = `0.235,0.255,0.295`).
  - **`GridBackground` đã bị TẮT** (trước đó là tấm nền tối, cùng sortingOrder với ô → dưới camera nghiêng nó **sort đè che ô hàng trên thành đen**; tắt đi vừa hết che vừa bỏ nền đen quanh board).
  - **Viền trắng preview khi kéo**: bắt child `"Ghost Highlight"` của mỗi ô → `ShowPlacementPreview/ClearPlacementPreview` (đặt highlight đồng phẳng + đúng tâm ô để không lệch dưới camera nghiêng).
  - **Animation triệt tiêu**: sub-cell khớp màu **nổi lên + phồng → MERGE về mép chung giữa 2 khối → thu nhỏ biến mất** (`MarkMerge` truyền điểm mép; thực thi trong `JellyBlock.PopSubCellsAsync`).
- **Khối jelly** — `Assets/Game/Scripts/Gameplay/JellyBlock.cs`
  - 4 sub-cell đặt `localPosition ±0.21`, `localScale 0.54` → **chồng mép thành 1 khối bo góc liền mạch** (cùng màu liền, khác màu chia vùng — như gốc).
  - **Màu**: 8 màu lấy đúng `_BaseColor` từ TCP2 của APK (`GetColorFromId`).
  - **Shader**: `Assets/Game/Shaders/JellyCandy.shader` ("Jelly/Candy") — unlit "fake-lit" (half-lambert + specular + rim + saturation) → **màu rực + bóng kẹo dẻo + khối 3D**, render được trên SkinnedMeshRenderer (URP Lit/matcap cũ KHÔNG hợp: bị xám / không render trên skinned mesh).
  - **Jelly wobble (rung như cục thạch khi kéo)**: lò xo "trailing" trong `LateUpdate` (`BeginDragWobble/EndDragWobble`); dừng tay thì **lắc qua lại vài nhịp "boing boing" rồi tắt**. Hằng số chỉnh: `WOBBLE_STIFF/DAMP/DRIVE/MAX/LEAN`.
- **Kéo-thả / pickup** — `Assets/Game/Scripts/Gameplay/JellyLauncher.cs`
  - Chiếu tia lên mặt board (đúng camera nghiêng), dùng **camera cùng scene Game** (KHÔNG `Camera.main` — xem gotcha).
  - Khi nhấc: giữ offset điểm nắm, **nhấc block lên cao hơn ngón tay (DRAG_LIFT_Y) + về phía camera (z âm)** để không bị ngón tay che / hòa màu.
  - **Ô đích tính theo VỊ TRÍ BLOCK** (chiếu tâm block xuống board `ProjectToBoard`), không theo vị trí chuột.
  - Pickup slot visual scale `1.0` (vừa khít khối).
- **Mục tiêu / thắng-thua** — `Assets/Game/Scripts/Gameplay/JellyGameManager.cs`
  - Cờ `_resolved` đảm bảo **TriggerWin/TriggerLose chỉ chạy 1 lần/level** (trước đây fire nhiều lần → `CompleteLevel` +nhiều → **nhảy bậc level**; đã fix).
- **Level / endless** — `Assets/Game/Scripts/Manager/LevelManager.cs`
  - 20 `LevelData` (`Assets/Game/ScriptableObjects/Level_01..20.asset`) chơi theo thứ tự; **từ level 21 trở đi bốc ngẫu nhiên từ pool, xác định theo index** (ổn định khi retry, tránh lặp liền trước). **UI số level (`CurrentLevelIndex+1`) tăng vô hạn.**

## 4. Camera & UI nổi (world-space)
- Camera Game (`Assets/Game/Scenes/Game.unity`): **orthographic, nghiêng nhìn từ dưới lên** pitch **−15°**, pos `(0, −3.15, −12)`, `orthographic size 5.4`, clearFlags=SolidColor, **bg dark slate `(0.09,0.10,0.13)`**.
- UI count/level là **world-space canvas** → dùng `Assets/Game/Scripts/UI/FaceCameraFlat.cs` để **luôn quay chính diện** (không nghiêng theo camera). `LevelTextUI` hiển thị `Level {CurrentLevelIndex+1}`.

## 5. Gotchas (lưu ý khi làm tiếp)
- **CÓ 2 camera tag `MainCamera`** (Main scene + Game scene) → `Camera.main` có thể trỏ NHẦM. Code gameplay/UI nên lấy **camera cùng scene** (xem `FaceCameraFlat.ResolveCamera`, `JellyLauncher.Cam`).
- Mesh jelly (`Assets/PrefabInstance/Jelly.prefab`) là SkinnedMeshRenderer, AABB lệch — cẩn thận khi đổi scale/recenter.
- Recompile khi đang Play sẽ **reset play mode** → MCP có thể báo "board not ready"; vào lại INGAME.
- Frame video gốc đã trích bằng ffmpeg ở scratchpad (`frames/`, `frames2/`, `elim/`, `drag/`) — tham chiếu look/animation.

## 6. Map file chính
- Gameplay: `Assets/Game/Scripts/Gameplay/{JellyGrid,JellyBlock,JellyLauncher,JellyGameManager}.cs`
- Manager: `Assets/Game/Scripts/Manager/{GameManager,GameplayManager,LevelManager}.cs`
- Data: `Assets/Game/Scripts/Data/LevelData.cs`; SO: `Assets/Game/ScriptableObjects/Level_01..20.asset`
- UI: `Assets/Game/Scripts/UI/**` (HomeMenu, GamePlayMenu, popups, LevelTextUI, GoalCounterUI, FaceCameraFlat) + `Editor/UIPrefabGenerator.cs`
- Shader: `Assets/Game/Shaders/JellyCandy.shader`; Prefab: `Assets/PrefabInstance/{Cell,Jelly}.prefab`
- Scene: `Assets/Game/Scenes/{Main,Game}.unity`

## 7. VIỆC TIẾP THEO: chỉnh sửa prefab UI
Gameplay đã ổn → tập trung vào **UI** (HomeMenu, GamePlayMenu/HUD, các popup Win/Lose/Setting/Shop, goal counter, level text…). Lưu ý NFramework: cân nhắc chỉnh qua `UIPrefabGenerator` (menu `Game/Generate UI Prefabs`) HOẶC sửa prefab trực tiếp — kiểm tra cách dự án đang dùng trước khi sửa. **Không cần thay đổi logic gameplay** trừ khi UI yêu cầu data mới.
