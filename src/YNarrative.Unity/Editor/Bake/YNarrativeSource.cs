using System.Collections.Generic;

namespace YNarrative.Editor
{
    internal sealed class YNarrativeSource
    {
        public readonly List<YNarrativeChapter> chapters = new List<YNarrativeChapter>();
        public readonly List<YNarrativeCharacter> speakers = new List<YNarrativeCharacter>();
        public readonly List<YNarrativeVariable> variables = new List<YNarrativeVariable>();
        public readonly List<YNarrativeCondition> conditions = new List<YNarrativeCondition>();
        public readonly List<YNarrativeEffect> effects = new List<YNarrativeEffect>();
    }
    
    internal sealed class YNarrativeChapter
    {
        public string chapterId; // meta.csv: chapter_id
        public string title; // meta.csv: chapter_title
        public int num; // 从 chapter_id 解析出的序号，用于排序，-1 表示解析失败
        public readonly List<YNarrativeNode> nodes = new List<YNarrativeNode>(); // 章节下的所有节点 nodes.csv
    }
    
    internal sealed class YNarrativeNode
    {
        public string nodeId; // nodes.csv: node_id
        public string type; // nodes.csv: type
        public string speakerId; // nodes.csv: speaker_id
        public string zhCn; // nodes.csv: zh_cn
        public string enUs; // nodes.csv: en_us
        public string note; // nodes.csv: note
        public readonly List<string> branches = new List<string>(); // nodes.csv: branches，分支节点的分支列表
        
        public int row; // Excel 行号，报错用
    }
    
    internal sealed class YNarrativeCharacter
    {
        public string speakerId; // characters.csv: speaker_id
        public string name; // characters.csv: speaker_name
        public string portraitDefault; // characters.csv: portrait_default
        
        public int row; // Excel 行号，报错用
    }
    
    internal sealed class YNarrativeVariable
    {
        public string name; // variables.csv: var_name
        public string type; // variables.csv: type
        public string desc; // variables.csv: desc
        
        public int row; // Excel 行号，报错用
    }
    
    internal sealed class YNarrativeCondition
    {
        public string id; // conditions.csv: condition_id
        public string expr; // conditions.csv: expr
        public string desc; // conditions.csv: desc
        
        public int row; // Excel 行号，报错用
    }
    
    internal sealed class YNarrativeEffect
    {
        public string id; // effects.csv: effect_id
        public int seq; // effects.csv: seq
        public string op; // effects.csv: op
        public string target; // effects.csv: target
        public string value; // effects.csv: value
        
        public int row; // Excel 行号，报错用
    }
}
