/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace Inworld.Util
{
    /// <summary>
    /// This class is used to get data fetching progress of InworldCharacter.
    /// </summary>
    public class CharacterFetchingProgress
    {
        public UnityWebRequestAsyncOperation thumbnailProgress;
        public UnityWebRequestAsyncOperation avatarProgress;

        public float Progress
        {
            get
            {
                if (thumbnailProgress == null && avatarProgress == null)
                    return 1f;
                if (thumbnailProgress == null)
                    return avatarProgress.progress;
                if (avatarProgress == null)
                    return thumbnailProgress.progress;
                return thumbnailProgress.progress * 0.2f + avatarProgress.progress * 0.8f;
            }
        }
    }
    /// <summary>
    /// The class for downloading related thumbnail/avatars of Inworld Character Data.
    /// </summary>
    public class InworldFileDownloader : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] bool m_DownloadThumbnail;
        [SerializeField] bool m_DownloadAvatar;
        #endregion

        #region Events
        public event Action<InworldCharacterData> OnAvatarDownloaded;
        public event Action<InworldCharacterData> OnThumbnailDownloaded;
        public event Action<InworldCharacterData> OnAvatarFailed;
        public event Action<InworldCharacterData> OnThumbnailFailed;
        #endregion

        #region Private Members & Functions
        const string k_ResourcePath = "Assets/Inworld.AI/Resources";
        readonly Dictionary<InworldCharacterData, CharacterFetchingProgress> m_RequestPool = new Dictionary<InworldCharacterData, CharacterFetchingProgress>();
        UnityWebRequest _GetResponse(AsyncOperation op)
        {
            return op is not UnityWebRequestAsyncOperation webTask ? null : webTask.webRequest;
        }        
        #endregion
        
        #region Callbacks
        void OnThumbnailComplete(AsyncOperation op)
        {
            UnityWebRequest uwr = _GetResponse(op);
            if (uwr == null)
                return;
            foreach (InworldCharacterData charData in m_RequestPool.Keys.Where(charData => charData.previewImgUri == uwr.url))
            {
                if (uwr.isDone)
                    OnThumbnailDownloaded?.Invoke(charData);
                else
                    OnThumbnailFailed?.Invoke(charData);
            }
        }
        void OnAvatarUpdate(AsyncOperation op)
        {
            UnityWebRequest uwr = _GetResponse(op);
            if (uwr == null)
                return;
            foreach (InworldCharacterData charData in m_RequestPool.Keys.Where(charData => charData.modelUri == uwr.url))
            {
                if (uwr.isDone)
                    OnAvatarDownloaded?.Invoke(charData);
                else
                    OnAvatarFailed?.Invoke(charData);
            }
        }
        #endregion

        #region Properties & Functions
        /// <summary>
        /// Return the progress of all the downloading objects.
        /// </summary>
        public float Progress
        {
            get
            {
                if (m_RequestPool.Count == 0)
                    return 100f;
                return m_RequestPool.Sum(req => req.Value.Progress) / m_RequestPool.Count * 100;
            }
        }
        
        /// <summary>
        /// Download thumbnail and avatar of Inworld Character Data.
        /// </summary>
        /// <param name="charData">target Inworld character Data</param>
        public void DownloadCharacterData(InworldCharacterData charData)
        {
            UnityWebRequest uwrThumbnail = null;
            UnityWebRequest uwrAvatar = null;
            
            if (!File.Exists(charData.LocalThumbnailFileName) && m_DownloadThumbnail)
            {
                if (!string.IsNullOrEmpty(charData.previewImgUri))
                {
                    uwrThumbnail = new UnityWebRequest(charData.previewImgUri);
                    uwrThumbnail.downloadHandler = new DownloadHandlerFile(charData.LocalThumbnailFileName);
                }
                else
                {
                    OnThumbnailFailed?.Invoke(charData);
                }
            }
            if (!File.Exists(charData.LocalAvatarFileName) && m_DownloadAvatar)
            {
                if (!string.IsNullOrEmpty(charData.modelUri))
                {
                    uwrAvatar = new UnityWebRequest(charData.modelUri);
                    uwrAvatar.downloadHandler = new DownloadHandlerFile(charData.LocalAvatarFileName);
                }
                else
                {
                    OnAvatarFailed?.Invoke(charData);
                }
            }
            CharacterFetchingProgress fetchingProcess = new CharacterFetchingProgress();
            UnityWebRequestAsyncOperation reqThumbnail = uwrThumbnail?.SendWebRequest();
            if (reqThumbnail != null)
            {
                reqThumbnail.completed += OnThumbnailComplete;
                fetchingProcess.thumbnailProgress = reqThumbnail;
            }
            UnityWebRequestAsyncOperation reqAvatar = uwrAvatar?.SendWebRequest();
            if (reqAvatar != null)
            {                
                reqAvatar.completed += OnAvatarUpdate;
                fetchingProcess.avatarProgress = reqAvatar;
            }
            m_RequestPool[charData] = fetchingProcess;
        }
        
        /// <summary>
        /// Clear all the current Downloading requests.
        /// </summary>
        public void Init() => m_RequestPool.Clear();
        
        /// <summary>
        /// Download Thumbnail of the Inworld Character Data.
        /// </summary>
        /// <param name="charData">Target Inworld Character Data</param>
        public void DownloadThumbnail(InworldCharacterData charData)
        {
            UnityWebRequest uwrThumbnail = new UnityWebRequest(charData.previewImgUri);
            uwrThumbnail.downloadHandler = new DownloadHandlerFile(charData.LocalThumbnailFileName);
            CharacterFetchingProgress fetchingProcess = new CharacterFetchingProgress();
            UnityWebRequestAsyncOperation reqThumbnail = uwrThumbnail.SendWebRequest();
            if (reqThumbnail != null)
            {
                reqThumbnail.completed += OnThumbnailComplete;
                fetchingProcess.thumbnailProgress = reqThumbnail;
            }
            m_RequestPool[charData] = fetchingProcess;
        }
        /// <summary>
        /// Download Avatar of the Inworld Character Data
        /// </summary>
        /// <param name="charData">Target Inworld Character Data</param>
        public void DownloadAvatar(InworldCharacterData charData)
        {
            UnityWebRequest uwrAvatar = new UnityWebRequest(charData.modelUri);
            uwrAvatar.downloadHandler = new DownloadHandlerFile(charData.LocalAvatarFileName);
            CharacterFetchingProgress fetchingProcess = new CharacterFetchingProgress();
            UnityWebRequestAsyncOperation reqAvatar = uwrAvatar.SendWebRequest();
            if (reqAvatar != null)
            {                
                reqAvatar.completed += OnAvatarUpdate;
                fetchingProcess.avatarProgress = reqAvatar;
            }
            m_RequestPool[charData] = fetchingProcess;
        }
        #endregion
    }
}
