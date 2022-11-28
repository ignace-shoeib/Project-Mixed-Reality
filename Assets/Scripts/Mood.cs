using System.Collections;
using System.Collections.Generic;
using Inworld;
using Inworld.Packets;
using UnityEngine;
public class Mood : MonoBehaviour
{
    public Weather_Controller WeatherController;
    public int MoodLevel = 0;
    private Weather_Controller.WeatherType desiredWeather;
    
    void Start()
    {
        InworldController.Instance.OnPacketReceived += OnPacketEvents;
		desiredWeather = WeatherController.en_CurrWeather;
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
        print(spaffcode.ToString());
        switch (spaffcode)
        {
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Sadness:
	            MoodLevel--;
                break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Anger:
	            MoodLevel--;
                break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Affection:
	            MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Joy:
	            MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Humor:
	            MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Interest:
	            MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Disgust:
	            MoodLevel--;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Tension:
	            MoodLevel--;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Belligerence:
	            MoodLevel--;
	            break;
		}
    }

    public void Update()
    {
	    switch (MoodLevel)
	    {
		    case <= -30:
				desiredWeather = Weather_Controller.WeatherType.THUNDERSTORM;
				break;
		    case <= -15:
				desiredWeather = Weather_Controller.WeatherType.RAIN;
				break;
		    case <= 0:
				desiredWeather = Weather_Controller.WeatherType.CLOUDY;
				break;
		    case >= 15:
				desiredWeather = Weather_Controller.WeatherType.SUN;
				break;
	    }
		if (desiredWeather == WeatherController.en_CurrWeather)
			return;
		WeatherController.ExitCurrentWeather((int)desiredWeather);
    }
}