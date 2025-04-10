using UnityEngine;
using System.IO;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using System.Threading.Tasks;
using Nethereum.Contracts;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

public class UserUserContract : MonoBehaviour
{
    private string rpcUrl;
    private string contractAddress;
    private string abi;
    private Web3 web3;
    private Contract contract;

    [Event("TransferSigned")]
    public class TransferSignedEventDTO : IEventDTO
    {
        [Parameter("address", "receiver", 1, true)]
        public string Receiver { get; set; }

        [Parameter("uint256", "amount", 2, false)]
        public BigInteger Amount { get; set; }

        [Parameter("string", "message", 3, false)]
        public string Message { get; set; }
    }

    public delegate void TransferSignedHandler(string receiver, decimal amount, string message);
    public event TransferSignedHandler OnTransferSigned;

    async void Start()
    {
        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractUserUser;

        await GetABIAsync();
        InitializeWeb3AndContract();
        await SubscribeToTransferSignedEvent();
    }

    async Task GetABIAsync()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/UserUser.json");
        if (File.Exists(path))
        {
            abi = await Task.Run(() => File.ReadAllText(path));
        }
        else
        {
            Debug.LogError("Failed to load JSON file: " + path);
        }
    }

    private void InitializeWeb3AndContract() // Initializing the connection and contract
    {
        web3 = new Web3(rpcUrl);
        contract = web3.Eth.GetContract(abi, contractAddress);
    }

    public async Task<string> InitiateTransfer(string senderPrivateKey, string recipientAddress, decimal amount, string message) // Initializing the transaction details 
    {
        try
        {
            var account = new Account(senderPrivateKey);
            var web3Sender = new Web3(account, rpcUrl);
            var contractSender = web3Sender.Eth.GetContract(abi, contractAddress);
            var amountInWei = Web3.Convert.ToWei(amount);

            var txHash = await contractSender.GetFunction("initiateTransfer").SendTransactionAsync(
                account.Address, new HexBigInteger(500000), new HexBigInteger(0), new HexBigInteger(amountInWei),
                recipientAddress, amountInWei, message
            );

            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"Message sent with transfer: {message}");
            }
            return txHash;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initiating transfer: {e.Message}");
            return null;
        }
    }

    public async Task<string> SignTransfer(string recipientPrivateKey) // Function to sign the transaction
    {
        try
        {
            var account = new Account(recipientPrivateKey);
            var web3Recipient = new Web3(account, rpcUrl);
            var contractRecipient = web3Recipient.Eth.GetContract(abi, contractAddress);

            var txHash = await contractRecipient.GetFunction("signTransfer").SendTransactionAsync(
                account.Address, 
                new HexBigInteger(500000), 
                new HexBigInteger(0), 
                new HexBigInteger(0)
            );

            Debug.Log($"Sign attempt by {account.Address}! Hash: {txHash}");
            return txHash;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error signing transfer: {e.Message}");
            return null;
        }
    }

    private async Task SubscribeToTransferSignedEvent() // Transfer the signed event
    {
        if (contract == null)
        {
            Debug.LogError("Contract not initialized!");
            return;
        }
        
        var eventTransferSigned = contract.GetEvent("TransferSigned");
        var filterId = await eventTransferSigned.CreateFilterAsync();

        while (true)
        {
            try
            {
                var logs = await eventTransferSigned.GetFilterChangesAsync<TransferSignedEventDTO>(filterId);
                foreach (var evt in logs)
                {
                    string receiver = evt.Event.Receiver;
                    decimal amount = Web3.Convert.FromWei(evt.Event.Amount);
                    string message = evt.Event.Message;
                    OnTransferSigned?.Invoke(receiver, amount, message);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error fetching TransferSigned events: {e.Message}");
            }
            await Task.Delay(2000);
        }
    }
}