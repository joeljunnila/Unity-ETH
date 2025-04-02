using UnityEngine;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Threading.Tasks;
using System.IO;

public class BlockchainDoor : MonoBehaviour
{
    private string rpcUrl = "http://127.0.0.1:8545"; // GoQuorum RPC URL
    private string contractAddress = "";

    private KeyInputHandler keyInputHandler;
    
    private Web3 web3;
    private string abi;

    void Start()
    {
        GetABI();
        web3 = new Web3(rpcUrl);
    }

    void GetABI()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/UserDevice.json");

        if (File.Exists(path))
        {
            abi = File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("Failed to load contract ABI file: " + path);
        }
    }

    public async Task<bool> CheckAccess(string playerAddress)
    {
        if (string.IsNullOrEmpty(playerAddress))
        {
            Debug.LogError("Player address is null or empty.");
            return false;
        }

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var canOpenFunction = contract.GetFunction("canOpenDoor");
        bool hasAccess = await canOpenFunction.CallAsync<bool>(playerAddress);
        return hasAccess;
    }

    public async void RequestDoorOpen(string playerAddress)
    {
        bool access = await CheckAccess(playerAddress);
        if (access)
        {
            Debug.Log("Access granted! Requesting door to open...");
            FindObjectOfType<DoorSigner>().SignDoorTransaction(playerAddress);
        }
        else
        {
            Debug.Log("Access Denied!");
        }
    }
}