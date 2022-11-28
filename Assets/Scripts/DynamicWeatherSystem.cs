using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public enum weatherState { change, Sun, Rain, Thunder, Mist, Snow }

[RequireComponent(typeof(AudioSource))]
public class DynamicWeatherSystem : MonoBehaviour
{
    public float switchTimer = 0;
    public float resetTimer = 3600f;
    public float minLightIntencity = 0f;
    public float maxLightIntencity = 1f;
    private int switchWeather;
    
    public AudioSource audioSource;
    public Light sunLight;
    
    public Transform windzone;

    public weatherState weatherState;

    public WeatherData[] weatherData;

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadWeatherSystem();
        StartCoroutine(StartDynamicWeather());
    }

    void LoadWeatherSystem()
    {
        for (int i = 0; i < weatherData.Length; i++)
        {
            weatherData[i].emission = weatherData[i].ParticleSystem.emission;
        }

        switchTimer = resetTimer;
    }

    private void FixedUpdate()
    {
        switchTimer -= Time.deltaTime;
        if (switchTimer > 0) return;
        if (switchTimer <= 0)
        {
            switchTimer = 0;
            weatherState = weatherState.change;
            switchTimer = resetTimer;
        }
    }
    // ReSharper disable Unity.PerformanceAnalysis
    IEnumerator StartDynamicWeather()
    {
        if (weatherState == weatherState.change)
            selectWeather();
        else if (weatherState == weatherState.Sun)
            activeWeather("Sun");
        else if (weatherState == weatherState.Rain)
            activeWeather("Rain");
        else if (weatherState == weatherState.Thunder)
            activeWeather("Thunder");

        yield return null;
    }

    void selectWeather()
    {
        
        // hier emoties
        switchWeather = Random.Range(0, System.Enum.GetValues(typeof(WeatherData)).Length);
        resetWeather();
        if (switchWeather == 0)
            weatherState = weatherState.change;
        else if (switchWeather == 1)
            weatherState = weatherState.Sun;
        else if (switchWeather == 2)
            weatherState = weatherState.Rain;
        else if (switchWeather == 3)
            weatherState = weatherState.Thunder;
    }

    void activeWeather(string weather)
    {
        if (weatherData.Length > 0)
        {
            for (int i = 0; i < weatherData.Length; i++)
            {
                if (weatherData[i].ParticleSystem != null)
                {
                    if (weatherData[i].name == weather)
                    {
                        weatherData[i].emission.enabled = true;
                        weatherData[i].fogColor = RenderSettings.fogColor;
                        RenderSettings.fogColor = Color.Lerp(weatherData[i].currentForCollor, weatherData[i].fogColor,
                            weatherData[i].fogChangeSpeed * Time.deltaTime);
                        changeWeatherSettings(weatherData[i].lightIntencity, weatherData[i].weatherAudio);
                    }
                }
            }
        }
    }
    void changeWeatherSettings(float lightIntencity, AudioClip audioClip)
    {
        Light tmpLight = GetComponent<Light>();
        if (tmpLight.intensity > maxLightIntencity)
            tmpLight.intensity -= Time.deltaTime * lightIntencity;
        if (tmpLight.intensity < maxLightIntencity)
            tmpLight.intensity += Time.deltaTime * lightIntencity;
        if (weatherData[switchWeather].useAudio == true)
        {
            AudioSource tmpAudio = GetComponent<AudioSource>();
            if (tmpAudio.volume > 0 && tmpAudio.clip != audioClip)
            {
                tmpAudio.volume -= Time.deltaTime * weatherData[switchWeather].audioFadeTimer;
            }

            if (tmpAudio.volume == 0 )
            {
                tmpAudio.Stop();
                tmpAudio.clip = audioClip;
                tmpAudio.loop = true;
                tmpAudio.Play();
            }

            if (tmpAudio.volume < 1 && tmpAudio.clip != audioClip)
            {
                tmpAudio.volume -= Time.deltaTime * weatherData[switchWeather].audioFadeTimer;
            }
        }
    }
    
    void resetWeather()
    {
        if (weatherData.Length > 0)
        {
            for (int i = 0; i < weatherData.Length; i++)
            {
                if (weatherData[i].emission.enabled != false)
                {
                    weatherData[i].emission.enabled = false;
                }
            }
        }
           
    }
    
}
