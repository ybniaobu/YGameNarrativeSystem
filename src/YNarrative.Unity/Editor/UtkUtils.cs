using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace YNarrative.Editor
{
    /// <summary>
    /// UI Toolkit 扩展方法。
    /// </summary>
    internal static class UtkEditorExtensions
    {
        /// <summary>
        /// 获取指定名称的元素，若不存在则抛异常。
        /// </summary>
        public static T Require<T>(this VisualElement root, string elementName) where T : VisualElement
        {
            var element = root.Q<T>(elementName);
            if (element == null)
                throw new System.InvalidOperationException($"[UtkEditorExtensions] UXML 里缺少元素：{elementName}");
            return element;
        }
    }
    
    /// <summary>
    /// UI Toolkit 工具类，提供一些常用方法。
    /// </summary>
    internal static class UtkUtils
    {
        /// <summary>
        /// 弹出文件夹选择对话框，返回相对路径（Assets\… 或 Packages\…），若取消则返回空字符串。
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="folder">默认的文件夹路径，支持 <c>/</c> 或 <c>\\</c></param>
        /// <returns>选择的文件夹的相对路径（输出 Unity 风格分隔符 <c>/</c>），若取消则返回空字符串</returns>
        public static string BrowseFolder(string title, string folder)
        {
            var absolute = PathUtils.ToFullPath(folder);
            var start = Directory.Exists(absolute) ? absolute : string.Empty;
            var picked = EditorUtility.OpenFolderPanel(title, start, string.Empty);
            return PathUtils.ToRelativePath(picked).Replace('\\', '/');
        }
    }
}