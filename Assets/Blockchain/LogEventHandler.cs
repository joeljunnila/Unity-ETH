using System;
using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using System.IO;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;

public class BlockchainEventLogger : MonoBehaviour
{

    [Header("Mouse Scroll Settings")]
    public Camera mainCamera;
    public LayerMask raycastLayers;
    public GameObject scrollTargetObject; 
    public float scrollSensitivity = 0.1f;

    private string rpcUrl;
    private string contractAddress;
    private string abi;

    [Header("UI Settings")]
    public TMP_Text logText;
    public ScrollRect scrollRect;

    private Web3 web3;
    private BigInteger lastCheckedBlock;

    void Start()
    {
        logText.text = ""; // tyhjennetään logi pelin alussa

        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
                Debug.LogWarning("No camera found in scene!");
        }

        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractUserDevice;

        GetABI();

        web3 = new Web3(rpcUrl);
        StartEventPolling();
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

    void Update()
    {
        
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            return;
        }

        if (mainCamera == null || scrollRect == null || scrollTargetObject == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (IsLookingAtScreen())
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                    scrollRect.verticalNormalizedPosition - scroll * scrollSensitivity
                );
            }
        }
    }

    string FormatRole(byte role)
    {
        switch (role)
        {
            case 3: return "<color=red>Admin 🛡</color>";
            case 2: return "<color=blue>Service 🛠</color>";
            case 1: return "<color=yellow>Default 👤</color>";
            default: return "<color=grey>None ❌</color>";
        }
    }


    string FormatDoor(BigInteger doorId)
    {
        int id = (int)(doorId % int.MaxValue); // Suojaus siltä varalta että arvo on liian suuri

        switch (id)
        {
            case 0:
                return "<color=#00FFFF>Main Door</color>"; // Cyan
            case 1:
                return "<color=#FF00FF>Server Room</color>"; // Magenta
            default:
                return $"<color=#FF0000>Door #{id}</color>";
        }
    }

    bool IsLookingAtScreen()
    {
        if (mainCamera == null || scrollTargetObject == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, raycastLayers))
        {
            return hit.collider != null && hit.collider.gameObject == scrollTargetObject;
        }
        return false;
    }


    private async void StartEventPolling()
    {
        var accessGrantedHandler = web3.Eth.GetEvent<AccessGrantedEventDTO>(contractAddress);
        var accessRevokedHandler = web3.Eth.GetEvent<AccessRevokedEventDTO>(contractAddress);
        var doorOpenedHandler = web3.Eth.GetEvent<DoorOpenedEventDTO>(contractAddress);

        var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        lastCheckedBlock = currentBlock.Value - 1;

        while (true)
        {
            try
            {
                currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                var fromBlock = new BlockParameter(new HexBigInteger(lastCheckedBlock + 1));
                var toBlock = new BlockParameter(new HexBigInteger(currentBlock.Value));

                // AccessGranted
                var grantedFilter = accessGrantedHandler.CreateFilterInput(fromBlock, toBlock);
                var grantedLogs = await accessGrantedHandler.GetAllChangesAsync(grantedFilter);
                foreach (var log in grantedLogs)
                {
                    AppendToLog($"<color=orange>[AccessGranted]</color> {log.Event.User} | Role: {FormatRole(log.Event.Role)} | Physical: {log.Event.Physical} | Digital: {log.Event.Digital}");
                }

                // AccessRevoked
                var revokedFilter = accessRevokedHandler.CreateFilterInput(fromBlock, toBlock);
                var revokedLogs = await accessRevokedHandler.GetAllChangesAsync(revokedFilter);
                foreach (var log in revokedLogs)
                {
                    AppendToLog($"<color=#AA0000>[AccessRevoked]</color> {log.Event.User}");
                }

                // DoorOpened
                var openedFilter = doorOpenedHandler.CreateFilterInput(fromBlock, toBlock);
                var openedLogs = await doorOpenedHandler.GetAllChangesAsync(openedFilter);
                foreach (var log in openedLogs)
                {
                    
                    AppendToLog($"<color=green>[DoorOpened]</color> {log.Event.User} | {FormatDoor(log.Event.DoorId)}");
                }

                // AccessChanged
                var accessChangedHandler = web3.Eth.GetEvent<AccessChangedEventDTO>(contractAddress);
                var changedFilter = accessChangedHandler.CreateFilterInput(fromBlock, toBlock);
                var changedLogs = await accessChangedHandler.GetAllChangesAsync(changedFilter);
                foreach (var log in changedLogs)
                {
                    string expirationStr = log.Event.Expiration == 0 ? "∞" : ((DateTimeOffset.FromUnixTimeSeconds((long)log.Event.Expiration)).ToString("yyyy-MM-dd HH:mm"));
                    AppendToLog($"<color=#00BFFF>[AccessChanged]</color> {log.Event.User} | Role: {FormatRole(log.Event.Role)} | Physical: {log.Event.Physical} | Digital: {log.Event.Digital} | AdminRoom: {log.Event.AdminRoom} | Expires: {expirationStr}");
                }

                lastCheckedBlock = currentBlock;

            }
            catch (Exception e)
            {
                Debug.LogError("Error while polling blockchain events: " + e.Message);
            }

            await Task.Delay(10000); // 10 sek
        }
    }

    void AppendToLog(string message)
    {
        if (logText != null)
        {
            logText.text += $"{DateTime.Now:HH:mm:ss} - {message}\n<alpha=#00>\n</alpha>";

            logText.ForceMeshUpdate();

            float height = logText.preferredHeight + 10f;
            logText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content); 

            ScrollToBottom();
        }
        else
        {
            Debug.Log(message);
        }
    }



    void ScrollToBottom()
    {
        if (scrollRect != null)
            StartCoroutine(EnsureScrollToBottom());
    }

    IEnumerator EnsureScrollToBottom()
    {
        yield return null; // Odota 1 frame
        yield return new WaitForEndOfFrame(); // Odota vielä seuraavan framen loppuun

        // Jos vielä ei näy, odota yksi ylimääräinen frame
        yield return null;

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = -10f;
    }



    // Event DTOs

    [Event("AccessGranted")]
    public class AccessGrantedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("uint8", "role", 2, false)]
        public byte Role { get; set; }

        [Parameter("bool", "physical", 3, false)]
        public bool Physical { get; set; }

        [Parameter("bool", "digital", 4, false)]
        public bool Digital { get; set; }
    }

    [Event("AccessRevoked")]
    public class AccessRevokedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }
    }

    [Event("AccessChanged")]
    public class AccessChangedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("uint8", "role", 2, false)]
        public byte Role { get; set; }

        [Parameter("bool", "physical", 3, false)]
        public bool Physical { get; set; }

        [Parameter("bool", "digital", 4, false)]
        public bool Digital { get; set; }

        [Parameter("bool", "adminRoom", 5, false)]
        public bool AdminRoom { get; set; }

        [Parameter("uint256", "expiration", 6, false)]
        public BigInteger Expiration { get; set; }
    }
 

    [Event("DoorOpened")]
    public class DoorOpenedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("uint256", "doorId", 2, false)]
        public BigInteger DoorId { get; set; }
    }

}
