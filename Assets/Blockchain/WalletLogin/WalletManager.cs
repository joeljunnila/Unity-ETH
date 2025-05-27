using UnityEngine;
using TMPro;
using Reown.AppKit.Unity;
using Nethereum.Web3;
using System.Threading.Tasks;

public class WalletManager : MonoBehaviour
{
    [Header("UI-komponentit")]
    public TextMeshProUGUI walletText;
    public TextMeshProUGUI balanceText;

    private string rpcUrl; 

    private async void Start()
    {
        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;

        while (!AppKit.IsInitialized)
        {
            await Task.Delay(100);
        }

        Debug.Log("✅ AppKit alustettu");

        if (await IsWalletConnected())
        {
            await ShowWalletInfo();
        }
        else
        {
            walletText.text = "Wallet not connected.";
            balanceText.text = "";
        }
    }

    public async void OpenWalletConnectModal()
    {
        if (!AppKit.IsInitialized)
        {
            Debug.LogWarning("⚠️ AppKit ei ole alustettu vielä.");
            return;
        }

        Debug.Log("🔄 Näytetään QR-koodi");
        AppKit.OpenModal();

        bool connected = await WaitForWalletConnection();
        if (connected)
        {
            await ShowWalletInfo();
        }
    }

    private async Task ShowWalletInfo()
    {
        var account = await AppKit.GetAccountAsync();
        string address = account.Address;

        walletText.text = $"Address:\n{Shorten(address)}";

        string balance = await GetEthBalanceAsync(address);
        balanceText.text = $"Balance:\n{balance} ETH";

        Debug.Log($"✅ Osoite: {address} | Saldo: {balance} ETH");
    }

    private async Task<string> GetEthBalanceAsync(string address)
    {
        try
        {
            var web3 = new Web3(rpcUrl);
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            var balanceEth = Web3.Convert.FromWei(balanceWei);
            return balanceEth.ToString("F4");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Saldoa ei saatu: " + ex.Message);
            return "0.0000";
        }
    }

    private async Task<bool> IsWalletConnected()
    {
        try
        {
            var account = await AppKit.GetAccountAsync();
            return account != null && !string.IsNullOrEmpty(account.Address);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitForWalletConnection(int timeoutSeconds = 30)
    {
        int waited = 0;
        while (!await IsWalletConnected())
        {
            await Task.Delay(1000);
            waited++;
            if (waited >= timeoutSeconds)
            {
                Debug.LogWarning("⏱️ Wallet connect timeout.");
                return false;
            }
        }
        return true;
    }

    private string Shorten(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 10)
            return address;
        return address.Substring(0, 6) + "..." + address.Substring(address.Length - 4);
    }
}
