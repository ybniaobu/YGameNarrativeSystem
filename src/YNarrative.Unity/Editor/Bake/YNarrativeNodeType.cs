namespace YNarrative.Editor
{
    internal enum YNarrativeNodeType
    {
        Line, Narration, Choice, Random, Jump, Event, End,
    }
    
    internal static class YNarrativeNodeExtensions
    {
        public static bool TryParse(string raw, out YNarrativeNodeType type)
        {
            switch (raw)
            {
                case "line":      type = YNarrativeNodeType.Line;      return true;
                case "narration": type = YNarrativeNodeType.Narration; return true;
                case "choice":    type = YNarrativeNodeType.Choice;    return true;
                case "random":    type = YNarrativeNodeType.Random;    return true;
                case "jump":      type = YNarrativeNodeType.Jump;      return true;
                case "event":     type = YNarrativeNodeType.Event;     return true;
                case "end":       type = YNarrativeNodeType.End;       return true;
                default:          type = default;                     return false;
            }
        }
        
        /// <summary>
        /// 必须恰好一条出边。
        /// </summary>
        public static bool RequiresSingleOutgoing(this YNarrativeNodeType type)
            => type == YNarrativeNodeType.Line
               || type == YNarrativeNodeType.Narration
               || type == YNarrativeNodeType.Jump
               || type == YNarrativeNodeType.Event;
        
        /// <summary>
        /// 必须多条出边。
        /// </summary>
        public static bool RequiresMultipleOutgoing(this YNarrativeNodeType type)
            => type == YNarrativeNodeType.Choice
               || type == YNarrativeNodeType.Random;
        
        /// <summary>
        /// 必须没有出边。
        /// </summary>
        public static bool RequiresNoOutgoing(this YNarrativeNodeType type)
            => type == YNarrativeNodeType.End;
        
        /// <summary>
        /// 必须有文本。
        /// </summary>
        public static bool RequiresText(this YNarrativeNodeType type)
            => type == YNarrativeNodeType.Line
               || type == YNarrativeNodeType.Narration
               || type == YNarrativeNodeType.Choice
               || type == YNarrativeNodeType.Random
               || type == YNarrativeNodeType.End;
    }
}