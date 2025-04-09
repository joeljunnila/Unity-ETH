using System;

[Serializable]
public class EthereumConfig
{
    public string doorPrivateKey;
    public string tempPrivateKey;
    public string contractUserUser;
    public string contractUserDevice;
    public string contractDeviceDevice;
    public string rpcUrl;
}

[Serializable]
public class NetworkConfig
{
    public int chainId;
    public string networkName;
}

[Serializable]
public class ConfigKeys
{
    public EthereumConfig ethereum;
    public NetworkConfig network;
}