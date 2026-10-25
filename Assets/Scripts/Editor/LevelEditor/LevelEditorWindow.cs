using System;
using System.Collections.Generic;
using System.Linq;
using BoxPushing.Core;
using BoxPushing.Gameplay;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoxPushing.Editor
{
    /// <summary>
    /// Level Editor: a single UI Toolkit window with a list page (add/delete/reorder with
    /// pending save/rollback) and a content page (edit name and paint the grid).
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        [MenuItem("Sokoban/Level/LevelEditor")]
        public static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("关卡编辑器");
            window.minSize = new Vector2(520, 560);
        }

        // ---- Working state ----

        private LevelList list;
        private readonly List<LevelEntry> entries = new List<LevelEntry>();
        private List<string> committedFileNames = new List<string>();
        private Dictionary<string, string> committedPathByName = new Dictionary<string, string>();

        // List page UI
        private VisualElement listPage;
        private VisualElement contentPage;
        private ListView listView;
        private Label pendingLabel;
        private Button deleteButton;
        private Button saveButton;
        private Button rollbackButton;
        private Button enterEditButton;

        // Content page UI
        private TextField nameField;
        private LevelGrid grid;
        private Label contentStatusLabel;
        private IntegerField widthField;
        private IntegerField heightField;
        private readonly List<Button> brushButtons = new List<Button>();
        private BrushKind brushKind = BrushKind.Wall;
        private int editingIndex = -1;
        private bool contentDirty;
        private LevelEntry selectedEntry;

        private static readonly Color ListBorderColor = new Color(0.35f, 0.37f, 0.42f);
        private static readonly Color BrushSelectedBorderColor = new Color(0.95f, 0.78f, 0.25f);
        private static readonly Color ListItemSelectedBorder = new Color(0.45f, 0.68f, 0.95f);
        private static readonly Color ListItemSelectedBackground = new Color(0.25f, 0.45f, 0.75f, 0.35f);

        private static void SetSolidBorder(IStyle style, float width, Color color)
        {
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }

        private bool HasPendingChanges
        {
            get
            {
                if (entries.Any(entry => entry.isNew))
                    return true;

                return !entries.Select(entry => entry.fileName).SequenceEqual(committedFileNames);
            }
        }

        private void CreateGUI()
        {
            list = LevelListStore.EnsureLevelList();
            LoadCommittedEntries();
            SnapshotCommitted();

            BuildListPage();
            BuildContentPage();

            rootVisualElement.Add(listPage);
            rootVisualElement.Add(contentPage);

            ShowListPage();
        }

        // ---- Level entries ----

        private sealed class LevelEntry
        {
            public TextAsset asset;
            public LevelData data;
            public string fileName;
            public bool isNew;
            public string originalPath;
        }

        private void LoadCommittedEntries()
        {
            entries.Clear();
            foreach (TextAsset asset in list.Levels)
            {
                if (asset == null)
                    continue;

                LevelData data = null;
                try
                {
                    data = LevelJson.FromJson(asset.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"LevelEditorWindow: failed to parse '{asset.name}': {e}");
                }

                entries.Add(new LevelEntry
                {
                    asset = asset,
                    data = data ?? LevelListStore.CreateDefaultLevel(asset.name),
                    fileName = asset.name,
                    isNew = false,
                    originalPath = AssetDatabase.GetAssetPath(asset)
                });
            }
        }

        private void ReloadEntryFromDisk(int index)
        {
            if (index < 0 || index >= entries.Count)
                return;

            LevelEntry entry = entries[index];
            if (entry.asset == null)
                return;

            try
            {
                entry.data = LevelJson.FromJson(entry.asset.text) ?? LevelListStore.CreateDefaultLevel(entry.fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"LevelEditorWindow: failed to reload '{entry.fileName}': {e}");
            }
        }

        private void SnapshotCommitted()
        {
            committedFileNames = entries.Select(entry => entry.fileName).ToList();
            committedPathByName = entries.ToDictionary(entry => entry.fileName, entry => entry.originalPath);
        }

        // ---- List page ----

        private void BuildListPage()
        {
            listPage = new VisualElement();
            listPage.style.flexGrow = 1;
            listPage.style.paddingLeft = 8;
            listPage.style.paddingRight = 8;
            listPage.style.paddingTop = 8;
            listPage.style.paddingBottom = 8;

            listPage.Add(new Label("关卡列表")
            {
                style = { fontSize = 20, marginBottom = 8 }
            });

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };

            var addButton = new Button(AddLevel) { text = "新增关卡" };
            toolbar.Add(addButton);

            deleteButton = new Button(DeleteLevel) { text = "删除" };
            toolbar.Add(deleteButton);

            saveButton = new Button(OnSaveClicked) { text = "保存" };
            toolbar.Add(saveButton);

            rollbackButton = new Button(Rollback) { text = "回滚" };
            toolbar.Add(rollbackButton);

            enterEditButton = new Button(EnterEdit) { text = "进入关卡编辑" };
            toolbar.Add(enterEditButton);

            listPage.Add(toolbar);

            pendingLabel = new Label { style = { marginBottom = 6, whiteSpace = WhiteSpace.Normal, color = new Color(0.95f, 0.75f, 0.3f) } };
            listPage.Add(pendingLabel);

            listView = new ListView
            {
                reorderable = true,
                selectionType = SelectionType.Single,
                fixedItemHeight = 39,
                style = { flexGrow = 1 }
            };
            SetSolidBorder(listView.style, 1f, ListBorderColor);
            listView.style.paddingLeft = 5;
            listView.style.paddingRight = 5;
            listView.style.paddingTop = 5;
            listView.makeItem = MakeListItem;
            listView.bindItem = BindListItem;
            listView.itemsSource = entries;
            listView.itemIndexChanged += OnItemIndexChanged;
            listView.selectionChanged += _ =>
            {
                UpdateSelectedEntry();
                listView.RefreshItems();
                RefreshListUi();
            };
            listPage.Add(listView);

            listPage.Add(new Label("提示：拖动条目调整顺序；新增/删除/重排后需点击「保存」落盘或「回滚」放弃。")
            {
                style = { marginTop = 6, whiteSpace = WhiteSpace.Normal }
            });

            RefreshListUi();
        }

        private static VisualElement MakeListItem()
        {
            var wrapper = new VisualElement();
            wrapper.style.paddingBottom = 5;
            wrapper.style.backgroundColor = new Color(0f, 0f, 0f, 0f);

            var label = new Label();
            SetSolidBorder(label.style, 1f, ListBorderColor);
            label.style.flexGrow = 1;
            label.style.paddingLeft = 6;
            label.style.paddingRight = 6;
            label.style.paddingTop = 5;
            label.style.paddingBottom = 5;

            wrapper.Add(label);
            return wrapper;
        }

        private void BindListItem(VisualElement element, int index)
        {
            var label = element.Q<Label>();
            if (label == null)
                return;

            LevelEntry entry = entries[index];
            string displayName = entry.data != null && !string.IsNullOrEmpty(entry.data.name) ? entry.data.name : entry.fileName;
            string marker = entry.isNew ? "（新）" : string.Empty;
            label.text = $"{index + 1}. {entry.fileName} · {displayName}{marker}";

            bool selected = listView != null && listView.selectedIndex == index;
            SetSolidBorder(label.style, 1f, selected ? ListItemSelectedBorder : ListBorderColor);
            label.style.backgroundColor = selected ? ListItemSelectedBackground : new Color(0f, 0f, 0f, 0f);
        }

        private void UpdateSelectedEntry()
        {
            int index = listView != null ? listView.selectedIndex : -1;
            selectedEntry = index >= 0 && index < entries.Count ? entries[index] : null;
        }

        private void OnItemIndexChanged(int from, int to)
        {
            // Keep the selection on the same level entry after a drag reorder.
            if (selectedEntry != null)
            {
                int newIndex = entries.IndexOf(selectedEntry);
                if (newIndex >= 0)
                    listView.selectedIndex = newIndex;
            }

            listView.RefreshItems();
            RefreshListUi();
        }

        private void RefreshListUi()
        {
            bool pending = HasPendingChanges;
            bool hasSelection = listView != null && listView.selectedIndex >= 0;

            saveButton.style.display = pending ? DisplayStyle.Flex : DisplayStyle.None;
            rollbackButton.style.display = pending ? DisplayStyle.Flex : DisplayStyle.None;
            pendingLabel.text = pending ? "有未保存的关卡列表改动（此状态下无法进入关卡编辑）" : string.Empty;

            deleteButton.SetEnabled(hasSelection);
            enterEditButton.SetEnabled(hasSelection && !pending);
        }

        private void AddLevel()
        {
            string fileName = LevelListStore.NextLevelFileName(entries.Select(entry => entry.fileName));
            entries.Add(new LevelEntry
            {
                asset = null,
                data = LevelListStore.CreateDefaultLevel(fileName),
                fileName = fileName,
                isNew = true,
                originalPath = null
            });

            listView.selectedIndex = entries.Count - 1;
            listView.RefreshItems();
            RefreshListUi();
        }

        private void DeleteLevel()
        {
            int index = listView != null ? listView.selectedIndex : -1;
            if (index < 0 || index >= entries.Count)
                return;

            LevelEntry entry = entries[index];
            ConfirmDialog.Open(
                "删除关卡",
                $"确定要删除关卡「{entry.fileName}」？\n\n保存时该关卡文件（含 .meta）将被移入操作系统回收站。",
                showCancel: true,
                confirmed =>
                {
                    if (!confirmed)
                        return;

                    entries.RemoveAt(index);
                    listView.selectedIndex = -1;
                    listView.RefreshItems();
                    RefreshListUi();
                },
                parent: this);
        }

        private void OnSaveClicked()
        {
            if (!HasPendingChanges)
                return;

            ConfirmDialog.Open(
                "保存关卡列表",
                "将应用以下改动：\n\n" + DescribePendingChanges() + "\n\n确认后这些改动才会真正落盘。",
                showCancel: true,
                confirmed =>
                {
                    if (confirmed)
                        ApplySave();
                },
                parent: this);
        }

        private string DescribePendingChanges()
        {
            var changes = new List<string>();
            var committedSet = new HashSet<string>(committedFileNames);
            var workingSet = new HashSet<string>(entries.Select(entry => entry.fileName));

            foreach (LevelEntry entry in entries)
            {
                if (entry.isNew)
                    changes.Add($"新增关卡：{entry.fileName}");
            }

            foreach (string name in committedFileNames)
            {
                if (!workingSet.Contains(name))
                    changes.Add($"删除关卡：{name}");
            }

            List<string> commonCommitted = committedFileNames.Where(workingSet.Contains).ToList();
            List<string> commonWorking = entries
                .Where(entry => committedSet.Contains(entry.fileName))
                .Select(entry => entry.fileName)
                .ToList();

            if (!commonCommitted.SequenceEqual(commonWorking))
                changes.Add("调整关卡顺序");

            return changes.Count == 0 ? "（无改动）" : string.Join("\n", changes);
        }

        private void ApplySave()
        {
            // 1. Persist newly added levels to JSON files.
            foreach (LevelEntry entry in entries)
            {
                if (!entry.isNew)
                    continue;

                TextAsset asset = LevelListStore.WriteLevelJson(entry.fileName, entry.data);
                entry.asset = asset;
                entry.isNew = false;
                entry.originalPath = AssetDatabase.GetAssetPath(asset);
            }

            // 2. Write back the ordered LevelList.
            LevelListStore.PersistLevelList(list, entries.Select(entry => entry.asset));
            AssetDatabase.SaveAssets();

            // 3. Move removed levels (json + meta) to the recycle bin.
            var workingSet = new HashSet<string>(entries.Select(entry => entry.fileName));
            foreach (string name in committedFileNames)
            {
                if (workingSet.Contains(name))
                    continue;

                if (committedPathByName.TryGetValue(name, out string path))
                {
                    LevelListStore.DeleteFileToRecycleBin(path);
                    LevelListStore.DeleteFileToRecycleBin(path + ".meta");
                }
            }

            AssetDatabase.Refresh();
            SnapshotCommitted();
            listView.RefreshItems();
            RefreshListUi();
        }

        private void Rollback()
        {
            if (!HasPendingChanges)
                return;

            ConfirmDialog.Open(
                "回滚改动",
                "确定要丢弃本轮所有未保存的关卡列表改动？",
                showCancel: true,
                confirmed =>
                {
                    if (!confirmed)
                        return;

                    LoadCommittedEntries();
                    SnapshotCommitted();
                    listView.selectedIndex = -1;
                    listView.RefreshItems();
                    RefreshListUi();
                },
                parent: this);
        }

        private void EnterEdit()
        {
            int index = listView != null ? listView.selectedIndex : -1;
            if (index < 0 || index >= entries.Count || HasPendingChanges)
                return;

            OpenContentPage(index);
        }

        // ---- Content page ----

        private void BuildContentPage()
        {
            contentPage = new VisualElement();
            contentPage.style.flexGrow = 1;
            contentPage.style.paddingLeft = 8;
            contentPage.style.paddingRight = 8;
            contentPage.style.paddingTop = 8;
            contentPage.style.paddingBottom = 8;

            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
            topRow.Add(new Button(GoBackToList) { text = "← 返回列表" });

            nameField = new TextField("关卡名称");
            nameField.style.flexGrow = 1;
            nameField.style.marginLeft = 8;
            nameField.style.marginRight = 8;
            nameField.RegisterValueChangedCallback(_ => contentDirty = true);
            topRow.Add(nameField);

            topRow.Add(new Button(SaveLevel) { text = "保存关卡" });
            contentPage.Add(topRow);

            var sizeRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 }
            };
            sizeRow.Add(new Label("尺寸"));

            widthField = new IntegerField { value = 8 };
            widthField.style.width = 60;
            widthField.style.marginLeft = 8;
            sizeRow.Add(widthField);

            sizeRow.Add(new Label("×") { style = { marginLeft = 4, marginRight = 4 } });

            heightField = new IntegerField { value = 8 };
            heightField.style.width = 60;
            sizeRow.Add(heightField);

            var applySizeButton = new Button(ApplySize) { text = "应用尺寸" };
            applySizeButton.style.marginLeft = 8;
            sizeRow.Add(applySizeButton);

            sizeRow.Add(new Label("保留左上区域，新增行列以地板填充")
            {
                style = { marginLeft = 10, whiteSpace = WhiteSpace.Normal, flexGrow = 1 }
            });
            contentPage.Add(sizeRow);

            var brushRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 8 }
            };
            AddBrushButton(brushRow, BrushKind.Wall);
            AddBrushButton(brushRow, BrushKind.Floor);
            AddBrushButton(brushRow, BrushKind.Goal);
            AddBrushButton(brushRow, BrushKind.Player);
            AddBrushButton(brushRow, BrushKind.Box);
            contentPage.Add(brushRow);

            contentPage.Add(new Label("左键：绘制当前笔刷；右键：擦除（按当前笔刷层）；按住拖动可连续绘制/擦除。")
            {
                style = { marginBottom = 8, whiteSpace = WhiteSpace.Normal }
            });

            var scroll = new ScrollView { style = { flexGrow = 1 } };
            grid = new LevelGrid();
            grid.Changed += () => contentDirty = true;
            scroll.Add(grid);
            contentPage.Add(scroll);

            contentStatusLabel = new Label { style = { marginTop = 6, whiteSpace = WhiteSpace.Normal } };
            contentPage.Add(contentStatusLabel);

            SelectBrush(BrushKind.Wall);
        }

        private void AddBrushButton(VisualElement row, BrushKind kind)
        {
            var button = new Button(() => SelectBrush(kind));
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 10;
            button.style.paddingTop = 3;
            button.style.paddingBottom = 3;
            button.style.marginRight = 6;
            SetSolidBorder(button.style, 1f, ListBorderColor);

            button.Add(CreateBrushSwatch(kind));

            var label = new Label(BrushName(kind));
            label.style.marginLeft = 6;
            button.Add(label);

            button.userData = kind;
            brushButtons.Add(button);
            row.Add(button);
        }

        private void SelectBrush(BrushKind kind)
        {
            brushKind = kind;
            if (grid != null)
                grid.Brush = kind;

            foreach (Button button in brushButtons)
            {
                bool selected = (BrushKind)button.userData == kind;
                SetSolidBorder(
                    button.style,
                    selected ? 2f : 1f,
                    selected ? BrushSelectedBorderColor : ListBorderColor);
            }
        }

        private static VisualElement CreateBrushSwatch(BrushKind kind)
        {
            var swatch = new VisualElement();
            swatch.style.width = 14;
            swatch.style.height = 14;
            swatch.style.flexShrink = 0;
            swatch.style.backgroundColor = LevelGrid.GetBrushColor(kind);

            if (kind == BrushKind.Player)
            {
                swatch.style.borderTopLeftRadius = 7;
                swatch.style.borderTopRightRadius = 7;
                swatch.style.borderBottomLeftRadius = 7;
                swatch.style.borderBottomRightRadius = 7;
            }

            SetSolidBorder(swatch.style, 1f, new Color(0f, 0f, 0f, 0.4f));
            return swatch;
        }

        private static string BrushName(BrushKind kind)
        {
            switch (kind)
            {
                case BrushKind.Wall:
                    return "墙";
                case BrushKind.Floor:
                    return "地板";
                case BrushKind.Goal:
                    return "目标";
                case BrushKind.Player:
                    return "玩家";
                case BrushKind.Box:
                    return "箱子";
                default:
                    return kind.ToString();
            }
        }

        private void OpenContentPage(int index)
        {
            editingIndex = index;
            LevelEntry entry = entries[index];

            nameField.SetValueWithoutNotify(entry.data != null ? entry.data.name : string.Empty);
            grid.Level = entry.data;
            RefreshSizeFields();
            SelectBrush(brushKind);
            contentDirty = false;
            contentStatusLabel.text = string.Empty;

            ShowContentPage();
        }

        private void GoBackToList()
        {
            if (contentDirty && editingIndex >= 0)
            {
                int index = editingIndex;
                ConfirmDialog.Open(
                    "放弃未保存修改",
                    "当前关卡有未保存的修改，确定放弃并返回列表？",
                    showCancel: true,
                    confirmed =>
                    {
                        if (!confirmed)
                            return;

                        ReloadEntryFromDisk(index);
                        ShowListPage();
                        RefreshListUi();
                    },
                    parent: this);
                return;
            }

            ShowListPage();
            RefreshListUi();
        }

        private void SaveLevel()
        {
            if (editingIndex < 0 || editingIndex >= entries.Count)
                return;

            LevelEntry entry = entries[editingIndex];
            entry.data.name = nameField.value != null ? nameField.value.Trim() : string.Empty;

            IReadOnlyList<string> errors = LevelValidator.GetErrors(entry.data);
            if (errors.Count > 0)
            {
                ConfirmDialog.Open(
                    "关卡校验未通过",
                    "无法保存，请先修复以下问题：\n\n" + string.Join("\n", errors),
                    showCancel: false,
                    null,
                    parent: this);
                return;
            }

            TextAsset asset = LevelListStore.WriteLevelJson(entry.fileName, entry.data);
            if (asset == null)
            {
                ConfirmDialog.Open("保存失败", $"无法写入关卡文件：{entry.fileName}.json", showCancel: false, null, parent: this);
                return;
            }

            entry.asset = asset;
            entry.originalPath = AssetDatabase.GetAssetPath(asset);
            LevelListStore.PersistLevelList(list, entries.Select(item => item.asset));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            contentDirty = false;
            contentStatusLabel.text = $"已保存 {entry.fileName}";
        }

        private void ApplySize()
        {
            if (editingIndex < 0 || editingIndex >= entries.Count)
                return;

            LevelEntry entry = entries[editingIndex];
            if (entry.data == null)
                return;

            int width = widthField.value;
            int height = heightField.value;

            if (width <= 0 || height <= 0)
            {
                ConfirmDialog.Open("尺寸无效", "宽和高必须为正整数。", showCancel: false, null, parent: this);
                return;
            }

            if (width > 100 || height > 100)
            {
                ConfirmDialog.Open("尺寸过大", "宽和高暂限制在 1~100 之间。", showCancel: false, null, parent: this);
                return;
            }

            Resize(entry.data, width, height);
            grid.Level = entry.data;
            contentDirty = true;
            contentStatusLabel.text = $"已调整为 {width} × {height}";
            RefreshSizeFields();
        }

        private static void Resize(LevelData level, int width, int height)
        {
            var map = new string[height];
            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                {
                    char cell = XsbParser.Floor;
                    if (level.TryGetCell(x, y, out char existing))
                        cell = existing;
                    row[x] = cell;
                }
                map[y] = new string(row);
            }

            level.map = map;
        }

        private void RefreshSizeFields()
        {
            if (editingIndex < 0 || editingIndex >= entries.Count)
                return;

            LevelEntry entry = entries[editingIndex];
            widthField.SetValueWithoutNotify(entry.data != null ? entry.data.Width : 0);
            heightField.SetValueWithoutNotify(entry.data != null ? entry.data.Height : 0);
        }

        // ---- Page switching ----

        private void ShowListPage()
        {
            editingIndex = -1;
            listPage.style.display = DisplayStyle.Flex;
            contentPage.style.display = DisplayStyle.None;
        }

        private void ShowContentPage()
        {
            listPage.style.display = DisplayStyle.None;
            contentPage.style.display = DisplayStyle.Flex;
        }

        // ---- Modal dialog (pure UI Toolkit) ----

        private sealed class ConfirmDialog : EditorWindow
        {
            private string bodyText;
            private bool showCancel;
            private Action<bool> onResult;

            public static void Open(string title, string body, bool showCancel, Action<bool> onResult, EditorWindow parent)
            {
                var dialog = CreateInstance<ConfirmDialog>();
                dialog.titleContent = new GUIContent(title);
                dialog.bodyText = body;
                dialog.showCancel = showCancel;
                dialog.onResult = onResult;

                Vector2 size = EstimateSize(body);
                dialog.minSize = size;
                dialog.maxSize = size;

                CenterOnParent(dialog, parent, size);
                dialog.ShowModalUtility();
            }

            private static Vector2 EstimateSize(string body)
            {
                string[] lines = string.IsNullOrEmpty(body) ? new[] { string.Empty } : body.Split('\n');
                int maxLen = 0;
                foreach (string line in lines)
                    maxLen = Math.Max(maxLen, line.Length);

                float width = Mathf.Clamp(160f + maxLen * 12f, 380f, 540f);
                float height = Mathf.Clamp(120f + lines.Length * 20f, 150f, 440f);
                return new Vector2(width, height);
            }

            private static void CenterOnParent(ConfirmDialog dialog, EditorWindow parent, Vector2 size)
            {
                if (parent == null)
                    return;

                Rect parentRect = parent.position;
                dialog.position = new Rect(
                    parentRect.x + (parentRect.width - size.x) * 0.5f,
                    parentRect.y + (parentRect.height - size.y) * 0.5f,
                    size.x,
                    size.y);
            }

            private void CreateGUI()
            {
                var root = rootVisualElement;
                root.style.paddingLeft = 12;
                root.style.paddingRight = 12;
                root.style.paddingTop = 12;
                root.style.paddingBottom = 12;

                var scroll = new ScrollView { style = { flexGrow = 1 } };
                var body = new Label(bodyText ?? string.Empty)
                {
                    style = { whiteSpace = WhiteSpace.Normal }
                };
                scroll.Add(body);
                root.Add(scroll);

                var buttons = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd,
                        marginTop = 12
                    }
                };

                if (showCancel)
                {
                    var cancel = new Button(() => Close(false)) { text = "取消" };
                    buttons.Add(cancel);
                }

                var ok = new Button(() => Close(true)) { text = "确定" };
                buttons.Add(ok);
                root.Add(buttons);
            }

            private void Close(bool value)
            {
                Action<bool> callback = onResult;
                onResult = null;
                callback?.Invoke(value);
                Close();
            }
        }
    }
}
