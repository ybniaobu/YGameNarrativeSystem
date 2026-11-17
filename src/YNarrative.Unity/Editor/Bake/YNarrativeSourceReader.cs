using System;
using System.Collections.Generic;
using System.IO;

using static YNarrative.Editor.YNarrativeInputFiles;

namespace YNarrative.Editor
{
    /// <summary>
    /// 读取的结果：数据 + 错误列表。
    /// </summary>
    internal sealed class YNarrativeReadResult
    {
        public readonly YNarrativeSource source = new YNarrativeSource();
        public readonly List<string> errors = new List<string>();
        public bool Succeeded => errors.Count == 0;
    }

    /// <summary>
    /// 把输入目录读成 YNarrativeSource。
    /// </summary>
    /// <remarks>
    /// 目录结构：
    /// <code>
    /// Sources/
    /// ├── global/
    /// │   ├── characters.csv
    /// │   ├── variables.csv
    /// │   ├── conditions.csv
    /// │   └── effects.csv
    /// └── chapter/
    ///     ├── chapter01/
    ///     │   ├── meta.csv
    ///     │   └── nodes.csv
    ///     └── chapter02/...
    /// </code>
    /// </remarks>
    internal static class YNarrativeSourceReader
    {
        /// <summary>
        /// 读取指定目录下的对话源数据。
        /// </summary>
        /// <param name="sourceRoot">对话源数据的根目录（full path）</param>
        public static YNarrativeReadResult Read(string sourceRoot)
        {
            YNarrativeReadResult result = new YNarrativeReadResult();
            string globalRoot = Path.Combine(sourceRoot, GlobalDir);
            string chapterRoot = Path.Combine(sourceRoot, ChapterDir);

            if (!Directory.Exists(sourceRoot))
            {
                result.errors.Add($"输入目录不存在：{sourceRoot}");
                return result;
            }
            
            if (!Directory.Exists(globalRoot))
            {
                result.errors.Add($"缺少全局信息目录：{globalRoot}");
                return result;
            }
            
            if (!Directory.Exists(chapterRoot))
            {
                result.errors.Add($"缺少章节目录：{chapterRoot}");
                return result;
            }
            
            ReadSpeakers(globalRoot, result);
            // ReadVariables(globalRoot, result);
            // ReadConditions(globalRoot, result);
            // ReadEffects(globalRoot, result);
            
            ReadChapters(chapterRoot, result);

            return result;
        }

        // ----------------------------------------------------------------------------------------------------
        // global
        // ----------------------------------------------------------------------------------------------------

        private static void ReadSpeakers(string globalRoot, YNarrativeReadResult result)
        {
            string path = Path.Combine(globalRoot, CharactersCsv);
            CsvTable table = TryParseTable(path, result);
            if (table == null) return;
            
            int speakerIdCol = Column(table, path, result, SpeakerIdCol);
            int speakerNameCol = Column(table, path, result, SpeakerNameCol);
            int portraitDefaultCol = Column(table, path, result, PortraitDefaultCol);
            if (speakerIdCol < 0 || speakerNameCol < 0 || portraitDefaultCol < 0) return; // 缺少列

            for (int r = 0; r < table.Count; r++)
            {
                CsvRow row = table[r];
                result.source.speakers.Add(new YNarrativeCharacter
                {
                    speakerId = table.Get(row, speakerIdCol),
                    name = table.Get(row, speakerNameCol),
                    portraitDefault = table.Get(row, portraitDefaultCol),
                    row = row.row,
                });
            }
        }

        private static void ReadVariables(string globalRoot, YNarrativeReadResult result)
        {
            string path = Path.Combine(globalRoot, VariablesCsv);
            CsvTable table = TryParseTable(path, result);
            if (table == null) return;
            
            int varNameCol = Column(table, path, result, VarNameCol);
            int typeCol = Column(table, path, result, TypeCol);
            int descCol = Column(table, path, result, DescCol);
            if (varNameCol < 0 || typeCol < 0 || descCol < 0) return; // 缺少列

            for (int r = 0; r < table.Count; r++)
            {
                CsvRow row = table[r];
                result.source.variables.Add(new YNarrativeVariable
                {
                    name = table.Get(row, varNameCol),
                    type = table.Get(row, typeCol),
                    desc = table.Get(row, descCol),
                    row = row.row,
                });
            }
        }

        private static void ReadConditions(string globalRoot, YNarrativeReadResult result)
        {
            string path = Path.Combine(globalRoot, ConditionsCsv);
            CsvTable table = TryParseTable(path, result);
            if (table == null) return;
            
            int conditionIdCol = Column(table, path, result, ConditionIdCol);
            int exprCol = Column(table, path, result, ExprCol);
            int descCol = Column(table, path, result, DescCol);
            if (conditionIdCol < 0 || exprCol < 0 || descCol < 0) return; // 缺少列

            for (int r = 0; r < table.Count; r++)
            {
                CsvRow row = table[r];
                result.source.conditions.Add(new YNarrativeCondition
                {
                    id = table.Get(row, conditionIdCol),
                    expr = table.Get(row, exprCol),
                    desc = table.Get(row, descCol),
                    row = row.row,
                });
            }
        }

        private static void ReadEffects(string globalRoot, YNarrativeReadResult result)
        {
            string path = Path.Combine(globalRoot, EffectsCsv);
            CsvTable table = TryParseTable(path, result);
            if (table == null) return;
            
            int effectIdCol = Column(table, path, result, EffectIdCol);
            int seqCol = Column(table, path, result, SeqCol);
            int opCol = Column(table, path, result, OpCol);
            int targetCol = Column(table, path, result, TargetCol);
            int valueCol = Column(table, path, result, ValueCol);
            if (effectIdCol < 0 || seqCol < 0 || opCol < 0 || targetCol < 0 || valueCol < 0) return; // 缺少列
            
            for (int r = 0; r < table.Count; r++)
            {
                CsvRow row = table[r];
                int.TryParse(table.Get(row, seqCol), out int seq);

                result.source.effects.Add(new YNarrativeEffect
                {
                    id = table.Get(row, effectIdCol),
                    seq = seq,
                    op = table.Get(row, opCol),
                    target = table.Get(row, targetCol),
                    value = table.Get(row, valueCol),
                    row = row.row,
                });
            }
        }

        // ----------------------------------------------------------------------------------------------------
        // chapter
        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// 读取章节目录下的所有章节，填充 result.source.chapters。
        /// </summary>
        private static void ReadChapters(string chapterRoot, YNarrativeReadResult result)
        {
            string[] dirs = Directory.GetDirectories(chapterRoot);

            foreach (string dir in dirs)
            {
                string metaPath = Path.Combine(dir, MetaCsv);
                Dictionary<string, string> meta = ReadMeta(metaPath, result);
                if (meta == null) continue;

                meta.TryGetValue(ChapterIdCol, out string chapterId);
                meta.TryGetValue(ChapterTitleCol, out string title);

                if (string.IsNullOrWhiteSpace(chapterId))
                {
                    result.errors.Add($"{metaPath}：缺少 chapter_id");
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    result.errors.Add($"{metaPath}：缺少 chapter_title");
                }
                
                int num = ParseChapterNumber(chapterId);
                if (num < 0)
                {
                    result.errors.Add($"{metaPath}：chapter_id 解析不出序号：{chapterId}");
                }

                YNarrativeChapter chapter = new YNarrativeChapter
                {
                    chapterId = chapterId,
                    title = title,
                    num = num,
                };

                string nodesPath = Path.Combine(dir, NodesCsv);
                ReadNodes(nodesPath, chapter, result);
                result.source.chapters.Add(chapter);
            }
            
            // 按章节序号排序
            result.source.chapters.Sort((a, b) => a.num.CompareTo(b.num));
        }

        /// <summary>
        /// 读取 meta.csv，返回 key-value 字典。
        /// </summary>
        private static Dictionary<string, string> ReadMeta(string path, YNarrativeReadResult result)
        {
            List<CsvRow> rows = TryParseRows(path, result);
            if (rows == null) return null;

            var meta = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (CsvRow row in rows)
            {
                if (row.fields.Length < 2) // 不成对的行报错
                {
                    result.errors.Add($"{path}：第 {row.row} 行格式不对");
                    continue;
                }
                
                string key = row.fields[0].Trim();
                if (key.Length == 0) continue;
                meta[key] = row.fields[1];
            }

            return meta;
        }

        /// <summary>
        /// 读取 nodes.csv，填充 chapter.nodes。
        /// </summary>
        private static void ReadNodes(string path, YNarrativeChapter chapter, YNarrativeReadResult result)
        {
            CsvTable table = TryParseTable(path, result);
            if (table == null) return;

            int idCol = Column(table, path, result, NodeIdCol);
            int typeCol = Column(table, path, result, TypeCol);
            int speakerCol = Column(table, path, result, SpeakerIdCol);
            int zhCol = Column(table, path, result, ZhCnCol);
            int enCol = Column(table, path, result, EnUsCol);
            int branchCol = Column(table, path, result, BranchesCol);
            int noteCol = Column(table, path, result, NoteCol);
            if (idCol < 0 || typeCol < 0 || speakerCol < 0 || zhCol < 0 
                          || enCol < 0 || branchCol < 0 || noteCol < 0) return; // 缺少列

            for (int r = 0; r < table.Count; r++)
            {
                CsvRow row = table[r];

                YNarrativeNode node = new YNarrativeNode
                {
                    nodeId = table.Get(row, idCol),
                    type = table.Get(row, typeCol),
                    speakerId = table.Get(row, speakerCol),
                    zhCn = table.Get(row, zhCol),
                    enUs = table.Get(row, enCol),
                    note = table.Get(row, noteCol),
                    row = row.row,
                };

                string branches = table.Get(row, branchCol);
                if (!string.IsNullOrWhiteSpace(branches))
                {
                    foreach (string b in branches.Split(';')) // 解析分支列表
                        if (!string.IsNullOrWhiteSpace(b)) node.branches.Add(b.Trim());
                }

                chapter.nodes.Add(node);
            }
        }
        
        /// <summary>
        /// 解析章节编号：从 chapter_id 中提取数字部分作为章节序号。
        /// </summary>
        private static int ParseChapterNumber(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId)) return -1;

            int i = 0;
            while (i < chapterId.Length && !char.IsDigit(chapterId[i])) i++;
            if (i >= chapterId.Length) return -1;

            return int.TryParse(chapterId.Substring(i), out int number) ? number : -1;
        }

        // ----------------------------------------------------------------------------------------------------
        // 工具
        // ----------------------------------------------------------------------------------------------------

        private static CsvTable TryParseTable(string path, YNarrativeReadResult result)
        {
            try
            {
                return CsvParser.ParseTableFromFile(path);
            }
            catch (CsvParserException e)
            {
                result.errors.Add($"{path}：{e.Message}");
                return null;
            }
        }
        
        private static List<CsvRow> TryParseRows(string path, YNarrativeReadResult result)
        {
            try
            {
                return CsvParser.ParseRowsFromFile(path);
            }
            catch (CsvParserException e)
            {
                result.errors.Add($"{path}：{e.Message}");
                return null;
            }
        }

        private static int Column(CsvTable table, string path, YNarrativeReadResult result, string name)
        {
            if (table.TryGetColumn(name, out int index)) return index;
            result.errors.Add($"{path}：缺少列 {name}");
            return -1;
        }
    }
}
