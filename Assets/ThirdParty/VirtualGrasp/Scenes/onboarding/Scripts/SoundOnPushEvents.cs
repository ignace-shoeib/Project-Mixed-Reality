using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;

public class SoundOnPushEvents : MonoBehaviour
{

    public AudioSource pushSoundEffect;

    // Start is called before the first frame update
    void Start()
    {
        VG_Controller.OnObjectPushed.AddListener(PlayPushSound);
    }

    public void PlayPushSound(VG_HandStatus hand)
    {
        if(hand.m_selectedObject != null && hand.m_selectedObject == transform)
            pushSoundEffect.Play();
    }
}
