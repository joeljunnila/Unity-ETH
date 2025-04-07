using UnityEngine;
using System;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using Unity.Netcode;
using Nethereum.Hex.HexTypes;
using System.Collections.Generic;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.FunctionEncoding.Attributes;

public class BlockchainDoor : NetworkBehaviour
{
    private string rpcUrl = "http://127.0.0.1:8545";
    private string contractAddress = "";
    private string ownerPrivateKey;
    private Web3 web3;
    private string abi;

    public float openAngle = 90f;
    public float openSpeed = 2f;
    public float interactionRange = 2f;

    public Coroutine doorAnimationCoroutine;

    public UnityEngine.Quaternion closedRotation;
    public UnityEngine.Quaternion openRotation;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        GetABI();
        web3 = new Web3(rpcUrl);

        closedRotation = transform.rotation;
        openRotation = UnityEngine.Quaternion.Euler(transform.eulerAngles + new UnityEngine.Vector3(0, openAngle, 0));

        // Sync door state when it changes
        isOpen.OnValueChanged += (oldValue, newValue) => StartDoorAnimation(newValue);
    }

    public void updateOwnerPrivateKey(string privateKey)
    {
        ownerPrivateKey = privateKey;
        Debug.Log("Owner private key updated: " + ownerPrivateKey);
        var account = new Account(ownerPrivateKey);
        web3 = new Web3(account, rpcUrl);
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

    public bool IsPlayerNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player")) // Ensure player objects have the "Player" tag
            {
                return true;
            }
        }
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDoorOpenServerRpc(string playerAddress)
    {
        if (!IsPlayerNearby()) return; // Prevent toggling if no player is close
        StartCoroutine(CheckAccessAndToggleDoor(playerAddress)); // Call separate async method
    }

    private IEnumerator CheckAccessAndToggleDoor(string playerAddress)
    {
        var task = CheckAccess(playerAddress);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result) // If access is granted
        {
            Debug.Log("Access granted! Toggling door state...");
            isOpen.Value = !isOpen.Value;  // ✅ Update the NetworkVariable
        }
        else
        {
            Debug.Log("Access Denied!");
        }
}

    public void StartDoorAnimation(bool open)
    {
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }
        doorAnimationCoroutine = StartCoroutine(AnimateDoor(open));
    }

    public IEnumerator AnimateDoor(bool open)
    {
        UnityEngine.Quaternion targetRotation = open ? openRotation : closedRotation;
        while (UnityEngine.Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = UnityEngine.Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }
        transform.rotation = targetRotation; // Ensure exact final rotation
    }

    public async Task GrantAccess(string userAddress, string privateKey)
    {
        if (string.IsNullOrEmpty(userAddress) || userAddress.Length != 42 || !userAddress.StartsWith("0x"))
        {
            Debug.LogError("Invalid Ethereum address: " + userAddress);
            return;
        }

        try
        {
            var account = new Account(privateKey);
            var web3WithAccount = new Web3(account, rpcUrl);
            var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
            var grantAccessFunction = contract.GetFunction("grantAccess");

            Debug.Log($"Granting access to: {userAddress}");
            var txHash = await grantAccessFunction.SendTransactionAsync(
                account.Address,
                new HexBigInteger(300000),
                new HexBigInteger(0),
                new HexBigInteger(0),
                userAddress
            );

            TransactionReceipt receipt = null;
            while (receipt == null)
            {
                await Task.Delay(1000);
                receipt = await web3WithAccount.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            }

            if (receipt.Status.Value == 1)
            {
                Debug.Log("Access granted successfully to " + userAddress);
            }
            else
            {
                Debug.LogError("Failed to grant access: " + receipt.Status.Value);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error granting access: {ex.Message}");
        }
    }

    public async Task RevokeAccess(string userAddress, string privateKey)
    {
        if (string.IsNullOrEmpty(userAddress) || userAddress.Length != 42 || !userAddress.StartsWith("0x"))
        {
            Debug.LogError("Invalid Ethereum address: " + userAddress);
            return;
        }

        try
        {
            var account = new Account(privateKey);
            var web3WithAccount = new Web3(account, rpcUrl);
            var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
            var revokeAccessFunction = contract.GetFunction("revokeAccess");

            Debug.Log($"Revoking access for: {userAddress}");
            var txHash = await revokeAccessFunction.SendTransactionAsync(
                account.Address,
                new HexBigInteger(300000),
                new HexBigInteger(0),
                new HexBigInteger(0),
                userAddress
            );

            TransactionReceipt receipt = null;
            while (receipt == null)
            {
                await Task.Delay(1000);
                receipt = await web3WithAccount.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            }

            if (receipt.Status.Value == 1)
            {
                Debug.Log("Access revoked successfully for " + userAddress);
            }
            else
            {
                Debug.LogError("Transaction failed with status: " + receipt.Status.Value);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error revoking access: {ex.Message}");
        }
    }

    public async Task<List<string>> GetAccessList(string ownerAddress)
    {
        try
        {
            var handler = web3.Eth.GetContractQueryHandler<GetAccessListFunction>();
            var result = await handler.QueryAsync<GetAccessListOutputDTO>(contractAddress, new GetAccessListFunction { FromAddress = ownerAddress });
            
            if (result.AccessList != null && result.AccessList.Count > 0)
            {
                Debug.Log("Access List:");
                foreach (var address in result.AccessList)
                {
                    Debug.Log(address);
                }
            }
            else
            {
                Debug.Log("No addresses have access.");
            }

            return result.AccessList;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error getting access list: " + ex.Message);
            return null;
        }
    }


    [Function("getAccessList", typeof(GetAccessListOutputDTO))]
    public class GetAccessListFunction : FunctionMessage {}

    [FunctionOutput]
    public class GetAccessListOutputDTO : IFunctionOutputDTO
    {
        [Parameter("address[]", 0)] public List<string> AccessList { get; set; }
    }
}