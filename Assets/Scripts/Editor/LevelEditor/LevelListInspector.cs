using BoxPushing.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoxPushing.Editor
{
    /// <summary>
    /// Read-only inspector for <see cref="LevelList"/>. The list must be maintained through the
    /// Level Editor (Sokoban/Level/LevelEditor) so the inspector only shows the current order.
    /// </summary>
    [CustomEditor(typeof(LevelList))]
    public class LevelListInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;

            root.Add(new Label("关卡列表由关卡编辑器统一维护。")
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.95f, 0.75f, 0.3f),
                    marginBottom = 8
                }
            });

            root.Add(new Label("请通过 Sokoban/Level/LevelEditor 维护")
            {
                style = { whiteSpace = WhiteSpace.Normal, marginBottom = 10 }
            });

            LevelList list = (LevelList)target;
            var listView = new ListView { reorderable = false, selectionType = SelectionType.None };
            listView.makeItem = () => new Label();
            listView.bindItem = (element, index) =>
            {
                var label = (Label)element;
                TextAsset asset = list.GetAt(index);
                label.text = asset != null ? asset.name : "<null>";
            };
            listView.itemsSource = list.Levels;
            listView.fixedItemHeight = 22;
            listView.style.flexGrow = 1;

            root.Add(listView);
            return root;
        }
    }
}
