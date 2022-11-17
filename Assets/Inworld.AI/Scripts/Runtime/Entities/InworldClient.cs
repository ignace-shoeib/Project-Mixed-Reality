/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Grpc.Core;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Inworld.Runtime;
using Inworld.Util;
using Inworld.Grpc;
using GrpcPacket = Inworld.Grpc.InworldPacket;
using InworldPacket = Inworld.Packets.InworldPacket;


namespace Inworld
{
    /// <summary>
    /// This class used to save the communication data in runtime.
    /// </summary>
    class Connection
    {
        // Events ready to send to server.
        internal readonly ConcurrentQueue<GrpcPacket> outgoingEventsQueue = new ConcurrentQueue<GrpcPacket>();
        // Audio chunks ready to play.
        internal readonly ConcurrentQueue<Packets.AudioChunk> incomingAudioQueue = new ConcurrentQueue<Packets.AudioChunk>();
        // Animation Chunks.
        internal readonly ConcurrentQueue<Packets.AnimationChunk> incomingAnimationQueue = new ConcurrentQueue<Packets.AnimationChunk>();
        // Events that need to be processed by NPC.
        internal readonly ConcurrentQueue<InworldPacket> incomingInteractionsQueue = new ConcurrentQueue<InworldPacket>();
    }
    /// <summary>
    /// This is the logic class for Server communication.
    /// </summary>
    class InworldClient
    {
        #region Private Variables
        readonly WorldEngine.WorldEngineClient m_WorldEngineClient;
        readonly Channel m_Channel;
        AsyncDuplexStreamingCall<GrpcPacket, GrpcPacket> m_StreamingCall;
        Connection m_CurrentConnection;
        InworldAuth m_InworldAuth;
        string m_SessionKey = "";
        Metadata m_Header;
        internal event Action<RuntimeStatus, string> RuntimeEvent;
        #endregion

        #region Properties
        internal ConcurrentQueue<Exception> Errors { get; } = new ConcurrentQueue<Exception>();
        internal bool IsInteracting { get; private set; }
        internal bool HasInit => !m_InworldAuth.IsExpired;
        bool IsSessionInitialized => m_SessionKey.Length != 0;
        Google.Protobuf.WellKnownTypes.Timestamp Now => Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
        #endregion
        
        internal InworldClient()
        {
            m_Channel = new Channel(InworldAI.Game.RuntimeServer, new SslCredentials());
            m_WorldEngineClient = new WorldEngine.WorldEngineClient(m_Channel);
        }
        
        #region Call backs
        void OnAuthCompleted()
        {
            InworldAI.Log("Init Success!");
            m_Header = new Metadata()
            {
                {"authorization", $"Bearer {m_InworldAuth.Token}"},
                {"session-id", m_InworldAuth.SessionID}
            };
            RuntimeEvent?.Invoke(RuntimeStatus.InitSuccess, "");
        }
        void OnAuthFailed(string msg)
        {
            RuntimeEvent?.Invoke(RuntimeStatus.InitFailed, msg);
        }
        #endregion

        #region Private Functions
        internal void GetAppAuth()
        {
#if UNITY_EDITOR 
            VSAttribution.VSAttribution.SendAttributionEvent("Login Runtime", InworldAI.k_CompanyName, InworldAI.Game.APIKey);
#endif
            m_InworldAuth = new InworldAuth(OnAuthCompleted, OnAuthFailed);
            m_InworldAuth.GenerateAccessToken(InworldAI.Game.StudioServer, InworldAI.Game.APIKey, InworldAI.Game.APISecret);
        }
        internal async Task<LoadSceneResponse> LoadScene(string sceneName)
        {
            LoadSceneRequest lsRequest = new LoadSceneRequest
            {
                Name = sceneName,
                Capabilities = InworldAI.Settings.Capabilities,
                User = InworldAI.User.Request
            };
            try
            {
                LoadSceneResponse response = await m_WorldEngineClient.LoadSceneAsync(lsRequest, m_Header);
                // Yan: They somehow use {WorkSpace}:{sessionKey} as "sessionKey" now. Need to remove the first part.
                m_SessionKey = response.Key.Split(':')[1];
                m_Header.Add("Authorization", $"Bearer {m_SessionKey}");
                RuntimeEvent?.Invoke(RuntimeStatus.LoadSceneComplete, m_SessionKey);
                return response;
            }
            catch (RpcException e)
            {
                RuntimeEvent?.Invoke(RuntimeStatus.LoadSceneFailed, e.ToString());
                return null;
            }
        }
        // Marks audio session start.
        internal void StartAudio(Packets.Routing routing)
        {
            InworldAI.Log($"Start Audio Event");
            if (IsInteracting)
                m_CurrentConnection?.outgoingEventsQueue.Enqueue(new Grpc.InworldPacket
                {
                    Timestamp = Now,
                    Routing = routing.ToGrpc(),
                    Control = new ControlEvent
                    {
                        Action = ControlEvent.Types.Action.AudioSessionStart
                    }
                });
        }

        // Marks session end.
        internal void EndAudio(Packets.Routing routing)
        {
            if (IsInteracting)
                m_CurrentConnection?.outgoingEventsQueue.Enqueue(new Grpc.InworldPacket
                {
                    Timestamp = Now,
                    Routing = routing.ToGrpc(),
                    Control = new ControlEvent()
                    {
                        Action = ControlEvent.Types.Action.AudioSessionEnd
                    }
                });
        }

        // Sends audio chunk to server.
        internal void SendAudio(Packets.AudioChunk audioEvent)
        {
            if (IsInteracting)
                m_CurrentConnection?.outgoingEventsQueue.Enqueue(audioEvent.ToGrpc());
        }
        internal bool GetAudioChunk(out Packets.AudioChunk chunk)
        {
            if (m_CurrentConnection != null)
            {
                return m_CurrentConnection.incomingAudioQueue.TryDequeue(out chunk);
            }
            chunk = null;
            return false;
        }
        internal bool GetAnimationChunk(out Packets.AnimationChunk chunk)
        {
            if (m_CurrentConnection != null)
            {
                return m_CurrentConnection.incomingAnimationQueue.TryDequeue(out chunk);
            }
            chunk = null;
            return false;
        }
        internal void SendEvent(InworldPacket e)
        {
            if (IsInteracting)
                m_CurrentConnection?.outgoingEventsQueue.Enqueue(e.ToGrpc());
        }
        internal bool GetIncomingEvent(out InworldPacket incomingEvent)
        {
            if (m_CurrentConnection != null)
            {
                return m_CurrentConnection.incomingInteractionsQueue.TryDequeue(out incomingEvent);
            }
            incomingEvent = null;
            return false;
        }
        internal async Task StartSession()
        {
            if (!IsSessionInitialized)
            {
                throw new ArgumentException("No sessionKey to start Inworld session, use CreateWorld first.");
            }
            // New queue for new session.
            Connection connection = new Connection();
            m_CurrentConnection = connection;

            IsInteracting = true;
            try
            {
                using (m_StreamingCall = m_WorldEngineClient.Session(m_Header))
                {
                    // https://grpc.github.io/grpc/csharp/api/Grpc.Core.IAsyncStreamReader-1.html
                    Task inputTask = Task.Run(async () =>
                    {
                        while (IsInteracting)
                        {
                            bool next;
                            try
                            {
                                // Waiting response for some time before checking if done.
                                next = await m_StreamingCall.ResponseStream.MoveNext();
                            }
                            catch (RpcException rpcException)
                            {
                                if (rpcException.StatusCode == StatusCode.Cancelled)
                                {
                                    next = false;
                                }
                                else
                                {
                                    // rethrowing other errors.
                                    throw;
                                }
                            }
                            if (next)
                            {
                                GrpcPacket response = m_StreamingCall.ResponseStream.Current;
                                if (response.DataChunk != null)
                                {
                                    switch (response.DataChunk.Type)
                                    {
                                        case DataChunk.Types.DataType.Audio:
                                            connection.incomingAudioQueue.Enqueue(new Packets.AudioChunk(response));
                                            break;
                                        case DataChunk.Types.DataType.Animation:
                                            connection.incomingAnimationQueue.Enqueue(new Packets.AnimationChunk(response));
                                            break;
                                        default:
                                            InworldAI.LogError($"Unsupported incoming event: {response}");
                                            break;
                                    }
                                }
                                else if (response.Text != null)
                                {
                                    connection.incomingInteractionsQueue.Enqueue(new Packets.TextEvent(response));
                                }
                                else if (response.Gesture != null)
                                {
                                    connection.incomingInteractionsQueue.Enqueue(new Packets.GestureEvent(response));
                                }
                                else if (response.Control != null)
                                {
                                    connection.incomingInteractionsQueue.Enqueue(new Packets.ControlEvent(response));
                                } else if (response.Emotion != null)
                                {
                                    connection.incomingInteractionsQueue.Enqueue(new Packets.EmotionEvent(response));
                                }
                                else
                                {
                                    InworldAI.LogError($"Unsupported incoming event: {response}");
                                }
                            }
                            else
                            {
                                InworldAI.Log("Session is closed.");
                                break;
                            }
                        }
                    });
                    Task outputTask = Task.Run(async () =>
                    {
                        while (IsInteracting)
                        {
                            Task.Delay(100).Wait();
                            // Sending all outgoing events.
                            Grpc.InworldPacket e;
                            while (connection.outgoingEventsQueue.TryDequeue(out e))
                            {
                                if (IsInteracting)
                                {
                                    await m_StreamingCall.RequestStream.WriteAsync(e);
                                }
                            }
                        }
                    });
                    await Task.WhenAll(inputTask, outputTask);
                }
            }
            catch (Exception e)
            {
                IsInteracting = false;
                Errors.Enqueue(e);
            }
            finally
            {
                IsInteracting = false;
            }
        }
        internal async Task EndSession()
        {
            if (IsInteracting)
            {
                m_CurrentConnection = null;
                IsInteracting = false;
                await m_StreamingCall.RequestStream.CompleteAsync();
                m_StreamingCall.Dispose();
            }
        }
        internal void Destroy()
        {
#pragma warning disable CS4014
            EndSession();
#pragma warning restore CS4014
            m_Channel.ShutdownAsync();
        }
        #endregion
    }
}