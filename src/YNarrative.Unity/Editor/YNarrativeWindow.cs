using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace YNarrative.Editor
{
    /// <summary>叙事编辑器主窗口</summary>
    public sealed class YNarrativeWindow : EditorWindow
    {
        [MenuItem("YStudio/Narrative System", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<YNarrativeWindow>();
            window.titleContent = new GUIContent("叙事系统编辑器", LoadIcon());
            window.minSize = new Vector2(960, 540);
        }

        private void CreateGUI()
        {
            rootVisualElement.style.paddingLeft   = 8;
            rootVisualElement.style.paddingRight  = 8;
            rootVisualElement.style.paddingTop    = 8;
            rootVisualElement.style.paddingBottom = 8;

            rootVisualElement.Add(new BakePanel());
        }
        
        private static Texture2D LoadIcon()
        {
            string path = YNarrativePaths.Icon("NarrativeWindow");
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null) Debug.LogWarning($"[YNarrativeWindow] 找不到图标：{path}");
            return icon;
        }
    }
}
