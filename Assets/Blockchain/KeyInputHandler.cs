using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public class KeyInputHandler : NetworkBehaviour
{
    private NetworkVariable<NetString> publicKey = new NetworkVariable<NetString>(
        new NetString { Value = "<PublicKey>" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private TMP_Text UserName, WalletAddress, Balance, ReceivedMessage;
    [SerializeField] private TMP_InputField privateKeyInputField, amountInputField, messageInputField;
    [SerializeField] private Button sendButton, signButton;
    
    [Header("Admin Panel UI")]
    [SerializeField] private GameObject adminCanvas;
    [SerializeField] private TMP_Dropdown roleDropdown;
    [SerializeField] private Toggle physicalAccessToggle, digitalAccessToggle;
    [SerializeField] private Button grantAccess, revokeAccess, listAccess;
    [SerializeField] private TMP_InputField doorAccessInputField, expirationHoursField;

    private BlockchainDoor blockchainDoor;  
    private GameObject overlappingPlayer;
    private UserUserContract contractScript;
    private Web3 web3;
    private float balanceCheckInterval = 5f;
    private float timeSinceLastCheck = 0f;

    void Start()
    {
        // Set up UI button listeners and initialize dropdowns, toggles, etc.
        // Also handles contract event subscriptions and player door registration.

        sendButton.onClick.AddListener(OnSendButtonClick);
        sendButton.interactable = false;
        signButton.onClick.AddListener(OnSignButtonClick);
        
        adminCanvas.SetActive(!adminCanvas.activeSelf);
        
        if (roleDropdown != null)
        {
            roleDropdown.ClearOptions();
            roleDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Default", "Admin"
            });
        }
        if (physicalAccessToggle != null) physicalAccessToggle.isOn = true;
        if (digitalAccessToggle != null) digitalAccessToggle.isOn = true;

        grantAccess.onClick.AddListener(OnGrantAccessButtonClick);
        grantAccess.interactable = false;
        revokeAccess.onClick.AddListener(OnRevokeAccessButtonClick);
        revokeAccess.interactable = false;
        
        doorAccessInputField.onEndEdit.AddListener((string key) =>
        {
            if (key.Length == 42 && key.StartsWith("0x"))
            {
                grantAccess.interactable = true;
                revokeAccess.interactable = true;
            }
        });

        if (IsClient)
        {
            UpdateUsernameDisplay(publicKey.Value.ToString());
            WalletAddress.text = "Address: ";
            Balance.text = "Balance: ";
            ReceivedMessage.text = "";
        }

        publicKey.OnValueChanged += OnPublicKeyChanged;

        contractScript = GetComponent<UserUserContract>();
        if (contractScript == null)
        {
            Debug.LogError("Contract component not found!");
        }
        else
        {
            contractScript.OnTransferSigned += HandleTransferSigned;
        }

        privateKeyInputField.onEndEdit.AddListener(OnPrivateKeyEndEdit);
        InitializeWeb3();

        DoorEventListener doorMonitor = FindObjectOfType<DoorEventListener>();
        if (doorMonitor != null && !string.IsNullOrEmpty(publicKey.Value.Value))
        {
            doorMonitor.RegisterPlayerAddress(publicKey.Value.Value);
        }
    }

    private async void OnGrantAccessButtonClick() // Called when the "Grant Access" button is clicked; grants blockchain-based door access.
    {
        BlockchainDoor door = FindClosestDoor();
        if (door != null)
        {
            BlockchainDoor.AccessRole selectedRole = BlockchainDoor.AccessRole.Default;
            if (roleDropdown != null && roleDropdown.value == 1)
                selectedRole = BlockchainDoor.AccessRole.Admin;

            bool grantPhysical = physicalAccessToggle == null || physicalAccessToggle.isOn;
            bool grantDigital = digitalAccessToggle == null || digitalAccessToggle.isOn;

            uint expirationHours = 0;
            if (expirationHoursField != null && !string.IsNullOrEmpty(expirationHoursField.text))
            {
                if (uint.TryParse(expirationHoursField.text, out uint hours))
                    expirationHours = hours;
            }

            await door.GrantAccess(
                doorAccessInputField.text, 
                privateKeyInputField.text,
                selectedRole,
                grantPhysical,
                grantDigital,
                expirationHours
            );
        }
    }

    private async void OnRevokeAccessButtonClick() // Called when "Revoke Access" is clicked; revokes door access on the blockchain.
    {
        BlockchainDoor door = FindClosestDoor();
        if (door != null)
        {
            await door.RevokeAccess(doorAccessInputField.text, privateKeyInputField.text);
        }
    }

    private BlockchainDoor FindClosestDoor() // Returns the nearest door to the player (used for granting/revoking access).
    {
        BlockchainDoor[] allDoors = FindObjectsOfType<BlockchainDoor>();
        BlockchainDoor closestDoor = null;
        float closestDistance = float.MaxValue;

        foreach (BlockchainDoor door in allDoors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance < closestDistance)
            {
                closestDoor = door;
                closestDistance = distance;
            }
        }

        return closestDoor;
    }

    private void RequestDoorAccess() // Called when the player presses 'E'; sends door open request if nearby.
    {
        string playerAddress = publicKey.Value.Value;

        BlockchainDoor[] allDoors = FindObjectsOfType<BlockchainDoor>();
        BlockchainDoor closestDoor = null;
        float closestDistance = float.MaxValue;

        foreach (BlockchainDoor door in allDoors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance <= door.interactionRange && distance < closestDistance)
            {
                closestDoor = door;
                closestDistance = distance;
            }
        }

        if (closestDoor != null && !string.IsNullOrEmpty(playerAddress))
        {
            Debug.Log($"Attempting to access {(closestDoor.isPhysicalDoor ? "Physical" : "Digital")} {closestDoor.doorName}");
            closestDoor.RequestDoorOpenServerRpc(playerAddress);
        }
        else if (closestDoor == null)
        {
            Debug.Log("No door within interaction range");
        }
        else
        {
            Debug.LogError("Player address is not set.");
        }
    }


    private void HandleTransferSigned(string receiver, decimal amount, string message) // Handles event when a transfer is signed by a smart contract.
    {
        if (receiver.ToLower() == publicKey.Value.ToString().ToLower())
        {
            Debug.Log($"Contract signed by {receiver} for {amount} ETH with message: {message}");
            ReceivedMessage.text = $"Received: {amount}, Message: {message}";
        }
    }

    private void InitializeWeb3() // Initializes the Web3 object for Ethereum transactions.
    {
        web3 = new Web3("http://localhost:8545");
        web3.TransactionManager.UseLegacyAsDefault = true;
    }

    private async void OnSendButtonClick() // Transfers value and message to the overlapping player when "Send" is pressed using the smart contract.
    {
        if (overlappingPlayer != null && contractScript != null)
        {
            KeyInputHandler otherPlayerHandler = overlappingPlayer.GetComponent<KeyInputHandler>();
            if (otherPlayerHandler != null)
            {
                string senderPrivateKey = privateKeyInputField.text;
                string recipientPublicKey = otherPlayerHandler.publicKey.Value.ToString();
                string amountText = amountInputField.text;
                string message = messageInputField.text;

                if (!string.IsNullOrEmpty(senderPrivateKey) && 
                    !string.IsNullOrEmpty(recipientPublicKey) && 
                    !string.IsNullOrEmpty(amountText))
                {
                    if (decimal.TryParse(amountText, out decimal amount) && amount > 0)
                    {
                        string txHash = await contractScript.InitiateTransfer(senderPrivateKey, recipientPublicKey, amount, message);
                        if (txHash != null)
                        {
                            UpdateWalletAddressAndBalance();
                        }
                    }
                    else
                    {
                        Debug.LogError("Invalid amount entered!");
                    }
                }
            }
        }
    }

    private async void OnSignButtonClick() // Signs an initiated transfer from the contract using the private key when "Sign" is pressed.
    {
        string recipientPrivateKey = privateKeyInputField.text;
        if (!string.IsNullOrEmpty(recipientPrivateKey))
        {
            string txHash = await contractScript.SignTransfer(recipientPrivateKey);
            if (txHash != null)
            {
                UpdateWalletAddressAndBalance();
            }
        }
    }

    private void OnPrivateKeyEndEdit(string newPrivateKey) // Called when the user finishes editing the private key input field.
    {
        if (!string.IsNullOrEmpty(newPrivateKey))
        {
            try
            {
                // Extracts public key from private key and sends it to server for syncing.

                var chainId = ConfigLoader.config.network.chainId;
                var account = new Account(newPrivateKey, chainId);
                if (IsClient && !IsServer)
                {
                    if (publicKey.Value.Value != account.Address)
                    {
                        SubmitPublicKeyServerRpc(account.Address);
                    }
                }
                else if (IsServer)
                {
                    if (publicKey.Value.Value != account.Address)
                    {
                        publicKey.Value = new NetString { Value = account.Address };
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to process private key: {ex.Message}");
            }
        }
    }
    
    public string GetPublicAddress() // Returns the current public address for this player.
    {
        return publicKey.Value.ToString();
    }
    
    private async void UpdateWalletAddressAndBalance() // Updates the wallet address and balance UI from blockchain data.
    {
        string privateKey = privateKeyInputField.text;
        if (string.IsNullOrEmpty(privateKey) || privateKey.Length != 66 || !privateKey.StartsWith("0x"))
        {
            WalletAddress.text = "Address: ";
            Balance.text = "Balance: ";
            return;
        }

        try
        {
            var chainId = ConfigLoader.config.network.chainId;
            var account = new Account(privateKey, chainId);
            web3 = new Web3(account, "http://localhost:8545");
            web3.TransactionManager.UseLegacyAsDefault = true;

            WalletAddress.text = $"Address: {account.Address}";
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            decimal balanceEth = Web3.Convert.FromWei(balanceWei.Value);
            Balance.text = $"Balance: {balanceEth.ToString("F4")}";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update wallet address or balance: {ex.Message}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPublicKeyServerRpc(string key) // Called by client to submit public key to server.
    {
        publicKey.Value = new NetString { Value = key };
    }

    private void OnPublicKeyChanged(NetString previous, NetString current) // Triggered when publicKey NetworkVariable changes; updates client display.
    {
        if (IsClient)
        {
            UpdateUsernameDisplay(current.ToString());
        }
    }

    private void UpdateUsernameDisplay(string key) // Updates the displayed player name using a short version of the address.
    {
        if (key.Length <= 12)
        {
            UserName.text = key;
        }
        else
        {
            UserName.text = key.Substring(0, 5) + "...";
        }
    }

    private void OnTriggerEnter(Collider other)   // Trigger event when another player enters this player’s collider.
    {
        if (other.CompareTag("Player"))
        {
            overlappingPlayer = other.gameObject;
            sendButton.interactable = true;
        }
    }

    private void OnTriggerExit(Collider other) // Trigger event when another player exits this player’s collider.
    {
        if (other.CompareTag("Player"))
        {
            overlappingPlayer = null;
            sendButton.interactable = false;
        }
    }

    public override void OnNetworkSpawn() // Called when the object spawns in the network.
    {
        if (IsClient)
        {
            UpdateUsernameDisplay(publicKey.Value.ToString());
            if (!string.IsNullOrEmpty(privateKeyInputField.text))
            {
                UpdateWalletAddressAndBalance();
            }
        }
        base.OnNetworkSpawn();
    }

    public override void OnDestroy() // Cleans up listeners and events when the object is destroyed.
    {
        publicKey.OnValueChanged -= OnPublicKeyChanged;
        if (privateKeyInputField != null)
        {
            privateKeyInputField.onEndEdit.RemoveListener(OnPrivateKeyEndEdit);
        }
        if (contractScript != null)
        {
            contractScript.OnTransferSigned -= HandleTransferSigned;
        }
        base.OnDestroy();
    }

    void Update() // Handles automatic wallet updates and input-driven actions like door access or toggling admin UI in realtime.
    {
        if (IsClient && !string.IsNullOrEmpty(privateKeyInputField.text))
        {
            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck >= balanceCheckInterval)
            {
                UpdateWalletAddressAndBalance();
                timeSinceLastCheck = 0f;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                RequestDoorAccess();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            adminCanvas.SetActive(!adminCanvas.activeSelf);                       
        }     
    }
}