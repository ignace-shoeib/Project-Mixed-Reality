 using System;
 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSound : MonoBehaviour
{
    public bool audioPlayed = false;
    
    private void OnTriggerEnter(Collider other)
    {
       print(other.tag);
        if (audioPlayed == false)
        {
            if (other.CompareTag("MainCamera")) 
            {
                AudioSource audio = GetComponent<AudioSource>();
                audio.Play();
            }
            
          
        }



    }
}