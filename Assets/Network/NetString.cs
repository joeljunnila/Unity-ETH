using Unity.Netcode;

public struct NetString : INetworkSerializable
{
    public string Value;

    public NetString(string value)
    {
        Value = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
