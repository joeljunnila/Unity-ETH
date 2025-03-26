using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Threading.Tasks;

public class KeyInputHandler : NetworkBehaviour
{
    // NetworkVariable to sync the public key for this specific instance
    private NetworkVariable<NetString> publicKey = new NetworkVariable<NetString>(
        new NetString { Value = "<PublicKey>" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private TMP_Text UserName;
    [SerializeField] private TMP_Text WalletAddress;
    [SerializeField] private TMP_Text Balance;
    [SerializeField] private TMP_InputField publicKeyInputField;
    [SerializeField] private TMP_InputField privateKeyInputField;
    [SerializeField] private TMP_InputField amountInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button updateButton;

    private GameObject overlappingPlayer;
    private Transaction transactionScript;
    private Web3 web3;
    private float balanceCheckInterval = 5f; // Balance check every 5 seconds
    private float timeSinceLastCheck = 0f;

    void Start()
    {
        updateButton.onClick.AddListener(OnUpdateButtonClick);
        sendButton.onClick.AddListener(OnSendButtonClick);
        sendButton.interactable = false;

        // Ensure initial UI state
        if (IsClient)
        {
            UpdateUsernameDisplay(publicKey.Value.ToString());
            WalletAddress.text = "Address: ";
            Balance.text = "Balance: ";
        }

        // Subscribe to changes in the NetworkVariable
        publicKey.OnValueChanged += OnPublicKeyChanged;

        // Get the Transaction component
        transactionScript = GetComponent<Transaction>();
        if (transactionScript == null)
        {
            Debug.LogError("Transaction component not found on this GameObject!");
        }

        // Listen for private key changes
        privateKeyInputField.onValueChanged.AddListener(OnPrivateKeyChanged);

        // Initialize Web3
        InitializeWeb3();
    }

    void Update()
    {
        // Periodically check balance if private key is set
        if (IsClient && !string.IsNullOrEmpty(privateKeyInputField.text))
        {
            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck >= balanceCheckInterval)
            {
                UpdateWalletAddressAndBalance();
                timeSinceLastCheck = 0f;
            }
        }
    }

    private void InitializeWeb3()
    {
        web3 = new Web3("http://localhost:8545");
        web3.TransactionManager.UseLegacyAsDefault = true;
    }

    private void OnUpdateButtonClick()
    {
        string newKey = publicKeyInputField.text;
        if (string.IsNullOrEmpty(newKey)) return;

        if (IsClient && !IsServer)
        {
            SubmitPublicKeyServerRpc(newKey);
        }
        else if (IsServer)
        {
            publicKey.Value = new NetString { Value = newKey };
        }

        // Update wallet address on update button click (balance updates via OnPrivateKeyChanged or polling)
        if (!string.IsNullOrEmpty(privateKeyInputField.text))
        {
            UpdateWalletAddressAndBalance();
        }
    }

    private void OnSendButtonClick()
    {
        if (overlappingPlayer != null && transactionScript != null)
        {
            KeyInputHandler otherPlayerHandler = overlappingPlayer.GetComponent<KeyInputHandler>();
            if (otherPlayerHandler != null)
            {
                string senderPrivateKey = privateKeyInputField.text;
                string recipientPublicKey = otherPlayerHandler.publicKey.Value.ToString();
                string amountText = amountInputField.text;

                if (!string.IsNullOrEmpty(senderPrivateKey) && !string.IsNullOrEmpty(recipientPublicKey) && !string.IsNullOrEmpty(amountText))
                {
                    if (decimal.TryParse(amountText, out decimal amount) && amount > 0)
                    {
                        transactionScript.ExecuteTransaction(senderPrivateKey, recipientPublicKey, amount);
                        UpdateWalletAddressAndBalance();
                    }
                    else
                    {
                        Debug.LogError("Invalid amount entered!");
                    }
                }
            }
        }
    }

    private void OnPrivateKeyChanged(string newPrivateKey)
    {
        if (!string.IsNullOrEmpty(newPrivateKey))
        {
            UpdateWalletAddressAndBalance(); // Update address and balance when private key changes
        }
        else
        {
            WalletAddress.text = "Address: ";
            Balance.text = "Balance: ";
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
            // Derive account from private key
            var chainId = 1337;
            var account = new Account(privateKey, chainId);
            web3 = new Web3(account, "http://localhost:8545");
            web3.TransactionManager.UseLegacyAsDefault = true;

            // Update wallet address
            WalletAddress.text = $"Address: {account.Address}";

            // Get balance
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            decimal balanceEth = Web3.Convert.FromWei(balanceWei.Value);

            // Update balance UI
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
            privateKeyInputField.onValueChanged.RemoveListener(OnPrivateKeyChanged);
        }
        base.OnDestroy();
    }
}