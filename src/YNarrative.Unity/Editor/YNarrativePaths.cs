using System.IO;
using UnityEngine;

namespace YNarrative.Editor
{
    /// <summary>
    /// 对话编辑器工具用到的路径常量。
    /// </summary>
    internal static class YNarrativePaths
    {
        public const string PackageRoot = "Packages/com.ystudio.gameplay.narrative-system";
        public const string DefaultInputRoot = "Assets/Dialogue/Sources";
        public const string DefaultOutputRoot = "Assets/Dialogue/Generated";
        
        public const string EditorRoot = PackageRoot + "/Editor";
        public const string PanelsRoot = EditorRoot + "/Panels";
        public const string IconsRoot = EditorRoot + "/Icons";
        
        public const string RuntimeRoot = PackageRoot + "/Runtime/Dialogue";
        
        public static string Icon(string iconName) => $"{IconsRoot}/{iconName}.png";
        public static string Uxml(string panelName) => $"{PanelsRoot}/{panelName}.uxml";
        public static string Uss(string panelName) => $"{PanelsRoot}/{panelName}.uss";
    }
}