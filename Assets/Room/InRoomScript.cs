using UnityEngine;

public class InRoomScript : MonoBehaviour {

    public GameObject floor;
    public int inRoom;
    [SerializeField]public Light lightSource;

    public void OnTriggerEnter(Collider other) {
        if (lightSource) {
            inRoom = 1;
            lightSource.enabled = !lightSource.enabled; // Toggle light on/off
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (lightSource) {
            inRoom = 1;
            lightSource.enabled = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (lightSource) {
            inRoom = 0;
            lightSource.enabled = !lightSource.enabled; // Toggle light on/off
        }    
    }
}