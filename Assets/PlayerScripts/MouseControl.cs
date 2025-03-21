using Unity.Netcode;
using UnityEngine;

public class MouseControl : NetworkBehaviour
{
    public KeyCode toggleKey = KeyCode.Tab;
    public static bool isCameraFrozen = false; // Flag to freeze camera movement

    void Update()
    {
        if (!IsOwner) return; // Ensure this only runs for the local player

        if (Input.GetKeyDown(toggleKey))
        {
            bool isCursorVisible = Cursor.visible;
            Cursor.visible = !isCursorVisible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

            isCameraFrozen = Cursor.visible; // Freeze camera when cursor is visible
        }
    }
}