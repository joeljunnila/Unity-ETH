using UnityEngine;
using System.IO;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using System.Threading.Tasks;

public class UserUserContract : MonoBehaviour
{
    private string rpcUrl = "http://localhost:8545";
    private string contractAddress = "";
    private string abi;

    async void Start()
    {
        await GetABIAsync();
    }

    async Task GetABIAsync()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/UserUserContract.json");
        if (File.Exists(path))
        {
            abi = await Task.Run(() => File.ReadAllText(path));
        }
        else
        {
            Debug.LogError("Failed to load JSON file: " + path);
        }
    }

    // Sender initiates a transfer
    public async Task<string> InitiateTransfer(string senderPrivateKey, string recipientAddress, decimal amount)
    {
        try
        {
            var account = new Account(senderPrivateKey);
            var web3 = new Web3(account, rpcUrl);
            var contract = web3.Eth.GetContract(abi, contractAddress);

            var amountInWei = Web3.Convert.ToWei(amount, Nethereum.Util.UnitConversion.EthUnit.Ether);

            var txHash = await contract.GetFunction("initiateTransfer").SendTransactionAsync(
                from: account.Address,
                gas: new HexBigInteger(500000),
                gasPrice: new HexBigInteger(0),
                value: new HexBigInteger(amountInWei),
                recipientAddress,
                amountInWei
            );

            Debug.Log($"Transfer initiated! Hash: {txHash}");
            return txHash;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initiating transfer: {e.Message}");
            return null;
        }
    }

    // Sign transfer
    public async Task<string> SignTransfer(string recipientPrivateKey)
    {
        try
        {
            var account = new Account(recipientPrivateKey);
            var web3 = new Web3(account, rpcUrl);
            var contract = web3.Eth.GetContract(abi, contractAddress);

            var txHash = await contract.GetFunction("signTransfer").SendTransactionAsync(
                from: account.Address,
                gas: new HexBigInteger(500000),
                gasPrice: new HexBigInteger(0),
                value: new HexBigInteger(0)
            );

            return txHash;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error signing transfer: {e.Message}");
            return null;
        }
    }
}