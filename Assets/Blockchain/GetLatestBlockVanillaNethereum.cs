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
        // Accept all certificates
        return true;
    }

    private string Url = "http://localhost:8545";

    // Use this for initialization
    async void Start()
    {
        await CheckBlockNumberPeriodically();
    }

    public async Task CheckBlockNumberPeriodically()
    {
        var wait = 1000;
        while (true)
        {
            await Task.Delay(wait);
            wait = 1000;
            var web3 = new Web3(new UnityWebRequestRpcTaskClient(new Uri(Url)));
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            Debug.Log($"Latest Block Number: {blockNumber.Value}");
        }
    }
}
