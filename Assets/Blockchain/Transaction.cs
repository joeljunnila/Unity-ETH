using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using UnityEngine;

public class Transaction : MonoBehaviour
{
    private Web3 web3;

    public async void ExecuteTransaction(string senderPrivateKey, string receiverPublicKey, decimal amount)
    {
        try
        {
            Debug.Log($"Attempting to send {amount} ETH to {receiverPublicKey}");

            if (senderPrivateKey.Length != 66 || !senderPrivateKey.StartsWith("0x"))
            {
                Debug.LogError("Invalid sender private key.");
                return;
            }

            var chainId = 1337;
            var account = new Account(senderPrivateKey, chainId);
            

            if (string.IsNullOrEmpty(receiverPublicKey) || receiverPublicKey.Length != 42 || !receiverPublicKey.StartsWith("0x"))
            {
                Debug.LogError("Invalid receiver address.");
                return;
            }

            web3 = new Web3(account, "http://localhost:8545");
            web3.TransactionManager.UseLegacyAsDefault = true;

            var balanceBefore = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            Debug.Log($"Sender balance before: {Web3.Convert.FromWei(balanceBefore.Value)} ETH");

            string receiverAddress = receiverPublicKey;

            var balanceReceiverBefore = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);
            Debug.Log($"Receiver balance before: {Web3.Convert.FromWei(balanceReceiverBefore.Value)} ETH");

            var transaction = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAddress, amount);

            Debug.Log($"Transaction completed. Hash: {transaction.TransactionHash}");

            var balanceReceiverAfter = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);
            Debug.Log($"Receiver balance after: {Web3.Convert.FromWei(balanceReceiverAfter.Value)} ETH");

            var balanceSenderAfter = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            Debug.Log($"Sender balance after: {Web3.Convert.FromWei(balanceSenderAfter.Value)} ETH");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Transaction failed: {ex.Message}");
            Debug.LogError($"Stack Trace: {ex.StackTrace}");
        }
    }
}