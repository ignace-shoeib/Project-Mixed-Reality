using System.Collections;
using System.Collections.Generic;
using Inworld;
using Inworld.Packets;
using UnityEngine;

public class Mood : MonoBehaviour
{
    void Start()
    {
        InworldController.Instance.OnPacketReceived += OnPacketEvents;
    }

    void OnPacketEvents(InworldPacket packet)
    {
        switch (packet)
        {
            case EmotionEvent emotionEvent:
                HandleEmotion(emotionEvent.SpaffCode);
                break;
        }
    }

    public void HandleEmotion(Inworld.Grpc.EmotionEvent.Types.SpaffCode spaffcode)
    {
        switch (spaffcode)
        {
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Affection:
                print("haha");
                break;
        }
        print(spaffcode.ToString());
    }
}