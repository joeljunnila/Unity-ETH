using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Threading.Tasks;

public class KeyInputHandler : NetworkBehaviour
{
    private NetworkVariable<NetString> publicKey = new NetworkVariable<NetString>(
        new NetString { Value = "<PublicKey>" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private TMP_Text UserName;
    [SerializeField] private TMP_Text WalletAddress;
    [SerializeField] private TMP_Text Balance;
    [SerializeField] private TMP_Text ReceivedMessage;
    [SerializeField] private TMP_InputField privateKeyInputField;
    [SerializeField] private TMP_InputField amountInputField;
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button signButton;

    private BlockchainDoor blockchainDoor;

    private GameObject overlappingPlayer;
    private UserUserContract contractScript;
    private Web3 web3;
    private float balanceCheckInterval = 5f;
    private float timeSinceLastCheck = 0f;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClick);
        sendButton.interactable = false;
        signButton.onClick.AddListener(OnSignButtonClick);

        if (IsClient)
        {
            UpdateUsernameDisplay(publicKey.Value.ToString());
            WalletAddress.text = "Address: ";
            Balance.text = "Balance: ";
            ReceivedMessage.text = "";
        }

        publicKey.OnValueChanged += OnPublicKeyChanged;
        blockchainDoor = FindObjectOfType<BlockchainDoor>();
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
    }

    private void HandleTransferSigned(string receiver, decimal amount, string message)
    {
        if (receiver.ToLower() == publicKey.Value.ToString().ToLower())
        {
            Debug.Log($"Contract signed by {receiver} for {amount} ETH with message: {message}");
            ReceivedMessage.text = $"Received: {amount}, Message: {message}";
        }
    }

    private void InitializeWeb3()
    {
        web3 = new Web3("http://localhost:8545");
        web3.TransactionManager.UseLegacyAsDefault = true;
    }

    private void RequestDoorAccess()
    {
        string playerAddress = publicKey.Value.Value; // Get dynamically stored public key
        if (!string.IsNullOrEmpty(playerAddress) && blockchainDoor != null)
        {
            blockchainDoor.RequestDoorOpen(playerAddress);
        }
        else
        {
            Debug.LogError("BlockchainDoor not found or player address is not set.");
        }
    }

    private async void OnSendButtonClick()
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

    private async void OnSignButtonClick()
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

    private void OnPrivateKeyEndEdit(string newPrivateKey)
    {
        if (!string.IsNullOrEmpty(newPrivateKey))
        {
            try
            {
                var chainId = 1337;
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

    private async void UpdateWalletAddressAndBalance()
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
            var chainId = 1337;
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
    private void SubmitPublicKeyServerRpc(string key)
    {
        publicKey.Value = new NetString { Value = key };
    }

    private void OnPublicKeyChanged(NetString previous, NetString current)
    {
        if (IsClient)
        {
            UpdateUsernameDisplay(current.ToString());
        }
    }

    private void UpdateUsernameDisplay(string key)
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            overlappingPlayer = other.gameObject;
            sendButton.interactable = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            overlappingPlayer = null;
            sendButton.interactable = false;
        }
    }

    public override void OnNetworkSpawn()
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

    public override void OnDestroy()
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

    void Update()
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

        
    }
}