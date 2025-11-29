using System.Net;

public class DnsResourceRecord
{
    public string Name { get; set; } = string.Empty;
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    public uint TTL { get; set; }
    public ushort DataLength { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public IPAddress IPAddress { get; set; } = IPAddress.Any;

    public byte[] ToBytes()
    {
        var data = new List<byte>();

        // Name (with compression pointer - point to the name in question section)
        data.Add(0xC0);
        data.Add(0x0C); // Pointer to offset 12 (start of question section)

        // Type, Class, TTL
        data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)Type)));
        data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)Class)));
        data.AddRange(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)TTL)));

        // Data
        if (Type == 1 && Class == 1 && IPAddress != IPAddress.Any) // A record
        {
            var ipBytes = IPAddress.GetAddressBytes();
            data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)ipBytes.Length)));
            data.AddRange(ipBytes);
        }
        else if (Data.Length > 0)
        {
            data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)DataLength)));
            data.AddRange(Data);
        }
        else
        {
            data.AddRange(new byte[] { 0, 0 }); // Zero length
        }

        return data.ToArray();
    }
}