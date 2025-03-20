using UnityEngine;

public class MouseControl : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Tab;  // Set the key to toggle mouse visibility

    void Update()
    {
        // If the toggle key is pressed
        if (Input.GetKeyDown(toggleKey))
        {
            // Toggle the cursor visibility
            bool isCursorVisible = Cursor.visible;
            Cursor.visible = !isCursorVisible;
        

            // Lock/Unlock cursor to the screen depending on visibility
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}