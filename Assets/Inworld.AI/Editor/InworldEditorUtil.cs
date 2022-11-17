/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Inworld.Model.Sample;
using Inworld.Util;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;


namespace Inworld.Editor
{
    /// <summary>
    /// This class would be called when package is imported, or Unity Editor is opened.
    /// </summary>
    [InitializeOnLoad]
    public class InitInworld : IPreprocessBuildWithReport
    {
        static InitInworld()
        {
            AssetDatabase.importPackageCompleted += packName =>
            {
                InworldAI.User.OrganizationID = CloudProjectSettings.organizationId;
                InworldAI.User.Name = CloudProjectSettings.userName;
                _AddDebugMacro();
            };
            EditorApplication.playModeStateChanged += _LogPlayModeState;
            EditorApplication.wantsToQuit += _ClearStatus;
            SceneView.duringSceneGui += OnSceneGUIChanged;
        }
        public int callbackOrder { get; }

        #region Call backs
        static void OnSceneGUIChanged(SceneView view)
        {
            _DrawGizmos();
            if (Event.current.type != EventType.DragExited)
                return;
            if (!InworldAI.Game.currentScene)
                return;
            GameObject avatar = Selection.activeGameObject;
            if (!avatar)
                return;
            _SetupInworldCharacter(avatar);
        }

        // YAN: Inworld Log will not display in release,
        //      unless “Development Build”, or "Is Verbose Log" is checked.
        public void OnPreprocessBuild(BuildReport report)
        {
            if (Debug.isDebugBuild || InworldAI.IsDebugMode)
                return;
            _RemoveDebugMacro();
        }
        #endregion

        #region Private Functions
        static void _AddDebugMacro()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string strSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            if (!strSymbols.Contains("INWORLD_DEBUG"))
                strSymbols = string.IsNullOrEmpty(strSymbols) ? "INWORLD_DEBUG" : strSymbols + ";INWORLD_DEBUG";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, strSymbols);
        }
        static void _RemoveDebugMacro()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string strSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            strSymbols = strSymbols.Replace(";INWORLD_DEBUG", "").Replace("INWORLD_DEBUG", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, strSymbols);
        }
        static void _DrawGizmos()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(20, 20, 350, 80));
            GUIStyle gizmosStyle = new GUIStyle
            {
                fontSize = 10
            };
            if (InworldAI.Game.currentScene)
            {
                GUILayout.Label($"Current InworldScene: <size=15><color=red><b>{InworldAI.Game.currentScene.ShortName}</b></color></size>", gizmosStyle);
                GUILayout.Label($"If you drag any InworldCharacters that are not in\n<b>{InworldAI.Game.currentScene.ShortName}</b>, they will be deleted!", gizmosStyle);
            }
            else
            {
                GUILayout.Label($"No InworldScene has found. Please set in InworldStudio Panel", gizmosStyle);
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        static void _SetupInworldCharacter(GameObject avatar)
        {
            InworldCharacterData selectedCharacter = InworldAI.User.Characters.Values.FirstOrDefault
                (charData => charData.CharacterName == avatar.transform.name);
            if (selectedCharacter)
                InworldEditor.SetupInworldCharacter(avatar, selectedCharacter);
            else if (avatar.transform.name == "Default" && InworldAI.Game.currentCharacter != null)
                InworldEditor.SetupInworldCharacter(avatar, InworldAI.Game.currentCharacter);
            else
            {
                InworldCharacterData[] charList = Resources.LoadAll<InworldCharacterData>("Characters");
                selectedCharacter = charList.FirstOrDefault(charData => charData.avatar.transform.name == avatar.transform.name);
                if (selectedCharacter)
                    InworldEditor.SetupInworldCharacter(avatar, selectedCharacter);
            }
        }
        static void _LogPlayModeState(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (EditorWindow.HasOpenInstances<InworldEditor>())
                    {
                        PlayerPrefs.SetInt("OPEN_INWORLD_STUDIO", 1);
                    }
                    if (InworldAI.IsDebugMode)
                        _AddDebugMacro();
                    else
                        _RemoveDebugMacro();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (PlayerPrefs.GetInt("OPEN_INWORLD_STUDIO") == 1)
                    {
                        PlayerPrefs.SetInt("OPEN_INWORLD_STUDIO", 0);
                        InworldEditor.Instance.Init();
                    }
                    _AddDebugMacro();
                    break;
            }
        }
        static bool _ClearStatus()
        {
            InworldEditor.Instance.Close();
            return true;
        }
        #endregion
    }
    /// <summary>
    /// This class defines Editor integrations.
    /// Such as Top Menu, Right click menu, Editor > Project Settings, Preference, etc.
    /// </summary>
    public static class InworldAISettingsProvider
    {
        /// <summary>
        /// For the options on top menu bar "Inworld". 
        /// </summary>
        #region Top Menu
        [MenuItem("Inworld/Studio Panel", false ,0)]
        static void TopMenuConnectStudio() => InworldEditor.Instance.ShowPanel();
        
        [MenuItem("Inworld/Setting Panel", false, 1)]
        static void TopMenuShowPanel() => Selection.SetActiveObjectWithContext(InworldAI.Instance, InworldAI.Instance);
        #endregion

        /// <summary>
        /// For right click the project window.
        /// </summary>
        #region Asset Menu
        [MenuItem("Assets/Inworld Studio Panel", false ,0)]
        static void ConnectStudio() => InworldEditor.Instance.ShowPanel();

        [MenuItem("Assets/Inworld Setting Panel", false, 1)]
        static void ShowPanel() => Selection.SetActiveObjectWithContext(InworldAI.Instance, InworldAI.Instance);
        #endregion

        /// <summary>
        /// For the menu in "Edit > Project Settings > Inworld.AI"
        /// </summary>
        [SettingsProvider]
        static SettingsProvider CreateInworldProjectSettingsProvider() => new SettingsProvider("Project/Inworld.AI", SettingsScope.Project)
        {
            guiHandler = (searchContext) =>
            {
                UnityEditor.Editor.CreateEditor(InworldAI.User).OnInspectorGUI();
            },
        };
        /// <summary>
        /// For the menu in "Edit > Preference > Inworld.AI"
        /// </summary>
        [SettingsProvider]
        static SettingsProvider CreateInworldUserSettingsProvider() => new SettingsProvider("Preferences/Inworld.AI", SettingsScope.User)
        {
            guiHandler = (searchContext) =>
            {
                UnityEditor.Editor.CreateEditor(InworldAI.Settings).OnInspectorGUI();
            },
        };
    }
    
    /// <summary>
    /// Add a back button to navigate back to InworldAI Global settings.
    /// </summary>
    public class InworldInspector : UnityEditor.Editor
    {
        GUIStyle m_BtnStyle;
        GUIStyle BtnStyle
        {
            get
            {
                if (m_BtnStyle != null)
                    return m_BtnStyle;
                m_BtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fixedWidth = 100,
                    margin = new RectOffset(10, 10, 0, 0)
                };
                return m_BtnStyle;
            }
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(40);
            if (GUILayout.Button("Back", BtnStyle))
            {
                Selection.SetActiveObjectWithContext(InworldAI.Instance, InworldAI.Instance);
            }
        }
    }
    [CustomEditor(typeof(InworldGameSettings))] public class InworldGameSettingInspector : InworldInspector {}
    [CustomEditor(typeof(GLTFAvatarLoader))] public class InworldAvatarLoaderInspector : InworldInspector {}
}
#endif