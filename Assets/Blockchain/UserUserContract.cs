using UnityEngine;
using System.IO;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Threading.Tasks;

public class UserUserContract : MonoBehaviour
{
    private string rpcUrl = "http://localhost:8545";
    private string contractAddress = ""; // Contract address
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

    public async void Contract(string privateKey, string recipientAddress, decimal amount)
    {        
        try
        {
            // Set up the account and connect to Quorum
            var account = new Account(privateKey);
            var web3 = new Web3(account, rpcUrl);
            var contract = web3.Eth.GetContract(abi, contractAddress);

            // Convert amount to Wei
            var amountInWei = Web3.Convert.ToWei(amount, Nethereum.Util.UnitConversion.EthUnit.Ether);

            // Call the sendEther function on the contract
            var txHash = await contract.GetFunction("sendEther").SendTransactionAsync(
                from: account.Address,         
                gas: new HexBigInteger(500000),
                gasPrice: new HexBigInteger(0),
                value: new HexBigInteger(amountInWei),
                recipientAddress,
                amountInWei      
            );

            Debug.Log($"Transaction sent! Hash: {txHash}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Oops, something went wrong: {e.Message}");
        }
    }
}