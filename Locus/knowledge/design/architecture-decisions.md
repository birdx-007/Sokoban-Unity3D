---
id: kd_fcefdbb7-d609-4f7c-8cd3-e5061be815aa
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 架构决策（补充 README）

> 本文件记录 README「框架设计」之外的实现层决策。基础架构（四层分层、关卡数据格式、关卡编辑器）以 README.md 为准，不在此重复。

## 1. 程序集与命名空间

采用 asmdef 强制分层边界。依赖方向自上而下：Gameplay → Core，Editor → (Core, Gameplay)，Tests → (Core, Gameplay)。

| asmdef | 程序集引用 | 命名空间 | 承载内容 |
| --- | --- | --- | --- |
| `BoxPushing.Core` | 仅 UnityEngine.JsonUtility（noEngineReferences=false） | `BoxPushing.Core` | Domain 纯 C# 逻辑 + 数据模型与 JSON 编解码（`LevelData`、`SaveData`） |
| `BoxPushing.Gameplay` | Core | `BoxPushing.Gameplay` | Application + Presentation（流程状态机、输入、存档、关卡布设、BoardView、UI、相机） |
| `BoxPushing.Editor` | Core, Gameplay | `BoxPushing.Editor` | 关卡编辑器（UI Toolkit EditorWindow）、`LevelRepository`（AssetDatabase 读写） |
| `BoxPushing.Tests.EditMode` | Core, Gameplay, Test Framework | `BoxPushing.Tests` | 规则 / 验证 / 数据单元测试 |
| `BoxPushing.Tests.PlayMode` | Core, Gameplay, Test Framework | `BoxPushing.Tests` | 流程集成测试 |

说明：

- Core 为纯 C# 逻辑（不使用 MonoBehaviour/GameObject/Transform/Time 等），仅 JSON 编解码借用 UnityEngine.JsonUtility（内置、零额外依赖，schema 极简够用）；若日后需严格零 UnityEngine 引用或更强 schema，再评估换 Newtonsoft。
- 文件读写适配器按环境拆分——编辑器用 AssetDatabase（Editor 程序集），运行时读 `LevelList.asset` 引用的 TextAsset（Gameplay 程序集）。
- 测试 asmdef 需勾选 Test Assemblies，并引用 UnityEditor/UnityEngine TestRunner。

## 2. 场景结构（Persistent + 叠加）

Build Settings 包含 `Persistent`（index 0，启动且唯一 active 场景）以及 `MainMenu`、`LevelSelect`、`Gameplay`（供 Additive 运行时加载）；启动时仅 `Persistent` 为 active，其余按需加载/卸载。

| 场景 | 加载方式 | 职责 |
| --- | --- | --- |
| `Assets/Scenes/Persistent.unity` | 启动（Build index 0） | 常驻根：GameBootstrap / FlowStateMachine、AudioManager、InputReader、SaveManager、通用 UI 根 |
| `Assets/Scenes/MainMenu.unity` | Additive | 主菜单 |
| `Assets/Scenes/LevelSelect.unity` | Additive | 选关 |
| `Assets/Scenes/Gameplay.unity` | Additive | 通用对局场景；关卡为数据，按 LevelData 布设；相机 rig 在此按关卡边界取景 |

启动流：`Persistent` 启动后加载 `MainMenu` → 切换 `LevelSelect` → 加载 `Gameplay`；返回时卸载当前叠加场景。

## 3. 输入（多平台 / 设备）

- 采用新 Input System 包：`com.unity.inputsystem`（1.11.2）。
- `activeInputHandler = 2`（Both：Legacy Input Manager 与新 Input System 并存）。
- Input Action Asset 拟存放于 `Assets/Settings/Input/InputActions.inputactions`。
- Action Maps：
  - `Gameplay`：Move（四方向）、Undo、Redo、Restart、Pause。
  - `UI`：导航 / 提交 / 取消。
- Control Schemes：Keyboard&Mouse、Gamepad、Touch；触屏端使用屏上方向键 + 按钮（四向离散移动即可，无需自由摇杆）。
- 后续可扩展按键重绑定 UI。
