using System.Collections;
using System.Collections.Generic;
using Inworld;
using Inworld.Packets;
using UnityEngine;
public class Mood : MonoBehaviour
{
    public Weather_Controller _weatherController;
    
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
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Sadness:
                _weatherController.ChangeWeatherToRain();
                print("im so sad");
                break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Anger:
                _weatherController.ChangeWeatherToRain();
                print("Anger issue");
                break;
        }
        print(spaffcode.ToString());
    }
}