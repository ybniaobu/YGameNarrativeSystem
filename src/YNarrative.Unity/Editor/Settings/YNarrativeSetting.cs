using UnityEditor;
using UnityEngine;

namespace YNarrative.Editor
{
    [FilePath("YNarrative/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class YNarrativeSetting : ScriptableSingleton<YNarrativeSetting>
    {
        private bool m_Dirty;
        
        static YNarrativeSetting()
        {
            // 域重载、编辑器退出前强制保存
            AssemblyReloadEvents.beforeAssemblyReload += static () => instance.Flush();
            EditorApplication.quitting += static () => instance.Flush();
        }

        private void MarkDirty()
        {
            m_Dirty = true;
            
            //先减后加：保证委托里只有一份 Flush
            EditorApplication.delayCall -= Flush;
            EditorApplication.delayCall += Flush;
        }
        
        private void Flush()
        {
            if (!m_Dirty) return;
            m_Dirty = false;
            Save(true);
        }
        
        // Dialogue Bake Settings
        
        [SerializeField] private string m_InputPath = YNarrativePaths.DefaultInputRoot;
        [SerializeField] private string m_OutputPath = YNarrativePaths.DefaultOutputRoot;
        [SerializeField] private bool m_ClearOutput = true;
        
        public string InputPath
        {
            get => m_InputPath;
            set
            {
                if (m_InputPath != value)
                {
                    m_InputPath = value;
                    MarkDirty();
                }
            }
        }
        
        public string OutputPath
        {
            get => m_OutputPath;
            set
            {
                if (m_OutputPath != value)
                {
                    m_OutputPath = value;
                    MarkDirty();
                }
            }
        }
        
        public bool ClearOutput
        {
            get => m_ClearOutput;
            set
            {
                if (m_ClearOutput != value)
                {
                    m_ClearOutput = value;
                    MarkDirty();
                }
            }
        }
    }
}