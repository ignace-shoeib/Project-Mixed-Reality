using UnityEngine;

public class EnableDisableObject : MonoBehaviour
{
    public void EnableDisable(GameObject gameObject)
    {
	    gameObject.SetActive(!gameObject.activeSelf);
    }
}
