/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Inworld.Sample.UI;
using Inworld.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityTemplateProjects;


namespace Inworld.Sample
{
    /// <summary>
    /// This is the class for global text management, by original, it's added in Player Controller.
    /// And would be called by Keycode.Backquote.
    /// </summary>
    public class InworldPlayer : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] SimpleCameraController m_CameraController;
        [SerializeField] GameObject m_Canvas;
        [SerializeField] RectTransform m_ScrollRT;
        [SerializeField] RectTransform m_InputPanelRT;
        [SerializeField] RectTransform m_InputFieldRT;
        [SerializeField] RectTransform m_SendButtonRT;
        [SerializeField] RecordButton m_RecordButton;
        [SerializeField] RectTransform m_ContentRT;
        [SerializeField] ChatBubble m_BubbleLeft;
        [SerializeField] ChatBubble m_BubbleRight;
        [SerializeField] TMP_InputField m_InputField;
        #endregion

        #region Private Variables
        Dictionary<string, ChatBubble> m_Bubbles = new Dictionary<string, ChatBubble>();
        Dictionary<string, InworldCharacter> m_Characters = new Dictionary<string, InworldCharacter>();
        Vector2 m_ScreenSize;
        #endregion

        #region Monobehavior Functions
        void Start()
        {
            InworldController.Instance.OnStateChanged += OnControllerStatusChanged;
        }
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.BackQuote))
            {
                m_Canvas.SetActive(!m_Canvas.activeSelf);
                m_CameraController.enabled = !m_Canvas.activeSelf;
            }
            InworldController.IsCapturing = m_RecordButton.IsRecording || !m_Canvas.activeSelf;
            if (!m_Canvas.activeSelf)
                return;
            if (!Input.GetKeyUp(KeyCode.Return) && !Input.GetKeyUp(KeyCode.KeypadEnter))
                return;
            if (string.IsNullOrEmpty(m_InputField.text))
                return;
            InworldController.Instance.CurrentCharacter.SendText(m_InputField.text);
            m_InputField.text = null;
        }
        #endregion

        #region Callbacks
        void OnControllerStatusChanged(ControllerStates states)
        {
            if (states == ControllerStates.Connected)
            {
                _ClearHistoryLog();
                foreach (InworldCharacter iwChar in InworldController.Instance.Characters)
                {
                    m_Characters[iwChar.ID] = iwChar;
                    iwChar.Event.AddListener(OnInteractionStatus);
                }
            }
        }
        void OnInteractionStatus(InteractionStatus status, List<HistoryItem> historyItems)
        {
            if (status != InteractionStatus.HistoryChanged)
                return;
            _RefreshBubbles(historyItems);
        }
        #endregion
        
        /// <summary>
        /// UI Functions. Called by button "Send" clicked or Keycode.Return clicked.
        /// </summary>
        public void SendText()
        {
            if (string.IsNullOrEmpty(m_InputField.text))
                return;
            InworldController.Instance.CurrentCharacter.SendText(m_InputField.text);
            m_InputField.text = null;
        }

        #region Private Functions
        void _RefreshBubbles(List<HistoryItem> historyItems)
        {
            foreach (HistoryItem item in historyItems)
            {
                if (!m_Bubbles.ContainsKey(item.UtteranceId))
                {
                    if (item.Event.Routing.Source.IsPlayer())
                    {
                        m_Bubbles[item.UtteranceId] = Instantiate(m_BubbleLeft, m_ContentRT);
                        m_Bubbles[item.UtteranceId].SetBubble(InworldAI.User.Name, InworldAI.Settings.DefaultThumbnail);
                    }
                    else if (item.Event.Routing.Source.IsAgent())
                    {
                        m_Bubbles[item.UtteranceId] = Instantiate(m_BubbleRight, m_ContentRT);
                        InworldCharacter source = m_Characters[item.Event.Routing.Source.Id];
                        m_Bubbles[item.UtteranceId].SetBubble(source.CharacterName, source.Data.Thumbnail);
                    }
                }
                m_Bubbles[item.UtteranceId].Text = item.Event.Text;
                _SetContentHeight();
            }
        }
        void _ClearHistoryLog()
        {
            foreach (KeyValuePair<string, ChatBubble> kvp in m_Bubbles)
            {
                Destroy(kvp.Value.gameObject, 0.25f);
            }
            m_Bubbles.Clear();
            m_Characters.Clear();
        }
        void _SetContentHeight()
        {
            float fHeight = m_Bubbles.Values.Sum(bubble => bubble.Height);
            m_ContentRT.sizeDelta = new Vector2(m_ContentRT.sizeDelta.x, fHeight);
        }

        void _CheckMode()
        {
            
        }
        #endregion
    }
}
