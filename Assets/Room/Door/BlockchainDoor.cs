using UnityEngine;
using System;
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

public class BlockchainDoor : NetworkBehaviour
{
    private string rpcUrl;
    private string contractAddress;
    private string ownerPrivateKey;
    private Web3 web3;
    private string abi;

    public float openAngle = 90f;
    public float openSpeed = 2f;
    public float interactionRange = 2f;
    [SerializeField] private DoorSigner doorSigner;

    public enum DoorType
    {
        Physical,
        Digital,
        Admin
    }

    [Tooltip("Type of this door (Physical, Digital, Admin Room)")]
    public DoorType doorType = DoorType.Physical;
    
    [Tooltip("Door identifier for debugging")]
    public string doorName = "Door";

    [Tooltip("Unique identifier for this door instance (used in contract calls)")]
    public int doorId;

    public Coroutine doorAnimationCoroutine;

    public UnityEngine.Quaternion closedRotation;
    public UnityEngine.Quaternion openRotation;

    public enum AccessRole
    {
        None = 0,
        Default = 1,
        Service = 2,
        Admin = 3
    }

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractUserDevice;

        GetABI();
        web3 = new Web3(rpcUrl);

        closedRotation = transform.rotation;
        openRotation = UnityEngine.Quaternion.Euler(transform.eulerAngles + new UnityEngine.Vector3(0, openAngle, 0));

        isOpen.OnValueChanged += (oldValue, newValue) => StartDoorAnimation(newValue);

        if (doorSigner == null)
        {
            doorSigner = GetComponent<DoorSigner>();
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

    public virtual async Task<bool> CheckAccess(string playerAddress)
    {
        if (string.IsNullOrEmpty(playerAddress))
        {
            Debug.LogError("Player address is null or empty.");
            return false;
        }

        var contract = web3.Eth.GetContract(abi, contractAddress);

        bool isAdmin = (doorType == DoorType.Admin);
        bool isPhysical = (doorType == DoorType.Physical);

        if (isAdmin)
        {
            var adminRoomCheckFunction = contract.GetFunction("canEnterAdminRoom");
            bool hasAdminRoomAccess = await adminRoomCheckFunction.CallAsync<bool>(playerAddress);

            if (!hasAdminRoomAccess)
            {
                Debug.Log($"Admin room access denied for address: {playerAddress}");
                return false;
            }
            // For admin, no further door type check needed (or add if needed)
            return true;
        }
        else
        {
            var checkFunction = isPhysical ?
                contract.GetFunction("canOpenPhysicalDoor") :
                contract.GetFunction("canOpenDigitalDoor");

            bool hasAccess = await checkFunction.CallAsync<bool>(playerAddress);
            return hasAccess;
        }
    }

    public async Task<bool> CheckAdminRoomAccess(string playerAddress)
    {
        if (string.IsNullOrEmpty(playerAddress))
        {
            Debug.LogError("Player address is null or empty.");
            return false;
        }

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var checkFunction = contract.GetFunction("canEnterAdminRoom");
        bool hasAccess = await checkFunction.CallAsync<bool>(playerAddress);      
        return hasAccess;
    }

    public bool IsPlayerNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDoorOpenServerRpc(string playerAddress)
    {
        if (!IsPlayerNearby()) return;
        StartCoroutine(CheckAccessAndToggleDoor(playerAddress));
    }

    private IEnumerator CheckAccessAndToggleDoor(string playerAddress)
    {
        var task = CheckAccess(playerAddress);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.Result)
        {
            string doorTypeDesc = doorType.ToString();
            Debug.Log($"Access granted to {doorTypeDesc} {doorName}! Toggling door state...");
            isOpen.Value = !isOpen.Value;

            if (doorSigner != null)
            {
                Debug.Log($"Attempting to record door access by {playerAddress} to blockchain...");
                doorSigner.SignDoorTransaction(playerAddress);
            }
            else
            {
                Debug.LogError($"Cannot record access: doorSigner is null on {doorName}");
            }
        }
        else
        {
            string doorTypeDesc = doorType.ToString();
            Debug.Log($"Access Denied to {doorTypeDesc} {doorName}!");
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
        transform.rotation = targetRotation;
    }    

    public async Task GrantAccess(
        string userAddress, 
        string privateKey, 
        AccessRole role = AccessRole.Default, 
        bool grantPhysicalAccess = true, 
        bool grantDigitalAccess = true, 
        bool grantAdminRoomAccess = false,
        uint expirationMinutes = 0)
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
            web3WithAccount.TransactionManager.UseLegacyAsDefault = true;
            
            var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
            
            string callerAddress = account.Address;
            Debug.Log($"Calling from address: {callerAddress}");
            
            var ownerFunction = contract.GetFunction("owner");
            string ownerAddress = await ownerFunction.CallAsync<string>();
            bool isOwner = callerAddress.ToLowerInvariant() == ownerAddress.ToLowerInvariant();
            
            var accessListFunction = contract.GetFunction("accessList");
            var callerInfo = await accessListFunction.CallDeserializingToObjectAsync<AccessInfoDTO>(callerAddress);
              Debug.Log($"Caller role: {(AccessRole)callerInfo.Role}, Physical: {callerInfo.HasPhysicalAccess}, " +
                     $"Digital: {callerInfo.HasDigitalAccess}, Admin Room: {callerInfo.HasAdminRoomAccess}, " +
                     $"Expiration: {(callerInfo.AccessExpiration == 0 ? "Never" : DateTime.UnixEpoch.AddSeconds(callerInfo.AccessExpiration).ToString())}");
            
            var grantAccessFunction = contract.GetFunction("grantAccess");
            
            uint expirationTimestamp = 0;
            if (expirationMinutes > 0)
            {
                expirationTimestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (expirationMinutes * 60);
            }            
            try {
                await grantAccessFunction.CallAsync<object>(
                    userAddress,      
                    (byte)role,
                    grantPhysicalAccess,
                    grantDigitalAccess,
                    grantAdminRoomAccess,
                    expirationTimestamp,
                    new { from = account.Address }
                );
                
                Debug.Log("Call simulation succeeded. Proceeding with transaction.");
            }
            catch (Exception callEx)
            {
                Debug.LogError($"Call simulation failed with reason: {callEx.Message}");
                if (callEx.Message.Contains("Only admin"))
                {
                    Debug.LogError("Error: Your account does not have admin privileges");
                    return;
                }
                if (callEx.Message.Contains("Only owner"))
                {
                    Debug.LogError("Error: Only the owner can grant admin role");
                    return;
                }
            }
            
            Debug.Log($"Sending grant access transaction...");
              var txHash = await grantAccessFunction.SendTransactionAsync(
                account.Address,
                new HexBigInteger(800000),
                new HexBigInteger(0),
                new HexBigInteger(0),
                userAddress,
                (byte)role,
                grantPhysicalAccess,
                grantDigitalAccess,
                grantAdminRoomAccess,
                expirationTimestamp
            );

            TransactionReceipt receipt = null;
            int retryCount = 0;
            while (receipt == null && retryCount < 10)
            {
                await Task.Delay(1000);
                try 
                {
                    receipt = await web3WithAccount.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                    retryCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Attempt {retryCount}: Failed to get receipt: {ex.Message}");
                    if (retryCount >= 10)
                        throw;
                }
            }

            if (receipt != null && receipt.Status.Value == 1)
            {
                Debug.Log("Access granted successfully to " + userAddress);
            }
            else if (receipt != null)
            {
                Debug.LogError($"Failed to grant access: Status {receipt.Status.Value}, Gas Used: {receipt.GasUsed.Value}");
            }
            else
            {
                Debug.LogError("Failed to get transaction receipt after multiple attempts");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error granting access: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
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

    [Function("getAccessList", typeof(GetAccessListOutputDTO))]
    public class GetAccessListFunction : FunctionMessage {}

    [FunctionOutput]
    public class GetAccessListOutputDTO : IFunctionOutputDTO
    {
        [Parameter("address[]", 0)] public List<string> AccessList { get; set; }
    }

    public async Task<List<AccessDetails>> GetAccessList(string privateKey)
    {
        List<AccessDetails> accessDetailsList = new List<AccessDetails>();
    
        try
        {
            var account = new Account(privateKey);
            Debug.Log($"Getting access list as: {account.Address}");
    
            var web3WithAccount = new Web3(account, rpcUrl);
            web3WithAccount.TransactionManager.UseLegacyAsDefault = true;
    
            var contract = web3WithAccount.Eth.GetContract(abi, contractAddress);
            var getAccessListFunction = contract.GetFunction("getAccessList");
    
            Debug.Log("Calling getAccessList function...");
    
            var result = await getAccessListFunction.CallAsync<List<string>>();
    
            Debug.Log($"Access list retrieved, contains {(result != null ? result.Count : 0)} addresses");
            
            if (result == null || result.Count == 0)
            {
                Debug.Log("No addresses have access.");
                return accessDetailsList;
            }
               
            Debug.Log("Processing Access List:");
            foreach (var address in result)
            {
                try
                {
                    var accessInfoFunction = contract.GetFunction("accessList");
                    var accessInfo = await accessInfoFunction.CallDeserializingToObjectAsync<AccessInfoDTO>(address);
                      var details = new AccessDetails
                    {
                        Address = address,
                        Role = (AccessRole)accessInfo.Role,
                        HasPhysicalAccess = accessInfo.HasPhysicalAccess,
                        HasDigitalAccess = accessInfo.HasDigitalAccess,
                        HasAdminRoomAccess = accessInfo.HasAdminRoomAccess,
                        ExpirationTime = accessInfo.AccessExpiration
                    };
                    
                    accessDetailsList.Add(details);
                      Debug.Log($"Address: {details.Address}, " +
                             $"Role: {details.Role}, " +
                             $"Physical: {details.HasPhysicalAccess}, " +
                             $"Digital: {details.HasDigitalAccess}, " +
                             $"Admin Room: {details.HasAdminRoomAccess}, " +
                             $"Expires: {(details.ExpirationTime == 0 ? "Never" : DateTime.UnixEpoch.AddSeconds(details.ExpirationTime).ToString())}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing address {address}: {ex.Message}");
                }
            }
            
            return accessDetailsList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get access list: {ex.Message}");
            if (ex.InnerException != null)
                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            return accessDetailsList;
        }
    }    

    public class AccessDetails
    {
        public string Address { get; set; }
        public AccessRole Role { get; set; }
        public bool HasPhysicalAccess { get; set; }
        public bool HasDigitalAccess { get; set; }
        public bool HasAdminRoomAccess { get; set; }
        public uint ExpirationTime { get; set; }
    }

    [FunctionOutput]
    public class AccessInfoDTO : IFunctionOutputDTO
    {
        [Parameter("uint8", "role", 0)] 
        public byte Role { get; set; }
        
        [Parameter("bool", "hasPhysicalAccess", 1)] 
        public bool HasPhysicalAccess { get; set; }
        
        [Parameter("bool", "hasDigitalAccess", 2)] 
        public bool HasDigitalAccess { get; set; }
        
        [Parameter("bool", "hasAdminRoomAccess", 3)] 
        public bool HasAdminRoomAccess { get; set; }
        
        [Parameter("uint256", "accessExpiration", 4)] 
        public uint AccessExpiration { get; set; }
    }    

    public string GetDoorTypeString()
    {
        return doorType.ToString();
    }
}
