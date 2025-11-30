// Converters/IPAddressConverter.cs
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnsChef.Converters
{
    public class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var ipString = reader.GetString();
            return IPAddress.TryParse(ipString, out var ip) ? ip : IPAddress.Any;
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class IPEndPointConverter : JsonConverter<IPEndPoint>
    {
        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var endPointString = reader.GetString();
            if (string.IsNullOrEmpty(endPointString))
                return new IPEndPoint(IPAddress.Any, 0);

            var parts = endPointString.Split(':');
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip) && int.TryParse(parts[1], out var port))
            {
                return new IPEndPoint(ip, port);
            }

            return new IPEndPoint(IPAddress.Any, 0);
        }

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Address}:{value.Port}");
        }
    }
}