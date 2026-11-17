using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using static YNarrative.Editor.YNarrativePaths;

namespace YNarrative.Editor
{
    /// <summary>烘焙面板：选目录、触发烘焙、显示日志。</summary>
    internal sealed class BakePanel : VisualElement
    {
        private readonly TextField _inputPath;
        private readonly Button _inputBrowse;
        private readonly TextField _outputPath;
        private readonly Button _outputBrowse;
        
        private readonly Toggle _clearOutput;

        private readonly Button _readButton;
        private readonly Button _validateButton;
        private readonly Button _bakeButton;
        private readonly Button _revealButton;
        
        private readonly ScrollView _log;
        private readonly Label _logText;
        private readonly Label _status;

        private readonly List<string> _lines = new List<string>();

        private YNarrativeSource _source;

        public BakePanel()
        {
            // ---------- 设置面板 ----------
            
            style.flexGrow = 1;

            // ---------- 加载 UXML / USS ----------
            
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Uxml("BakePanel"));
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(Uss("BakePanel"));
            
            if (uxml == null)
                throw new System.InvalidOperationException($"[BakePanel] 找不到 UXML：{Uxml("BakePanel")}");
            
            if (uss == null)
                throw new System.InvalidOperationException($"[BakePanel] 找不到 USS：{Uss("BakePanel")}");
            
            uxml.CloneTree(this);
            styleSheets.Add(uss);
            
            // ---------- 获取元素 ----------
            
            _inputPath = this.Require<TextField>("input-path");
            _inputBrowse = this.Require<Button>("input-browse");
            _outputPath = this.Require<TextField>("output-path");
            _outputBrowse = this.Require<Button>("output-browse");
            _clearOutput = this.Require<Toggle>("clear-output");
            _readButton = this.Require<Button>("read-button");
            _validateButton = this.Require<Button>("validate-button");
            _bakeButton = this.Require<Button>("bake-button");
            _revealButton = this.Require<Button>("reveal-button");
            _log = this.Require<ScrollView>("log");
            _logText = this.Require<Label>("log-text");
            _status = this.Require<Label>("status");

            _logText.selection.isSelectable = true; // 允许鼠标选中文本并 Ctrl+C
            
            RestoreSettings(); // 先恢复设置，再注册回调，否则会多触发一次回调

            _inputBrowse.clicked += OnBrowseInput;
            _outputBrowse.clicked += OnBrowseOutput;
            _readButton.clicked += OnReadClicked;
            _validateButton.clicked += OnValidateClicked;
            _bakeButton.clicked += OnBakeClicked;
            _revealButton.clicked += OnRevealOutput;
            
            _inputPath.RegisterValueChangedCallback(evt => YNarrativeSetting.instance.InputPath = evt.newValue);
            _outputPath.RegisterValueChangedCallback(evt => YNarrativeSetting.instance.OutputPath = evt.newValue);
            _clearOutput.RegisterValueChangedCallback(evt => YNarrativeSetting.instance.ClearOutput = evt.newValue);
            
            // ---------- 初始化日志 ----------
            
            AddLog("就绪。");
        }

        // ----------------------------------------------------------------------------------------------------
        // 事件
        // ----------------------------------------------------------------------------------------------------

        private void OnBrowseInput()
        {
            var picked = UtkUtils.BrowseFolder("选择输入目录（CSV 根目录）", _inputPath.value);
            if (string.IsNullOrEmpty(picked)) return;
            _inputPath.value = picked; // 回调写入设置
        }

        private void OnBrowseOutput()
        {
            var picked = UtkUtils.BrowseFolder("选择输出目录", _outputPath.value);
            if (string.IsNullOrEmpty(picked)) return;
            _outputPath.value = picked; // 回调写入设置
        }

        private void OnRevealOutput()
        {
            var absolute = PathUtils.ToFullPath(_outputPath.value);
            if (!Directory.Exists(absolute))
            {
                AddLog($"输出目录不存在：{_outputPath.value}");
                return;
            }
            EditorUtility.RevealInFinder(absolute);
        }

        private void OnReadClicked()
        {
            if (!TryReadSource()) return;
            SetStatus($"读取完成：{_source.chapters.Count} 章");
        }

        private void OnValidateClicked()
        {
            if (!TryReadSource()) return;

            YNarrativeValidateResult check = YNarrativeValidator.Validate(_source);

            foreach (string warning in check.warnings) AddLog(warning, LogLevel.Warning);
            foreach (string error in check.errors) AddLog(error, LogLevel.Error);

            if (check.Succeeded)
            {
                AddLog($"校验通过（警告 {check.warnings.Count} 条）", LogLevel.Success);
                SetStatus($"校验通过（警告 {check.warnings.Count} 条）");
            }
            else
            {
                AddLog($"校验失败：{check.errors.Count} 个错误", LogLevel.Error);
                SetStatus($"校验失败：{check.errors.Count} 个错误");
            }
        }

        /// <summary>读取输入目录到 _source，失败时把错误写进日志。</summary>
        private bool TryReadSource()
        {
            string root = PathUtils.ToFullPath(_inputPath.value);

            if (!Directory.Exists(root))
            {
                AddLog($"输入目录不存在：{_inputPath.value}", LogLevel.Error);
                SetStatus("读取失败");
                return false;
            }

            AddLog($"开始读取：{_inputPath.value}");

            YNarrativeReadResult read = YNarrativeSourceReader.Read(root);

            foreach (string error in read.errors) AddLog(error, LogLevel.Error);

            if (!read.Succeeded)
            {
                SetStatus($"读取失败：{read.errors.Count} 个错误");
                return false;
            }

            _source = read.source;

            int nodes = 0;
            foreach (YNarrativeChapter chapter in _source.chapters) nodes += chapter.nodes.Count;

            AddLog($"读取完成：{_source.chapters.Count} 章 / {_source.speakers.Count} 角色 / {nodes} 节点",
                   LogLevel.Success);
            return true;
        }

        private void OnBakeClicked()
        {
            var input = PathUtils.ToFullPath(_inputPath.value);
            var output = PathUtils.ToFullPath(_outputPath.value);

            if (!Directory.Exists(input))
            {
                AddLog($"输入目录不存在：{_inputPath.value}");
                SetStatus("失败：输入目录无效");
                return;
            }
            
            if (!Directory.Exists(output))
            {
                if (!PathUtils.IsInProject(output))
                {
                    AddLog($"输出目录不存在且不在工程内，无法创建：{_outputPath.value}");
                    SetStatus("失败：输出目录无效");
                    return;
                }
                AddLog($"输出目录不存在，尝试创建：{_outputPath.value}");
                Directory.CreateDirectory(output);
            }

            AddLog($"输入目录：{_inputPath.value}");
            AddLog($"输出目录：{_outputPath.value}");
            AddLog($"生成前清空：{(_clearOutput.value ? "是" : "否")}");
            AddLog("烘焙流程还没实现（阶段 1）");
            SetStatus("就绪（未实现）");
            
            AssetDatabase.Refresh();
        }

        /// ----------------------------------------------------------------------------------------------------
        // 日志 Log / 状态 Status
        // ----------------------------------------------------------------------------------------------------

        private enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success,
        }

        private void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            // Label 开了富文本，消息里的尖括号要转义，否则会被当成标签
            string safe = message.Replace("<", "&lt;").Replace(">", "&gt;");
            string color = ColorOf(level);

            _lines.Add($"<color={color}>[{System.DateTime.Now:HH:mm:ss}] {safe}</color>");

            // 限制日志行数
            if (_lines.Count > 500) _lines.RemoveRange(0, _lines.Count - 500);

            var sb = new StringBuilder();
            foreach (string line in _lines) sb.AppendLine(line);

            _logText.text = sb.ToString();

            // 文本变长后要等布局重算，再滚到底
            _log.schedule.Execute(() =>
                _log.scrollOffset = new Vector2(0, _log.verticalScroller.highValue)).ExecuteLater(1);
        }

        private static string ColorOf(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warning: return "#E6BE5A";
                case LogLevel.Error:   return "#FF7878";
                case LogLevel.Success: return "#8CD28C";
                default:               return "#C8C8C8";
            }
        }

        private void SetStatus(string text)
        {
            _status.text = text;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // 设置序列化
        // ----------------------------------------------------------------------------------------------------
        
        private void RestoreSettings()
        {
            _inputPath.value = YNarrativeSetting.instance.InputPath;
            _outputPath.value = YNarrativeSetting.instance.OutputPath;
            _clearOutput.value = YNarrativeSetting.instance.ClearOutput;
        }
        
        private void SaveSettings()
        {
            YNarrativeSetting.instance.InputPath = _inputPath.value;
            YNarrativeSetting.instance.OutputPath = _outputPath.value;
            YNarrativeSetting.instance.ClearOutput = _clearOutput.value;
        }
    }
}
