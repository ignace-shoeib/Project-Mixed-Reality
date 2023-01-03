 using System;
 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSound : MonoBehaviour
{
	public AudioClip AudioClip;
	private bool audioPlayed = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (audioPlayed == false)
        {
            if (other.CompareTag("MainCamera")) 
            {
                audioPlayed = true;
                AudioSource audio = other.GetComponent<AudioSource>();
				audio.enabled = true;
				audio.clip = AudioClip;
				audio.Play();
            }
        }
    }
}