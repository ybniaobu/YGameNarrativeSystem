using System;
using System.IO;
using UnityEngine;

namespace YNarrative.Editor
{
    internal static class PathUtils
    {
        /// <summary>
        /// 工程根目录，绝对路径，分隔符为当前平台风格（Windows 下是 <c>\</c>, Mac/Linux 下是 <c>/</c>）
        /// </summary>
        public static string ProjectRoot { get; } = Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// 工程相对路径 Assets/… 或 Packages/… 转绝对路径。若 Packages 在项目外，会重定向到外部地址（Unity 编辑器对路径的解析行为）。
        /// </summary>
        /// <param name="path">支持 <c>/</c> 或 <c>\\</c></param>
        /// <returns>分隔符为当前平台风格（Windows 下是 <c>\</c>, Mac/Linux 下是 <c>/</c>）</returns>
        public static string ToFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// 绝对路径转工程相对路径
        /// </summary>
        /// <param name="path">支持 <c>/</c> 或 <c>\\</c></param>
        /// <returns>工程相对路径（分隔符为当前平台风格，Windows 下是 <c>\</c>, Mac/Linux 下是 <c>/</c>）</returns>
        public static string ToRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return Path.GetRelativePath(ProjectRoot, path);
        }
        
        /// <summary>
        /// 判断路径是否在工程目录下
        /// </summary>
        /// <param name="path">相对路径或绝对路径，支持 <c>/</c> 或 <c>\\</c></param>
        /// <returns>是否在工程目录下</returns>
        public static bool IsInProject(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string relative = ToRelativePath(Path.GetFullPath(path));
            return !relative.StartsWith("..") && !Path.IsPathRooted(relative);
        }
    }
}