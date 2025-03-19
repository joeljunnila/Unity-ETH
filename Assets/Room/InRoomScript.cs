using UnityEngine;

public class InRoomScript : MonoBehaviour {

    public GameObject floor;
    public int inRoom;
    public Light lightSource;

    public void OnTriggerEnter(Collider other) {
        inRoom = 1;
        lightSource.enabled = !lightSource.enabled; // Toggle light on/off
        Debug.Log(inRoom);
    }

    public void OnTriggerStay(Collider other)
    {
        inRoom = 1;
        lightSource.enabled = true;
    }

    public void OnTriggerExit(Collider other)
    {
        inRoom = 0;
        lightSource.enabled = !lightSource.enabled; // Toggle light on/off
    }
}