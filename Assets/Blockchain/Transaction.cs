using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using UnityEngine;
using UnityEngine.UI;

public class Transaction : MonoBehaviour
{
    [SerializeField] private string privateKey1 = "0x8bbbb1b345af56b560a5b20bd4b0ed1cd8cc9958a16262bc75118453cb546df7"; // Sender private key (Quorum member 1) 0x8bbbb1b345af56b560a5b20bd4b0ed1cd8cc9958a16262bc75118453cb546df7
    [SerializeField] private string receiverAddress = "0xe090a28b8a9d0a69ec259cb745036d5d1030e3ea"; // Receiver address (Quorum member 2) 0xe090a28b8a9d0a69ec259cb745036d5d1030e3ea

    private Web3 web3;
    public Button transactionButton; // UI Button to trigger transaction

    private async void Start()
    {
        var chainId = 1337;
        var account = new Account(privateKey1, chainId);
        Debug.Log("Sender account: " + account.Address);

        web3 = new Web3(account, "http://localhost:8545");
        web3.TransactionManager.UseLegacyAsDefault = true;

        // Balance check before transaction
        var balance = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);
        Debug.Log("Receiver account balance before sending Ether: " + Web3.Convert.FromWei(balance.Value) + " Ether");

        // Assign button click event
        if (transactionButton != null)
        {
            transactionButton.onClick.AddListener(() => SendTransaction());
        }
    }

    private async void Update()
    {
        // Key to trigger the transaction
        if (Input.GetKeyDown(KeyCode.T))
        {
            await SendTransaction();
        }
    }

    private async Task SendTransaction()
    {
        Debug.Log("Attempting transaction...");
        try
        {
            // Transfer 1.11 Ether
            var transaction = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAddress, 1.11m);

            // Balance check after transaction
            var balance = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);
            Debug.Log("Receiver account balance after sending Ether: " + Web3.Convert.FromWei(balance.Value) + " Ether");
        }
        catch (Exception ex)
        {
            Debug.Log("Transaction failed: " + ex.Message);
        }
    }
}
