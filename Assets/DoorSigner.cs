using UnityEngine;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using System.IO;
using System.Threading.Tasks;

public class DoorSigner : MonoBehaviour
{
    private string rpcUrl = "http://127.0.0.1:8545"; 
    private string contractAddress = "0x0556AD7EC9b57269b3124374D9719fB2f6482DdB";
    private string doorPrivateKey = "f2af258ee3733513333652be19197ae7eace4b5e79a346cf25b02a857e6043f3"; // The door’s blockchain private key
    private Web3 web3;
    private string abi;

    void Start()
    {
        GetABI();
        web3 = new Web3(rpcUrl);
        web3.TransactionManager.UseLegacyAsDefault = true; // ✅ Ensure Legacy Transactions
    }

    void GetABI()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/UserDevice.json");  // Path to contract ABI file

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

        web3WithAccount.TransactionManager.UseLegacyAsDefault = true; // ✅ Ensure Legacy Transactions

        var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
        var openDoorFunction = contract.GetFunction("openDoor");

        var gasPrice = new HexBigInteger(0);  // ✅ Force zero gas price
        var gasLimit = new HexBigInteger(3000000);
        var value = new HexBigInteger(0);
        var nonce = await web3WithAccount.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address, BlockParameter.CreatePending());

        // ✅ Create a raw transaction manually with zero gas price
        var transactionInput = new TransactionInput
        {
            From = account.Address,
            To = contractAddress,
            Gas = gasLimit,
            GasPrice = gasPrice,  // ✅ Ensure zero gas price
            Value = value,
            Nonce = nonce
        };

        // ✅ Manually sign the transaction
        string signedTransaction = await web3WithAccount.TransactionManager.SignTransactionAsync(transactionInput);

        // ✅ Send the signed transaction
        string transactionHash = await web3WithAccount.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedTransaction);

        Debug.Log("✅ Door opened! Transaction Hash: " + transactionHash);
    }
}