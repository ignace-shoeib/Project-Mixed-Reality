using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;

public class SoundOnGraspEvents : MonoBehaviour
{

    public AudioSource graspSoundEffect;

    private int avatarID;

    private void Start()
    {
        avatarID = GetComponentInChildren<SkinnedMeshRenderer>().GetInstanceID();
        VG_Controller.OnGraspTriggered.AddListener(PlayGraspSound);

    }

    public void PlayGraspSound(VG_HandStatus hand)
    {
        if (hand.m_avatarID == avatarID)
            graspSoundEffect.Play();
    }
}
