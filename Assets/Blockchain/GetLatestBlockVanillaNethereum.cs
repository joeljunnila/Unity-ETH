using System;
using UnityEngine;
using Nethereum.Web3;
using System.Threading.Tasks;
using Nethereum.Unity.Rpc;

public class GetLatestBlockOnKeyPress : MonoBehaviour
{
    private string Url = "http://localhost:8545";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            FetchLatestBlockNumber();
        }
    }

    async void FetchLatestBlockNumber()
    {
        var web3 = new Web3(new UnityWebRequestRpcTaskClient(new Uri(Url)));
        var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        Debug.Log($"Latest Block Number: {blockNumber.Value}");
    }
}
