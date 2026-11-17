using System;
using System.Collections.Generic;

namespace YNarrative.Editor
{
    /// <summary>
    /// 对话资源校验结果。
    /// </summary>
    /// <remarks>
    /// error 会阻断烘焙，warning 不阻断。
    /// </remarks>
    internal sealed class YNarrativeValidateResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        
        public bool Succeeded => errors.Count == 0;
    }

    /// <summary>
    /// 对话资源校验器。
    /// </summary>
    /// <remarks>
    /// TODO: 节点可达性、终止性（从任意节点能否走到 end）等场景 Scene 表建好后再补。
    /// </remarks>
    internal static class YNarrativeValidator
    {
        public static YNarrativeValidateResult Validate(YNarrativeSource source)
        {
            YNarrativeValidateResult result = new YNarrativeValidateResult();

            if (source.chapters.Count == 0) result.errors.Add("没有任何章节");

            HashSet<string> speakerIds = ValidateSpeakers(source, result);
            HashSet<string> usedSpeakers = new HashSet<string>(StringComparer.Ordinal);

            ValidateChapters(source, speakerIds, usedSpeakers, result);
            ValidateUnusedSpeakers(source, usedSpeakers, result);

            return result;
        }

        // ----------------------------------------------------------------------------------------------------
        // 角色
        // ----------------------------------------------------------------------------------------------------

        private static HashSet<string> ValidateSpeakers(YNarrativeSource source, YNarrativeValidateResult result)
        {
            // 收集 speaker_id，后面用于校验节点中使用的 speaker_id 是否存在
            HashSet<string> speakerIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (YNarrativeCharacter speaker in source.speakers)
            {
                if (string.IsNullOrWhiteSpace(speaker.speakerId))
                    result.errors.Add($"characters.csv 第 {speaker.row} 行：speaker_id 为空");
                else if (!speakerIds.Add(speaker.speakerId))
                    result.errors.Add($"characters.csv 第 {speaker.row} 行：speaker_id 重复：{speaker.speakerId}");
            }

            return speakerIds;
        }

        private static void ValidateUnusedSpeakers(YNarrativeSource source, HashSet<string> usedSpeakers,
                                                   YNarrativeValidateResult result)
        {
            foreach (YNarrativeCharacter speaker in source.speakers)
            {
                if (string.IsNullOrWhiteSpace(speaker.speakerId)) continue; // 已作为错误报过
                if (usedSpeakers.Contains(speaker.speakerId)) continue;

                result.warnings.Add($"characters.csv 第 {speaker.row} 行：speaker_id 没有被任何节点使用：{speaker.speakerId}");
            }
        }

        // ----------------------------------------------------------------------------------------------------
        // 章节
        // ----------------------------------------------------------------------------------------------------

        private static void ValidateChapters(YNarrativeSource source, HashSet<string> speakerIds,
                                             HashSet<string> usedSpeakers, YNarrativeValidateResult result)
        {
            HashSet<string> chapterIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (YNarrativeChapter chapter in source.chapters)
            {
                if (string.IsNullOrWhiteSpace(chapter.chapterId))
                    result.errors.Add($"{chapter.title}：chapter_id 为空");
                else if (!chapterIds.Add(chapter.chapterId))
                    result.errors.Add($"chapter_id 重复：{chapter.chapterId}");

                ValidateNodes(chapter, speakerIds, usedSpeakers, result);
            }
        }

        private static void ValidateNodes(YNarrativeChapter chapter, HashSet<string> speakerIds,
                                            HashSet<string> usedSpeakers, YNarrativeValidateResult result)
        {
            // 章节没 id 时用标题兜底，避免错误信息里一片空白
            string chapterAt = string.IsNullOrWhiteSpace(chapter.chapterId) ? chapter.title : chapter.chapterId;

            if (chapter.nodes.Count == 0)
            {
                result.errors.Add($"{chapterAt}：章节没有任何节点");
                return;
            }

            // 收集 node_id，后面用于校验分支目标是否存在
            HashSet<string> nodeIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (YNarrativeNode node in chapter.nodes)
            {
                if (string.IsNullOrWhiteSpace(node.nodeId))
                    result.errors.Add($"{chapterAt} 第 {node.row} 行：node_id 为空");
                else if (!nodeIds.Add(node.nodeId))
                    result.errors.Add($"{chapterAt} 第 {node.row} 行：node_id 重复：{node.nodeId}");
            }

            string prefix = chapter.chapterId + "_";

            // 入口节点：约定为 {chapter_id}_0001，TODO：等场景 Scene 表建好删除章节的入口节点
            if (!string.IsNullOrWhiteSpace(chapter.chapterId) && !nodeIds.Contains(prefix + "0001"))
                result.errors.Add($"{chapterAt}：入口节点不存在：{prefix}0001");

            bool hasEnd = false;

            foreach (YNarrativeNode node in chapter.nodes)
            {
                string at = $"{chapterAt} 第 {node.row} 行";

                // node_id 前缀校验：前缀必须属于本章，防止复制粘贴别的章忘了改
                if (!string.IsNullOrWhiteSpace(node.nodeId) 
                    && !string.IsNullOrWhiteSpace(chapter.chapterId)
                    && !node.nodeId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.errors.Add($"{at}：node_id 前缀不是 {chapter.chapterId}：{node.nodeId}");
                }

                // 节点类型校验
                if (!YNarrativeNodeExtensions.TryParse(node.type, out YNarrativeNodeType type))
                {
                    result.errors.Add($"{at}：type 不合法：{node.type}");
                    continue; // 类型不确定，后面的类型相关规则跳过
                }

                // 终止节点校验：必须有 end 节点，TODO：等场景 Scene 表建好删除 nodeType 的 end，因为 end 节点是为了烘焙时能找到终止节点，场景表建好后可以直接从场景表找终止场景
                if (type == YNarrativeNodeType.End) hasEnd = true;

                // 出边数量校验：根据节点类型要求，检查分支数量是否符合预期
                int count = node.branches.Count;
                if (type.RequiresNoOutgoing() && count != 0)
                    result.errors.Add($"{at}：{node.type} 不应有分支，实际 {count} 条");

                if (type.RequiresSingleOutgoing() && count != 1)
                    result.errors.Add($"{at}：{node.type} 必须仅有 1 条分支，实际 {count} 条");

                if (type.RequiresMultipleOutgoing() && count == 0)
                    result.errors.Add($"{at}：{node.type} 至少需要 1 条分支，实际 {count} 条");

                // 分支目标校验：存在 + 不重复
                HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);

                foreach (string target in node.branches)
                {
                    if (!nodeIds.Contains(target))
                        result.errors.Add($"{at}：分支目标不存在：{target}");
                    else if (!targets.Add(target))
                        result.errors.Add($"{at}：分支目标重复：{target}");
                }

                // 角色 ID 校验：允许为空，表示旁白
                if (!string.IsNullOrWhiteSpace(node.speakerId))
                {
                    if (!speakerIds.Contains(node.speakerId))
                        result.errors.Add($"{at}：speaker_id 不存在：{node.speakerId}");
                    else
                        usedSpeakers.Add(node.speakerId);
                }

                // 文本校验
                if (type.RequiresText() && string.IsNullOrWhiteSpace(node.zhCn))
                    result.errors.Add($"{at}：zh_cn 不能为空");

                // 翻译缺失只提示，不阻断，TODO: 后续提供本地化语言的设置，根据设置提示缺失的语言
                if (!string.IsNullOrWhiteSpace(node.zhCn) && string.IsNullOrWhiteSpace(node.enUs))
                    result.warnings.Add($"{at}：缺少 en_us");
            }

            if (!hasEnd)
                result.errors.Add($"{chapterAt}：没有 end 节点");
        }
    }
}
