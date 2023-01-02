using Inworld;
using Inworld.Packets;
using UnityEngine;
public class Mood : MonoBehaviour
{
    public Weather_Controller WeatherController;
    public int MoodLevel = 0;
    public AudioSource AudioSource;
	public const int ThunderMood = -10;
    public const int RainMood = -5;
    public const int CloudyMood = 0;
    public const int SunMood = 5;
    public const int MaxMood = 10;
    public const int MinMood = -15;
    private Weather_Controller.WeatherType desiredWeather;
    
    void Start()
    {
        InworldController.Instance.OnPacketReceived += OnPacketEvents;
		desiredWeather = WeatherController.en_CurrWeather;
	}

    void OnPacketEvents(InworldPacket packet)
    {
		AudioSource.enabled = false;
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
				if (MoodLevel > MinMood)
					MoodLevel--;
                break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Anger:
				if (MoodLevel > MinMood)
					MoodLevel--;
				break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Affection:
				if (MoodLevel < MaxMood)
					MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Joy:
				if (MoodLevel < MaxMood)
					MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Humor:
				if (MoodLevel < MaxMood)
					MoodLevel++;
	            break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Interest:
				if (MoodLevel < MaxMood)
					MoodLevel++;
				break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Disgust:
				if (MoodLevel > MinMood)
					MoodLevel--;
				break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Tension:
				if (MoodLevel > MinMood)
					MoodLevel--;
				break;
            case Inworld.Grpc.EmotionEvent.Types.SpaffCode.Belligerence:
				if (MoodLevel > MinMood)
					MoodLevel--;
				break;
		}
    }

    public void Update()
    {
	    switch (MoodLevel)
	    {
		    case <= ThunderMood:
				desiredWeather = Weather_Controller.WeatherType.THUNDERSTORM;
				break;
		    case <= RainMood:
				desiredWeather = Weather_Controller.WeatherType.RAIN;
				break;
		    case <= CloudyMood:
				desiredWeather = Weather_Controller.WeatherType.CLOUDY;
				break;
		    case >= SunMood:
				desiredWeather = Weather_Controller.WeatherType.SUN;
				break;
	    }
		if (desiredWeather == WeatherController.en_CurrWeather)
			return;
		WeatherController.ExitCurrentWeather((int)desiredWeather);
    }
}