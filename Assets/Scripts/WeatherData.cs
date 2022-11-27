using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WeatherData
{
    public string name;
    public ParticleSystem ParticleSystem;
    [HideInInspector] 
    public ParticleSystem.EmissionModule emission;

    public bool useAudio;
    public AudioClip weatherAudio;
    public float audioFadeTimer;
    
    public float lightIntencity;

    public float lightDimTimer;
    public float fogChangeSpeed;

    public Color fogColor;
    public Color currentForCollor;







}
