using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : NetworkBehaviour
{
    public GameObject uiRoot; // The canvas or UI parent
    public Button stopHostButton;
    public Button stopClientButton;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Hide UI for other players
            uiRoot.SetActive(false);
            return;
        }

        // Show only relevant button based on role
        if (IsServer && IsClient)
        {
            stopHostButton.gameObject.SetActive(true);
            stopClientButton.gameObject.SetActive(false);
        }
        else if (IsClient)
        {
            stopClientButton.gameObject.SetActive(true);
            stopHostButton.gameObject.SetActive(false);
        }

        // Assign button listeners
        stopHostButton.onClick.AddListener(() =>
        {
            Debug.Log("Stopping Host...");
            NetworkManager.Singleton.Shutdown();
        });

        stopClientButton.onClick.AddListener(() =>
        {
            Debug.Log("Stopping Client...");
            NetworkManager.Singleton.Shutdown();
        });
    }
}