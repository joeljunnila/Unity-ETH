using Unity.Netcode;
using UnityEngine;


public class NetworkUIManager : MonoBehaviour
{
    public GameObject UIRoot;

    public void StartHost()
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
        UIRoot.SetActive(false);
    }

    public void StartClient()
    {
        Debug.Log("Starting Client...");
        NetworkManager.Singleton.StartClient();
        UIRoot.SetActive(false);
    }

    public void StartServer()
    {
        Debug.Log("Starting Server...");
        NetworkManager.Singleton.StartServer();
    }
    public void StopServer()
    {
        Debug.Log("Stopping Server...");
        NetworkManager.Singleton.Shutdown();
        UIRoot.SetActive(true);
    }
}