using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.IO;
using Nethereum.Web3.Accounts;

public class TemperatureSensor : MonoBehaviour
{
    public TextMeshPro temperatureText;
    private string rpcUrl = "http://127.0.0.1:8545";
    private string contractAddress = "0x1E3A517Cabb3a96fA35a4Dc1eB77D220A6117Ad5";
    private string abi;
    private string privateKey = "e417a0645214677eb9820ab5775eab4596538a06ff8c31a8c655c08a44139a37";
    private Web3 web3;
    private Contract contract;
    private int currentTemperature = 25;

    void Start()
    {
        GetABI();

        var account = new Account(privateKey);
        web3 = new Web3(account, rpcUrl);

        contract = web3.Eth.GetContract(abi, contractAddress);

        if (temperatureText == null)
        {
            temperatureText = GetComponentInChildren<TextMeshPro>();
        }

        InvokeRepeating(nameof(SimulateAndUploadTemp), 2f, 10f);
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

    void SimulateAndUploadTemp()
    {
        currentTemperature = Random.Range(20, 30);
        UpdateTemperatureDisplay(currentTemperature);
        UploadTemperatureToBlockchain(currentTemperature);
    }

    async void UploadTemperatureToBlockchain(int temp)
{
    try
    {
        if (contract == null)
        {
            Debug.LogError("Smart contract is not initialized.");
            return;
        }

        var updateTempFunction = contract.GetFunction("updateTemperature");

        if (updateTempFunction == null)
        {
            Debug.LogError("updateTemperature function not found in ABI!");
            return;
        }

        var gas = new Nethereum.Hex.HexTypes.HexBigInteger(300000);
        var gasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(0); // 0 for GoQuorum

        var transactionHash = await updateTempFunction.SendTransactionAsync(
            from: web3.TransactionManager.Account.Address,
            gas: gas,
            gasPrice: gasPrice,
            value: null,
            functionInput: temp
        );

        Debug.Log($"Temperature {temp}°C updated. TxHash: {transactionHash}");
    }
    catch (System.Exception e)
    {
        Debug.LogError("Error uploading temperature: " + e.Message);
    }
}

    void UpdateTemperatureDisplay(int temperature)
    {
        if (temperatureText != null)
        {
            temperatureText.text = "Temp: " + temperature + "°C";
        }
    }
}