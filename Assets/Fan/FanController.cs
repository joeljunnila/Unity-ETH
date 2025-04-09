using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.IO;

public class FanController : NetworkBehaviour
{
    public Transform fanBlade;
    private string rpcUrl;
    private string contractAddress;
    private string abi;
    private Web3 web3;
    private Contract contract;
    private float currentRotationSpeed = 100f;

    public NetworkVariable<float> fanSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Start()
    {
        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractDeviceDevice;

        GetABI();

        if (fanBlade == null)
        {
            Debug.LogError("Fan Blade is not assigned!");
            return;
        }

        web3 = new Web3(rpcUrl);

        if (!string.IsNullOrEmpty(abi))
        {
            contract = web3.Eth.GetContract(abi, contractAddress);
        }
        // Repeat request of temperature every 10 seconds
        InvokeRepeating(nameof(RequestTemperatureUpdate), 7f, 10f);

        fanSpeed.OnValueChanged += (oldSpeed, newSpeed) => UpdateFanRotation(newSpeed);
    }

    void GetABI()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/DeviceDevice.json");

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
    if (fanBlade != null)
    {
        fanBlade.Rotate(Vector3.left * currentRotationSpeed * Time.deltaTime);
    }
}

    private void UpdateFanRotation(float speed) // Updating fan rotationSpeed
    {
        currentRotationSpeed = speed;
    }

    private async void RequestTemperatureUpdate() // Requesting temperature update
    {
        int temp = await FetchTemperature();
        UpdateFanSpeedServerRpc(temp);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateFanSpeedServerRpc(int temp) // Maths for the fanSpeed and updating the value on server
    {
        float newSpeed = Mathf.Lerp(100f, 1000f, Mathf.Clamp01((temp - 20f) / 10f));
        fanSpeed.Value = newSpeed;
    }

    private async Task<int> FetchTemperature() // Function to fetch temp from the smart contract
    {
        if (contract == null)
        {
            Debug.LogError("Contract not initialized.");
            return 0;
        }

        var getTempFunction = contract.GetFunction("getTemperature");

        try
        {
            // Contract returns int256, we fetch as BigInteger and cast to int
            var tempBigInt = await getTempFunction.CallAsync<System.Numerics.BigInteger>();
            int temp = (int)tempBigInt;
            return temp;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error fetching temperature: " + ex.Message);
            return 0;
        }
    }
}