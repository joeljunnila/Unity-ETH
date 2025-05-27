using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLLoginManager : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void ConnectToMetamask();
    [DllImport("__Internal")] private static extern void SignMessage(string message);

    private string walletAddress;
    private string nonce;

    public void RequestLogin()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ConnectToMetamask();
#else
        Debug.Log("MetaMask toimii vain WebGL:ssä");
#endif
    }

    public void OnWalletConnected(string address)
    {
        walletAddress = address;
        Debug.Log("✅ Lompakko yhdistetty: " + walletAddress);

        // Luo nonce (haasteviesti)
        nonce = "UnityLogin_" + System.DateTime.UtcNow.Ticks;
        Debug.Log("🖊️ Allekirjoitetaan viesti: " + nonce);

#if UNITY_WEBGL && !UNITY_EDITOR
        SignMessage(nonce);
#endif
    }

    public void OnMessageSigned(string signature)
    {
        Debug.Log("🔏 Allekirjoitus vastaanotettu: " + signature);
        Debug.Log("✅ Käyttäjän identiteetti: " + walletAddress);
        
        // Tässä vaiheessa käyttäjä on kirjautunut vahvistetusti
        // Jos käytät backendiä, voit lähettää osoitteen + signature sinne
    }

    public void OnWalletError(string message)
    {
        Debug.LogError("❌ MetaMask virhe: " + message);
    }
}
