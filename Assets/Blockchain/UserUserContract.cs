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
    private BigInteger amountToSend = 1000000000000000000;
    private string abi;


    async void Start()
    {
        GetABI();
    }

    void GetABI()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/UserUserContract.json"); // Path to the contract ABI file

        if (File.Exists(path))
        {
            abi = File.ReadAllText(path);
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

            // Call the sendEther function on the contract
            var txHash = await contract.GetFunction("sendEther").SendTransactionAsync(
                from: account.Address,          // Sender (account with private key)
                gas: new HexBigInteger(300000), // Gas limit
                gasPrice: new HexBigInteger(0), // Gas price (0 for Quorum testnet)
                value: null,                    // No Ether sent with the call
                recipientAddress,               // Destination address
                amountToSend                    // Amount to send from contract (1 Ether)
            );

            Debug.Log($"Transaction sent! Hash: {txHash}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Oops, something went wrong: {e.Message}");
        }
    }
}