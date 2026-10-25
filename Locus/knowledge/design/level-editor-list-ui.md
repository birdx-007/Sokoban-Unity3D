---
id: kd_38260c7f-90af-45cb-9d31-920513ad6b72
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 关卡列表视觉样式（Level Editor 列表页）

> 记录关卡列表（关卡项 + 列表整体）从初版到当前的视觉优化思路与最终方案。只涉及列表页，不涉及关卡内容编辑页。

## 1. 目标

让关卡列表具备清晰的分层视觉：每个关卡项有独立实线边框、项与项及项与列表边界之间有均匀 5px 间距、选中高亮严格落在项的边框内、拖拽重排后高亮跟随被拖动项。

## 2. 演进过程

### 2.1 初版
- 直接使用 `ListView`，每项为一个 `Label` 文本，无边框、无间距，条目紧凑。

### 2.2 增加边框
- 列表整体：`ListView` 加 1px 实线边框。
- 每项：内层 `Label` 加 1px 实线边框，并增加内边距；`fixedItemHeight` 34。

### 2.3 增加 5px 间距
- 边界间距：`ListView.style.paddingLeft/Right/Top = 5`。
- 项间距：条目结构改为 `wrapper + 内层 Label`：
  - `wrapper` 作为 ListView 的 item 元素，`paddingBottom = 5`；
  - 内层 `Label` 用 `flexGrow = 1` 填充剩余空间；
  - `fixedItemHeight` 34 → 39（= 内容 34 + 底部间距 5）。
- 关键结论：UI Toolkit `ListView` 中 item 的 `margin` 不生效（item 仍按 `fixedItemHeight` 紧贴排列），但 `padding` 生效；因此用「列表 padding 做边界间距 + item 内部 padding 做项间距」。

### 2.4 选中高亮严格落在边框内
- 问题：ListView 自带选中高亮作用在外层 item（wrapper）上，而边框在内层 Label，wrapper 底部多 5px，高亮从边框底部溢出。
- 方案：
  - `wrapper` 设透明背景（内联样式优先级高于 USS），屏蔽 ListView 自带 item 高亮；
  - 在 `BindListItem` 中手动给内层 `Label` 上色：选中 = 半透明蓝底 + 蓝框；未选中 = 透明底 + 灰框；
  - `selectionChanged` 时 `RefreshItems()` 重绑，保证手动高亮实时刷新。

### 2.5 拖拽重排后高亮跟随
- 问题：拖拽重排后 `selectedIndex` 不随被拖动项自动更新，高亮停留在旧索引。
- 方案：改为「按条目对象追踪选中」：
  - `selectionChanged` 时记录 `selectedEntry`；
  - `itemIndexChanged` 里用 `entries.IndexOf(selectedEntry)` 找新索引并写回 `selectedIndex`，再 `RefreshItems()`。

## 3. 最终结构（每项）

```
ListView（1px 边框 + padding L/R/T 5）
└─ wrapper（paddingBottom 5，透明背景，屏蔽自带高亮）
   └─ Label（1px 边框 + 内边距 + flexGrow=1，手动绘制选中态）
```

## 4. 当前配色

- 边框灰：`(0.35, 0.37, 0.42)`
- 选中边框蓝：`(0.45, 0.68, 0.95)`
- 选中背景半透明蓝：`(0.25, 0.45, 0.75, 0.35)`

## 5. 可复用技术要点

- ListView item 的 `margin` 不生效，用 `padding`（在 `fixedItemHeight` 内）实现项间距。
- 内联样式可覆盖 USS 的 `:selected` / `:hover` 背景，用于屏蔽 ListView 自带高亮。
- 选中高亮要画在「带边框的元素」上，避免高亮与边框错位溢出。
- 拖拽重排后的选中跟随，用「对象引用 + `IndexOf`」比「索引偏移」更稳，且能同时处理拖选中项 / 拖非选中项两种情况。
