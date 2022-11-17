/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Inworld.Grpc;
using Inworld.Model.Sample;
using Inworld.Util;
using UnityEngine;


namespace Inworld.Model
{
    /// <summary>
    /// This class is the basic class to display head animations,
    /// that only supports looking at players.
    ///
    /// If you want to use detailed head-eye movement, please do the followings:
    /// 1. purchase and download page `Realistic Eye Movements`
    /// https://assetstore.unity.com/packages/tools/animation/realistic-eye-movements-29168
    /// 2. Add `LookTargetController` and `EyeAndHeadAnimator` components to InworldCharacters.
    /// 3. Implement `SetupHeadMovement`:
    ///     a. Call Resources.Load<TextAsset>(m_HeadEyeAsset);
    ///     b. Call `EyeAndHeadAnimator::ImportFromJson()`, with the data of the TextAsset you loaded.
    /// </summary>
    public class HeadAnimation : MonoBehaviour, InworldAnimation, IEyeHeadAnimLoader
    {
        #region Inspector Variables
        [SerializeField] protected Animator m_Animator;
        [SerializeField] protected InworldCharacter m_InworldCharacter;
        [SerializeField] string m_HeadEyeAsset = "Animations/REMRPM";
        #endregion

        #region Private Variables
        Transform m_trLookAt = null;
        Transform m_Transform;
        Vector3 m_vecInitPosition;
        Vector3 m_vecInitEuler;
        float m_LookAtWeight = 0;
        #endregion
        
        #region Properties
        /// <summary>
        /// Get/Set the attached Animator.
        /// </summary>
        public Animator Animator
        {
            get => m_Animator; 
            set => m_Animator = value;
        }
        /// <summary>
        /// Get/Set the attached Inworld Character.
        /// </summary>
        public InworldCharacter Character
        {
            get => m_InworldCharacter; 
            set => m_InworldCharacter = value;
        }
        #endregion

        #region Monobehavior Functions
        void Start()
        {
            if (!Animator)
                Animator = GetComponent<Animator>();
            m_Transform = transform;
            m_vecInitEuler = m_Transform.localEulerAngles;
            m_vecInitPosition = m_Transform.localPosition;
            InworldController.Instance.OnCharacterChanged += OnCharacterChanged;
        }
        void OnDisable()
        {
            if (InworldController.Instance)
            {
                InworldController.Instance.OnCharacterChanged -= OnCharacterChanged;
            }
        }
        void OnAnimatorIK(int layerIndex)
        {
            if (!Animator)
                return;
            if (m_trLookAt == null)
            {
                _StopLookAt();
                return;
            }
            _StartLookAt(m_trLookAt.position);
        }
        #endregion

        #region Callbacks
        void OnCharacterChanged(InworldCharacter oldChar, InworldCharacter newChar)
        {
            if (Character == oldChar)
                m_trLookAt = null;
            else if (Character == newChar)
                m_trLookAt = InworldController.Player.transform;
        }
        #endregion

        #region Private Functions
        void _StartLookAt(Vector3 lookPos)
        {
            m_LookAtWeight = Mathf.Clamp(m_LookAtWeight + 0.01f, 0, 1);
            Animator.SetLookAtWeight(m_LookAtWeight);
            Animator.SetLookAtPosition(lookPos);
        }
        void _StopLookAt()
        {
            m_Transform.localPosition = m_vecInitPosition;
            m_Transform.localEulerAngles = m_vecInitEuler;
            m_LookAtWeight = Mathf.Clamp(m_LookAtWeight - 0.01f, 0, 1);
            Animator.SetLookAtWeight(m_LookAtWeight);
        }
        #endregion
        
        #region Interface Implementation
        public void HandleMainStatus(AnimMainStatus status)
        {
            //Implement your own logic here.
        }
        public void HandleEmotion(EmotionEvent.Types.SpaffCode spaffCode)
        {
            //Implement your own logic here.
        }
        public void HandleGesture(GestureEvent.Types.Type gesture)
        {
            //Implement your own logic here.
        }
        public void SetupHeadMovement(GameObject avatar)
        {
            InworldAI.Log($"If you want to integrate detailed head/eye movent,\nplease Load {m_HeadEyeAsset} as Text,\nthen use`Realistic Eye Movements` to load it from json");
            //Implement your own logic here.
        }
        #endregion
    }
}
