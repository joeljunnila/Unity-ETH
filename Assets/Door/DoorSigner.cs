using UnityEngine;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using System.IO;
using System.Threading.Tasks;

public class DoorSigner : MonoBehaviour
{
    private string rpcUrl;
    private string contractAddress;
    private string doorPrivateKey;
    [Tooltip("Is this for a physical door (true) or digital door (false)?")]
    [SerializeField] private bool isPhysicalDoor;
    private Web3 web3;
    private string abi;

    void Start()
    {

        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractUserDevice;
        doorPrivateKey = ConfigLoader.config.ethereum.doorPrivateKey;

        GetABI();
        web3 = new Web3(rpcUrl);
        web3.TransactionManager.UseLegacyAsDefault = true;

        BlockchainDoor parentDoor = GetComponent<BlockchainDoor>();
        if (parentDoor != null)
        {
            isPhysicalDoor = parentDoor.isPhysicalDoor;
            Debug.Log($"DoorSigner synchronized with door type: {(isPhysicalDoor ? "Physical" : "Digital")}");
        }
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

    public async void SignDoorTransaction(string playerAddress)
    {
        var account = new Account(doorPrivateKey);
        var web3WithAccount = new Web3(account, rpcUrl);
        web3WithAccount.TransactionManager.UseLegacyAsDefault = true;

        var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
        var openDoorFunction = contract.GetFunction("openDoor");

        var txHash = await openDoorFunction.SendTransactionAsync(
            account.Address,
            new HexBigInteger(3000000),
            new HexBigInteger(0),
            new HexBigInteger(0),
            playerAddress,
            isPhysicalDoor
        );

        Debug.Log("✅ Door opened! Transaction Hash: " + txHash);
    }
}