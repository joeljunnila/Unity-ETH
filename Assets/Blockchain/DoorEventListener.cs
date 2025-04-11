using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Numerics;
using Nethereum.RPC.Eth.DTOs;
using TMPro;

public class DoorEventListener : NetworkBehaviour
{
    private string rpcUrl;
    private string contractAddress;
    private Web3 web3;
    private string abi;
    
    // Event handler for door openings only
    private Event<DoorOpenedEventDTO> doorOpenedEvent;
    private TMP_Text npcWalletDisplay;
    
    [Header("NPC Spawning")]
    [SerializeField] 
    [Tooltip("The NPC prefab to spawn when an external wallet is detected")]
    private GameObject npcPrefab;
    
    [SerializeField]
    [Tooltip("The transform position where NPCs will spawn")]
    private Transform spawnPoint;
    
    [SerializeField]
    [Tooltip("Random offset applied to the spawn position")]
    [Range(0.5f, 5f)]
    private float spawnOffset = 1.5f;

    [SerializeField]
    [Tooltip("Only spawn NPCs for physical door access")]
    private bool onlySpawnForPhysicalDoors = true;
    
    // Dictionary to track known wallet addresses in the scene
    private Dictionary<string, GameObject> addressToNPC = new Dictionary<string, GameObject>();
    
    // List of player wallet addresses currently in the game
    private List<string> knownPlayerAddresses = new List<string>();

    private const string LastBlockKey = "LastProcessedBlock";
    private BlockParameter lastBlock = null;

    
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
        
        // Start listening to door events
        StartCoroutine(SetupDoorEventListener());
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

    private void LoadLastBlock()
    {
        if (PlayerPrefs.HasKey(LastBlockKey))
        {
            BigInteger blockNum = BigInteger.Parse(PlayerPrefs.GetString(LastBlockKey));
            lastBlock = new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(blockNum));
        }
    }

    private void SaveLastBlock(BigInteger blockNum)
    {
        PlayerPrefs.SetString(LastBlockKey, blockNum.ToString());
        PlayerPrefs.Save();
    }
    
    IEnumerator SetupDoorEventListener()
    {
        yield return new WaitForSeconds(1f); // Small delay to ensure connection is ready
        
        var blockNumberTask = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        yield return new WaitUntil(() => blockNumberTask.IsCompleted);

        try
        {
            var contract = web3.Eth.GetContract(abi, contractAddress);
            
            // Set up door opened event handler correctly
            doorOpenedEvent = contract.GetEvent<DoorOpenedEventDTO>();

            var currentBlockNumber = blockNumberTask.Result.Value;
            lastBlock = new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(currentBlockNumber + 1));
            
            // Start monitoring door events using polling
            StartCoroutine(MonitorDoorEvents());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error setting up event listener: {ex.Message}");
        }
    }
    
    private IEnumerator MonitorDoorEvents()
    {
        // Create filter to get all door opened events
        var filterAll = doorOpenedEvent.CreateFilterInput();
        while (true)
        {
            Task<List<EventLog<DoorOpenedEventDTO>>> getAllEventsTask = null;
            bool hasError = false;
            
            // Step 1: Set up the task outside the try block
            try
            {
                // If we have a last known block, only get events from that block onward
                if (lastBlock != null)
                {
                    filterAll.FromBlock = lastBlock;
                }
                
                // Query for new events
                getAllEventsTask = doorOpenedEvent.GetAllChangesAsync(filterAll);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting up event query: {ex.Message}");
                hasError = true;
            }
            
            // Handle error case outside of the catch block
            if (hasError)
            {
                yield return new WaitForSeconds(3);
                continue;
            }
            
            // Step 2: Wait for task completion outside the try block
            yield return new WaitUntil(() => getAllEventsTask.IsCompleted);
            
            // Step 3: Process results in a separate try block
            try
            {
                // Process any events found
                var eventLogs = getAllEventsTask.Result;
                if (eventLogs.Count > 0)
                {
                    foreach (var log in eventLogs)
                    {
                        string walletAddress = log.Event.User;
                        bool isPhysical = log.Event.IsPhysical;
                                     
                        // Check if this address is unknown and spawn an NPC if needed
                        CheckAndSpawnNPC(walletAddress, isPhysical);
                    }
                    
                    // Update the last known block to avoid duplicates
                    var lastBlockNumber = eventLogs[eventLogs.Count - 1].Log.BlockNumber;
                    lastBlock = new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(lastBlockNumber.Value + 1));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing door events: {ex.Message}");
            }
            
            // Wait before the next check
            yield return new WaitForSeconds(3);
        }
    }
    
    void OnDestroy()
    {
        // Clean up coroutines when destroyed
        StopAllCoroutines();
    }
    
    // Register a player address so we don't spawn NPCs for actual players
    public void RegisterPlayerAddress(string playerAddress)
    {
        if (string.IsNullOrEmpty(playerAddress) || playerAddress.Length != 42 || !playerAddress.StartsWith("0x"))
        {
            return;
        }
        else
        {
            knownPlayerAddresses.Add(playerAddress);
        }
    }
    
    private void CheckAndSpawnNPC(string walletAddress, bool isPhysical)
    {
        // Skip if address is empty/invalid
        if (string.IsNullOrEmpty(walletAddress))
            return;
            
        // Only spawn NPCs for physical door access if configured that way
        if (onlySpawnForPhysicalDoors && !isPhysical)
        {
            return;
        }
        
        // First check if this address belongs to any player in the scene
        if (IsWalletAddressInScene(walletAddress))
        {
            return;
        }
        
        // Don't spawn if the address already has an NPC
        if (addressToNPC.ContainsKey(walletAddress))
        {
            return;
        }
        
        // This is a new external wallet accessing a physical door - spawn an NPC
        SpawnNPCForAddress(walletAddress);
    }
    
    private bool IsWalletAddressInScene(string walletAddress)
    {
        // Find all KeyInputHandlers in the scene and check their public keys
        KeyInputHandler[] allKeyHandlers = FindObjectsOfType<KeyInputHandler>();
        
        foreach (var handler in allKeyHandlers)
        {
            string playerAddress = handler.GetPublicAddress();
            
            // Normalize addresses for comparison (convert to lowercase)
            if (!string.IsNullOrEmpty(playerAddress) && 
                walletAddress.ToLowerInvariant() == playerAddress.ToLowerInvariant())
            {
                return true; // Found a match - this wallet belongs to a player in the scene
            }
        }
        
        // Also check our known player addresses list
        foreach (string address in knownPlayerAddresses)
        {
            if (walletAddress.ToLowerInvariant() == address.ToLowerInvariant())
            {
                return true;
            }
        }
        
        return false; // No matching player found
    }
    
    private void SpawnNPCForAddress(string walletAddress)
    {
        if (npcPrefab == null)
        {
            Debug.LogError("NPC prefab not assigned in the Inspector!");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point not set! Using this object's position instead.");
            spawnPoint = this.transform;
        }
        
        // Only the server should spawn
        if (!IsServer) return;
        
        // Calculate position with some randomization around spawn point
        UnityEngine.Vector3 spawnPosition = spawnPoint.position + new UnityEngine.Vector3(
            UnityEngine.Random.Range(-spawnOffset, spawnOffset), 
            0, 
            UnityEngine.Random.Range(-spawnOffset, spawnOffset)
        );
        
        // Spawn the NPC
        GameObject npc = Instantiate(npcPrefab, spawnPosition, spawnPoint.rotation);
        
        // Configure the NPC - set a wallet text display if available
        npcWalletDisplay = npc.GetComponentInChildren<TMP_Text>();

        if (npcWalletDisplay != null)
        {
            string shortAddress = walletAddress.Substring(0, 4) + "...";
            npcWalletDisplay.text = shortAddress;
        }
        
        // Make the NPC network-aware by spawning it through NetworkManager
        if (npc.GetComponent<NetworkObject>() != null)
        {
            npc.GetComponent<NetworkObject>().Spawn();
        }
        
        // Store the NPC in our dictionary
        addressToNPC[walletAddress] = npc;
        
        Debug.Log($"Spawned NPC for external wallet: {walletAddress}");
    }
    
    // Updated Event DTO for door opened events with isPhysical parameter
    [Event("DoorOpened")]
    public class DoorOpenedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }
        
        [Parameter("bool", "isPhysical", 2, false)]
        public bool IsPhysical { get; set; }
    }
}