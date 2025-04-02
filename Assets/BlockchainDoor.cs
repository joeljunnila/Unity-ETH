using UnityEngine;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using Unity.Netcode;

public class BlockchainDoor : NetworkBehaviour
{
    private string rpcUrl = "http://127.0.0.1:8545"; // GoQuorum RPC URL
    private string contractAddress = "0xE89e060B322483203e5b1f0Bf0dD3587f80583e8";

    public float openAngle = 90f;
    public float openSpeed = 2f;
    public float interactionRange = 2f; // Max distance to open the door

    public Coroutine doorAnimationCoroutine;

    public UnityEngine.Quaternion closedRotation;
    public UnityEngine.Quaternion openRotation;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    
    private Web3 web3;
    private string abi;

    void Start()
    {
        GetABI();
        web3 = new Web3(rpcUrl);

        closedRotation = transform.rotation;
        openRotation = UnityEngine.Quaternion.Euler(transform.eulerAngles + new UnityEngine.Vector3(0, openAngle, 0));

        // Sync door state when it changes
        isOpen.OnValueChanged += (oldValue, newValue) => StartDoorAnimation(newValue);
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
}