using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportationHandler : MonoBehaviour
{
    public GameObject player_rig;
    private bool is_aiming = false;
    private GameObject currentDestination;
    public GameObject destination;
    void Start()
    {
        currentDestination = Instantiate(destination, transform.position, Quaternion.identity);
    }
    public void SwitchAiming()
    {
        is_aiming = !is_aiming;

        if (!is_aiming)
        {
            currentDestination.SetActive(false);
        }
    }
    void Update()
    {
        if (is_aiming)
        {
            CheckForDestination();
        }
    }

    private void CheckForDestination()
    {
        Ray ray = new Ray(transform.position, transform.rotation * Vector3.up);

        RaycastHit hit;

        bool b = Physics.Raycast(ray, out hit, 5f, 1 << 9);
        if (b)
        {
            currentDestination.transform.position = hit.point;
            currentDestination.SetActive(true);
        }
    }

    public void Teleport()
    {
        if (is_aiming && currentDestination.activeSelf)
        {
            player_rig.transform.position = currentDestination.transform.position;
            currentDestination.SetActive(false);
        }
    }
}