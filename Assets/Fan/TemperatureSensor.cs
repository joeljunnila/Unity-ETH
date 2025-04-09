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
    private string rpcUrl;
    private string contractAddress;
    private string abi;
    private string privateKey;
    private Web3 web3;
    private Contract contract;
    private int currentTemperature = 25;

    void Start()
    {
        if (ConfigLoader.config == null || ConfigLoader.config.ethereum == null)
        {
            Debug.LogError("Ethereum config is not loaded. Check if ConfigLoader has run.");
            return;
        }

        rpcUrl = ConfigLoader.config.ethereum.rpcUrl;
        contractAddress = ConfigLoader.config.ethereum.contractDeviceDevice;
        privateKey = ConfigLoader.config.ethereum.tempPrivateKey;

        GetABI();

        var account = new Account(privateKey);
        web3 = new Web3(account, rpcUrl);

        contract = web3.Eth.GetContract(abi, contractAddress);

        if (temperatureText == null)
        {
            temperatureText = GetComponentInChildren<TextMeshPro>();
        }
        // Repeat simulating and uploading the temperature every 10 seconds
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

    void SimulateAndUploadTemp() // Simulating temperature as random and uploading it to display and blockchain
    {
        currentTemperature = Random.Range(20, 30);
        UpdateTemperatureDisplay(currentTemperature);
        UploadTemperatureToBlockchain(currentTemperature);
    }

    async void UploadTemperatureToBlockchain(int temp) // Uploading the temperature to blockchain via transaction to smart contract
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
        var gasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(0); 

        var transactionHash = await updateTempFunction.SendTransactionAsync(
            from: web3.TransactionManager.Account.Address,
            gas: gas,
            gasPrice: gasPrice,
            value: null,
            functionInput: temp
        );
    }
    catch (System.Exception e)
    {
        Debug.LogError("Error uploading temperature: " + e.Message);
    }
}

    void UpdateTemperatureDisplay(int temperature) // Uploading temperature as a text into the display
    {
        if (temperatureText != null)
        {
            temperatureText.text = "Temp: " + temperature + "°C";
        }
    }
}