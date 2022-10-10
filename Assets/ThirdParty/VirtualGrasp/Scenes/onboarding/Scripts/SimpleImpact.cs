using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleImpact : MonoBehaviour
{
    public AudioSource collisionEffect;

    private float unityVelocityThreshold = 1.0F;
    private float calculatedVelocityThreshold = .1F;


    ArticulationBody ownAB = null;
    Rigidbody ownRB = null;

    private void Start()
    {
        gameObject.TryGetComponent<Rigidbody>(out ownRB);
        gameObject.TryGetComponent<ArticulationBody>(out ownAB);
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.relativeVelocity.magnitude > 0) // sometimes we're lucky. unity calculates speed for us
        {
            if(collision.relativeVelocity.magnitude > unityVelocityThreshold)
                collisionEffect.Play();
        } else if (collision.body != null && (ownRB != null || ownAB != null)){ // sometimes we're not lucky, have to calculate

            Vector3 ownSpeed = ownRB != null ? ownRB.velocity : ownAB.velocity;
            Vector3 colliderSpeed = collision.rigidbody != null ? collision.rigidbody.velocity : collision.articulationBody.velocity;

            if((ownSpeed - colliderSpeed).magnitude > calculatedVelocityThreshold)
                collisionEffect.Play();
        }
    }
}
