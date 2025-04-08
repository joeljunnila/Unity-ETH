using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.IO;

public class FanController : NetworkBehaviour
{
    public Transform fanBlade;
    private string rpcUrl = "http://127.0.0.1:8545";
    private string contractAddress = "0x1E3A517Cabb3a96fA35a4Dc1eB77D220A6117Ad5";
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

        InvokeRepeating(nameof(RequestTemperatureUpdate), 7f, 10f);

        fanSpeed.OnValueChanged += (oldSpeed, newSpeed) => UpdateFanRotation(newSpeed);
    }

    void GetABI()
    {
        string path = Path.Combine(Application.dataPath, "Blockchain/ABI/DeviceDevice.json");

        if (File.Exists(path))
        {
            abi = File.ReadAllText(path);
            Debug.Log("Loaded Fan ABI successfully.");
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

    private void UpdateFanRotation(float speed)
    {
        currentRotationSpeed = speed;
        Debug.Log("Fan Speed Updated: " + speed);
    }

    private async void RequestTemperatureUpdate()
    {
        int temp = await FetchTemperature();
        Debug.Log($"Fetched Temp from chain: {temp}");
        UpdateFanSpeedServerRpc(temp);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateFanSpeedServerRpc(int temp)
    {
        float newSpeed = Mathf.Lerp(100f, 1000f, Mathf.Clamp01((temp - 20f) / 10f));
        Debug.Log($"Calculated Fan Speed (from temp {temp}): {newSpeed}");
        fanSpeed.Value = newSpeed;
    }

    private async Task<int> FetchTemperature()
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