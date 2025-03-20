using UnityEngine;
using TMPro;

public class MetaMaskConnect : MonoBehaviour
{
    public TMP_Text addressText; // Assign UI Text (TextMeshPro) in Inspector

    void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            ConnectToMetaMask();
        }
        else
        {
            addressText.text = "Run in WebGL to Connect MetaMask";
        }
    }

    public void ConnectToMetaMask()
    {
        Application.ExternalEval("ConnectMetaMask()");
    }

    // This method is called from JavaScript
    public void ReceiveAccount(string account)
    {
        if (account.StartsWith("Error"))
        {
            addressText.text = account; // Show error message
        }
        else
        {
            addressText.text = "Wallet: " + account;
        }
    }
}