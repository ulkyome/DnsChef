using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

public class DnsPacket
{
    public ushort TransactionId { get; set; }
    public ushort Flags { get; set; }
    public ushort Questions { get; set; }
    public ushort AnswerRRs { get; set; }
    public ushort AuthorityRRs { get; set; }
    public ushort AdditionalRRs { get; set; }
    public List<DnsQuestion> QuestionSection { get; set; }
    public List<DnsResourceRecord> AnswerSection { get; set; }

    public DnsPacket()
    {
        QuestionSection = new List<DnsQuestion>();
        AnswerSection = new List<DnsResourceRecord>();
    }

    public byte[] ToBytes()
    {
        var data = new List<byte>();

        // Header
        data.AddRange(GetNetworkBytes(TransactionId));
        data.AddRange(GetNetworkBytes(Flags));
        data.AddRange(GetNetworkBytes(Questions));
        data.AddRange(GetNetworkBytes(AnswerRRs));
        data.AddRange(GetNetworkBytes(AuthorityRRs));
        data.AddRange(GetNetworkBytes(AdditionalRRs));

        // Question section
        foreach (var question in QuestionSection)
        {
            data.AddRange(question.ToBytes());
        }

        // Answer section
        foreach (var answer in AnswerSection)
        {
            data.AddRange(answer.ToBytes());
        }

        return data.ToArray();
    }

    public static DnsPacket FromBytes(byte[] data)
    {
        if (data == null || data.Length < 12) // Minimum DNS header size
        {
            throw new ArgumentException("Invalid DNS packet data");
        }

        var packet = new DnsPacket();
        var offset = 0;

        try
        {
            // Header
            packet.TransactionId = ReadUInt16Network(data, ref offset);
            packet.Flags = ReadUInt16Network(data, ref offset);
            packet.Questions = ReadUInt16Network(data, ref offset);
            packet.AnswerRRs = ReadUInt16Network(data, ref offset);
            packet.AuthorityRRs = ReadUInt16Network(data, ref offset);
            packet.AdditionalRRs = ReadUInt16Network(data, ref offset);

            // Question section
            for (int i = 0; i < packet.Questions; i++)
            {
                if (offset >= data.Length) break;

                var question = DnsQuestion.FromBytes(data, ref offset);
                packet.QuestionSection.Add(question);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse DNS packet", ex);
        }

        return packet;
    }

    private static byte[] GetNetworkBytes(ushort value)
    {
        return BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)value));
    }

    private static byte[] GetNetworkBytes(uint value)
    {
        return BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)value));
    }

    private static ushort ReadUInt16Network(byte[] data, ref int offset)
    {
        var value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
        offset += 2;
        return value;
    }

    private static uint ReadUInt32Network(byte[] data, ref int offset)
    {
        var value = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
        offset += 4;
        return value;
    }
}