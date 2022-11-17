/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Grpc.Core;
using Inworld.Grpc;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Inworld.Util
{
    /// <summary>
    /// This class stores all the data from InworldUser,
    /// including this user's access token,
    /// and Workspace/Scene/Char Data fetched from Server.
    /// </summary>
    public class InworldUserSettings : ScriptableObject
    {
        #region Inspector Variables
        [SerializeField] string m_UserName;
        [SerializeField] string m_OrganizationID;
        #endregion

        #region Private Variables
        string m_IDToken;       // ID token is used to get Studio Token.
        string m_RefreshToken;  // Refresh is used to get ID token.
        string m_InworldToken;  // Studio Token is the actual token that used to get all the data you required.
        long m_ExpirationTime;
        InworldEditorStatus m_CurrentStatus = InworldEditorStatus.Default;
        Metadata m_Header;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the user's all Inworld Workspace Data, index by workspace's MRID.
        /// </summary>
        public Dictionary<string, InworldWorkspaceData> Workspaces { get; } = new Dictionary<string, InworldWorkspaceData>();
        /// <summary>
        /// Returns the user's all Inworld Scene Data, index by InworldScene's MRID.
        /// </summary>
        public Dictionary<string, InworldSceneData> InworldScenes { get; } = new Dictionary<string, InworldSceneData>();
        /// <summary>
        /// Returns the user's all Inworld Character Data, index by Inworld Character's MRID.
        /// </summary>
        public Dictionary<string, InworldCharacterData> Characters { get; } = new Dictionary<string, InworldCharacterData>();
        /// <summary>
        /// Returns the Header of the user.
        /// Header is used to Send List Workspace/Scene/Characters Request.
        /// and is retrieved by response of GetUserAccessToken.
        /// </summary>
        public Metadata Header
        {
            get
            {
                m_Header ??= new Metadata
                {
                    {"X-Authorization-Bearer-Type", "inworld"},
                    {"Authorization", $"Bearer {m_InworldToken}"}
                };
                return m_Header;
            }
        }
        /// <summary>
        /// Returns if the user's Current Token is expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow.Ticks > m_ExpirationTime;
        /// <summary>
        /// Get/Set the user name.
        /// This value could also be set from "Edit > Project Settings > Inworld.AI"
        /// </summary>
        public string Name
        {
            get => string.IsNullOrEmpty(m_UserName) ? "InworldUser" : m_UserName;
            set => m_UserName = value;
        }
        /// <summary>
        /// Get/Set the user's organization ID.
        /// This value could also be set from "Edit > Project Settings > Inworld.AI"
        /// </summary>
        public string OrganizationID
        {
            get => string.IsNullOrEmpty(m_OrganizationID) ? Name : m_OrganizationID;
            set => m_OrganizationID = value;
        }
        /// <summary>
        /// Get/Set the Editor's Status according to this user.
        /// The reason it's saved in UserSettings is that whenever you reopened Editor Application,
        /// that history page would be opened.
        /// </summary>
        public InworldEditorStatus EditorStatus
        {
            get => m_CurrentStatus;
            set => m_CurrentStatus = value;
        }
        /// <summary>
        /// Get/Set the ID token.
        /// ID token is used to get Studio Token.
        /// </summary>
        public string IDToken
        {
            get => m_IDToken;
            set => m_IDToken = value;
        }
        /// <summary>
        /// Get/Set the Refresh Token.
        /// Refresh is used to get ID token.
        /// </summary>
        public string RefreshToken => m_RefreshToken;
        /// <summary>
        /// Get the User Request, used to send to server,
        /// let server know the user's name.
        /// </summary>
        public UserRequest Request => new UserRequest
        {
            Name = this.Name
        };
        #endregion

        #region Functions
        /// <summary>
        /// LoadData is called whenever Editor/Application is opened.
        /// It'll load all the locally saved data by its name, and set to user.
        /// </summary>
        public void LoadData()
        {
            ScriptableObject[] dataToProcess = Resources.LoadAll<ScriptableObject>(InworldAI.User.Name);
            foreach (ScriptableObject data in dataToProcess)
            {
                switch (data)
                {
                    case InworldWorkspaceData wsData:
                        Workspaces[wsData.fullName] = wsData;
                        break;
                    case InworldSceneData sceneData:
                        InworldScenes[sceneData.fullName] = sceneData;
                        break;
                    case InworldCharacterData charData:
                        Characters[charData.brain] = charData;
                        break;
                }
            }
        }
        /// <summary>
        /// Log out Studio Server.
        /// </summary>
        public void LogOut()
        {
            m_IDToken = "";
            m_RefreshToken = "";
            m_ExpirationTime = 0;
        }
        /// <summary>
        /// Refresh the user's Token
        /// </summary>
        /// <param name="strIDToken">ID token to save.</param>
        /// <param name="strRefreshToken">Refresh token to save.</param>
        public void RefreshTokens(string strIDToken, string strRefreshToken)
        {
            m_IDToken = strIDToken;
            m_RefreshToken = strRefreshToken;
        }
        /// <summary>
        /// Callback once Studio server has been logged in.
        /// </summary>
        /// <param name="inworldToken">the inworld token received</param>
        /// <param name="expireTime">
        /// the inworld token's expiretime.
        /// It only lasts 1 hour. It should be refreshed frequently.
        /// </param>
        public void OnLoggedInCompleted(string inworldToken, DateTime expireTime)
        {
            m_InworldToken = inworldToken;
            m_Header = new Metadata
            {
                {"X-Authorization-Bearer-Type", "inworld"},
                {"Authorization", $"Bearer {m_InworldToken}"}
            };
            m_ExpirationTime = expireTime.Ticks;
            InworldAI.Log($"Token Refreshed. Next Expiration: {expireTime}");
        }
        #endregion
    }
}
