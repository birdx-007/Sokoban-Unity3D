# BoxPushing 推箱子

## 基础需求

基于unity，以产品级（非原型级）制作一个推箱子游戏。

需要包含关卡编辑器，及由该编辑器制作的若干关卡（关卡编辑器开发完成后手动制作）。

美术方面，拟使用unity3d内置的简单模板几何体搭配风格化shader及光照，构建极简主义风格化美学。

相机采取3D俯视角（参考《沉星之序》），视野覆盖关卡。

## 成品演示

关卡内操作方式：方向键移动，Z撤销上一步，X重做

![选择关卡](README.assets\选择关卡.png)

![关卡画面](README.assets\关卡画面.png)

![关卡编辑器2](README.assets/关卡编辑器2.png)

## 框架设计

### 关卡数据

关卡空间为2D网格，包含墙、地面、玩家、箱子，此外还有“玩家位于目标格”、“箱子位于目标格”两种状态。Sokoban社区采取XSB风格字符网格表示关卡空间，在此基础上加上元数据用json封装。关卡数据例：

```json
{
  "name": "Level 01",
  "map": [
    "########",
    "#      #",
    "# ...  #",
    "# $@   #",
    "#   $  #",
    "#   $  #",
    "#      #",
    "########"
  ]
}
```

XSB语义规定，\# 墙、 地面、. 目标、@ 玩家、$ 箱子、+ 玩家在目标、* 箱子在目标。

关卡数据统一存放在Assets/Data/Levels/下，命名为“Sokoban_{合法自定义后缀名}.json”。

### 关卡编辑器

形态为Unity编辑器内基于UI Toolkit的EditorWindow。菜单栏入口为Sokoban/Level/LevelEditor。

#### 关卡列表管理界面

关卡编辑器的第一层级，管理关卡列表，维护各关卡在列表中的顺序。关卡列表数据为位于Assets/Data/下的LevelList.asset（ScriptableObject），该数据包含一个列出Assets/Data/Levels/下所有关卡json的引用的有序列表（List），只能由关卡列表管理界面维护（禁止通过其inspector面板修改，且会在inspector提示需要通过关卡列表管理界面维护）。

在该界面中，可进行新关卡的添加（默认添加到列表末尾）、已有关卡在列表中的位置修改（选择并拖动对应项到目标位置）、删除已有关卡（会弹窗进行二次确认）。这些改动操作发生后，保存和回滚按钮会出现，保存会弹窗显示当前轮的所有改动操作，确认后所有的改动才会真正执行（新建关卡数据、将被删除的关卡数据移入操作系统回收站、写回LevelList数据）。

选择指定关卡后，可以点击“进入关卡编辑”进入第二层级。（该按钮在发生上述改动操作后会变得暂不可用，直至保存或回滚）

#### 关卡内容编辑界面

关卡编辑器的第二层级，进入该层级（通过第一层级）时自动加载对应的关卡数据。上侧编辑关卡的元数据（name等），下侧编辑关卡网格（map），类似Tilemap编辑器，具备笔刷、擦除工具。此外还提供关卡校验工具（见后文）用于保存关卡数据时校验关卡可玩性。

#### 关卡校验工具

静态校验（恰好 1 玩家、箱子数 == 目标数、边界闭合）、BFS可达性检查。

### 整体架构

分为自上而下单向依赖的四层：表现（Presentation）、游戏系统（Application）、底层逻辑（Domain）、数据（Data）。底层逻辑不反向依赖 Unity。

#### 表现（Gameplay）

- 关卡呈现：`BoardView`（墙/地板/目标/箱子 + 箱子注册表）、`PlayerView`（身体+面部）、`GameplayCamera`。
- 风格化：`StylizedScheme` / `StylizedController`（toon 参数 + 调色板 + 天空盒 + 后处理，按角色 `MaterialPropertyBlock` 着色）、`Palette`、`PrimitiveFactory`。
- 音频：`AudioManager`（SFX + 预留 BGM）。
- UI：`UiFactory`、`MainMenuController`、`LevelSelectController`、`GameplayHud`。

#### 游戏系统（Gameplay）

- 流程：`GameFlow`（FSM：Boot / MainMenu / LevelSelect / Gameplay，常驻 Persistent，异步加卸载叠加场景）、`TransitionOverlay`（过渡遮罩）。
- 输入：`InputReader`（Input Action Asset、Gameplay/UI map）、`DirectionInput`（2D 向量 → Direction）。
- 存档：`SaveManager`（persistentDataPath/save.json，记录胜利/进度、解锁、清除）。
- 关卡访问：`ILevelRepository`、`LevelList`（ScriptableObject）、`LevelListRepository`。
- 对局驱动：`GameplayBootstrap`（持有 `GameSession`，接线输入/HUD/存档，长按连发，Restart）、`MoveAnimator`、`MoveAnimationConfig`、`BlockFeedbackConfig`、`BoardGeometry`。

#### 底层逻辑（Core）

- 数据模型：`LevelData`（name + XSB map）、`LevelJson`；`GridPosition`、`Direction`、`CellType`、`XsbParser`。
- 规则与状态：`GameState`（LevelData 引用 + 玩家位置/箱子集合/朝向/Moves/Pushes）、`SokobanRules.TryMove`（原子移动，返回 `MoveResult` + `MoveDelta`）、`LevelValidator`（静态校验 + BFS 可达性）。
- 撤销/重做：`MoveDelta`、`ICommand` / `MoveCommand`、`UndoSystem`（命令双栈，Undo/Redo 返回 `MoveDelta` 供表现层动画）。
- 会话聚合：`GameSession`（持有 `GameState` + `UndoSystem`，提供 TryMove / Undo / Redo / Reset / ToSnapshot / Restore / TryRestore）。
- 序列化 DTO：`GridPositionDto`、`MoveDeltaDto`、`RunSnapshot`。
- 存档模型：`SaveData` / `LevelSaveData` + `SaveJson`。

#### 数据

- 关卡内容：`Assets/Data/Levels/Sokoban_LevelNN.json`（XSB + JSON），由编辑器产出。
- 关卡列表：`Assets/Data/LevelList.asset`（ScriptableObject，有序 `List<TextAsset>` 引用）。
- 玩家存档：`Application.persistentDataPath/save.json`（`SaveData`：每关 id / 完成 / 最佳步数推数 / 进行中快照）。

#### 编辑器（Editor）

- `LevelEditorWindow`（列表页 ↔ 内容页）、`LevelGrid`（Painter2D 分层笔刷）、`LevelListStore`（创建/命名/写 JSON/回收站删除）、`LevelListInspector`（只读）。

#### 场景结构

| 场景 | 加载方式 | 职责 |
| --- | --- | --- |
| `Assets/Scenes/Persistent.unity` | 启动（Build index 0，唯一 active） | GameFlow、EventSystem、InputReader、SaveManager、AudioManager、TransitionOverlay、相机 + 方向光 + PostProcessLayer |
| `Assets/Scenes/MainMenu.unity` | Additive | 主菜单 |
| `Assets/Scenes/LevelSelect.unity` | Additive | 选关 |
| `Assets/Scenes/Gameplay.unity` | Additive | 通用对局场景，按 LevelData 布设 |

Build Settings 含全部 4 个场景（Persistent 启动，其余运行时 Additive 加载）。

#### 运行时数据流

- 关卡：编辑器产出 JSON → `LevelList.asset` 引用 → `GameFlow.LevelList` → `LevelListRepository` → `LevelData` → `GameSession` / `BoardView`。
- 存档：`GameSession.ToSnapshot()` → `RunSnapshot`（动态状态 + undo/redo 栈）→ `SaveData` → `save.json`；续玩走 `GameSession.TryRestore`。
- 输入：`InputReader` → `DirectionInput` → `GameplayBootstrap.TryMove` → `GameSession.TryMove` → `MoveAnimator`。

#### 关键设计决策

- 网格：XZ 平面、原点居中、cell=1；`GridPosition`（x 列，y 行），row 0 → +Z。
- 相机：透视、俯角 75°（近正交）、按关卡包围盒自适应取景。
- 移动/动画：Domain 瞬时原子；表现层按 `MoveDelta` 动画；推箱锁步共享 pushCurve；移动后极短转向；动画期间锁输入（无缓冲）。
- 撤销/重做：命令模式双栈，命令存一步差分；Moves/Pushes 随 undo/redo 增减。
- 输入：新 Input System（Legacy + New 并存）；Move 用 2D 向量（死区 + 主轴 + 垂直优先 + Y 正 = Up）；长按连发（动画锁优先）。
- 存档续玩：退出时 `moves>0` 存快照、`moves==0` 清进度；胜利更新最佳（取更小）；选关弹「继续 / 重开 / 取消」。
- 渲染：BIRP 手写 toon Surface Shader + 径向渐变天空盒；后处理 Bloom + Vignette + ACES；shader 已入 Always Included Shaders 防打包裁剪。
- 场景过渡：Persistent 顶层全屏遮罩，淡入 → 切换 → 淡出。