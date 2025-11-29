using System.Net;
using System.Text;

public class DnsQuestion
{
    public string Name { get; set; } = string.Empty;
    public ushort Type { get; set; }
    public ushort Class { get; set; }

    public byte[] ToBytes()
    {
        var data = new List<byte>();

        // Domain name
        if (!string.IsNullOrEmpty(Name))
        {
            var labels = Name.Split('.');
            foreach (var label in labels)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    data.Add((byte)label.Length);
                    data.AddRange(Encoding.ASCII.GetBytes(label));
                }
            }
        }
        data.Add(0); // End of domain name

        // Type and Class
        data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)Type)));
        data.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)Class)));

        return data.ToArray();
    }

    public static DnsQuestion FromBytes(byte[] data, ref int offset)
    {
        var question = new DnsQuestion();

        // Read domain name
        question.Name = ReadDomainName(data, ref offset);

        // Read type and class
        question.Type = ReadUInt16Network(data, ref offset);
        question.Class = ReadUInt16Network(data, ref offset);

        return question;
    }

    private static string ReadDomainName(byte[] data, ref int offset)
    {
        var parts = new List<string>();
        int length;
        int originalOffset = offset;

        try
        {
            while ((length = data[offset]) != 0)
            {
                offset++;

                // Check for compression pointer
                if ((length & 0xC0) == 0xC0)
                {
                    int pointer = ((length & 0x3F) << 8) | data[offset];
                    offset++;
                    var savedOffset = offset;
                    offset = pointer;
                    var compressedName = ReadDomainName(data, ref offset);
                    offset = savedOffset;
                    return compressedName;
                }

                if (offset + length <= data.Length)
                {
                    var label = Encoding.ASCII.GetString(data, offset, length);
                    parts.Add(label);
                    offset += length;
                }
                else
                {
                    break;
                }
            }

            offset++; // Skip the final zero
        }
        catch (IndexOutOfRangeException)
        {
            // Return what we have so far
            offset = originalOffset;
            return "invalid.domain";
        }

        return string.Join(".", parts);
    }

    private static ushort ReadUInt16Network(byte[] data, ref int offset)
    {
        if (offset + 2 > data.Length)
            return 0;

        var value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
        offset += 2;
        return value;
    }
}