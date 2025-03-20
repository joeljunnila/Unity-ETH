using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Nethereum.Web3;
using System.Threading.Tasks;
using Nethereum.Unity.Rpc;

public class GetLatestBlockVanillaNethereum : MonoBehaviour
{
    private static bool TrustCertificate(object sender, X509Certificate x509Certificate, X509Chain x509Chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    private string Url = "http://localhost:8545/";
    private bool isRunning = true;

    async void Start()
    {
        isRunning = true;
        await CheckBlockNumberPeriodically();
    }

    async Task CheckBlockNumberPeriodically()
    {
        var wait = 1000;
        while (isRunning)
        {
            await Task.Delay(wait);
            if (!isRunning) break;

            var web3 = new Web3(new UnityWebRequestRpcTaskClient(new Uri(Url)));
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            Debug.Log($"Latest Block Number: {blockNumber.Value}");
        }
    }

    void OnDestroy()
    {
        isRunning = false;
    }
}
