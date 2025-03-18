using UnityEngine;

public class Door : MonoBehaviour
{

    public float openAngle = 90f;
    public float openSpeed = 2f;
    public KeyCode interactKey = KeyCode.E;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void Update()
    {

        if (Input.GetKeyDown(interactKey))
        {
            ToggleDoor();
        }

        if (isOpen)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, openRotation, Time.deltaTime * openSpeed);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, closedRotation, Time.deltaTime * openSpeed);
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}