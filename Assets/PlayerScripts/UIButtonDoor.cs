using UnityEngine;
using UnityEngine.UI;  // Required for UI Button
using Unity.Netcode;

public class UIButtonDoor : NetworkBehaviour
{
    public Button openDoorButton;  // Reference to the UI button in the Inspector
    public Door door;  // Reference to the Door script in the Inspector

    void Start()
    {
        // Ensure the button is assigned
        if (openDoorButton != null)
        {
            openDoorButton.onClick.AddListener(OnButtonClicked);
        }
    }

    // Method called when the button is clicked
    void OnButtonClicked()
    {
        // Check if the door is ready to be opened (optional)
        if (door != null && door.IsPlayerNearby())
        {
            // Call the ServerRpc to open the door
            
            door.RequestToggleDoorServerRpc();
        }
        else
        {
            Debug.Log("Player is not close enough to the door.");
        }
    }
}