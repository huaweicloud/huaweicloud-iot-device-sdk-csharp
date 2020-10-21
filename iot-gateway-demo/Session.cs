using System.Text;
using DotNetty.Transport.Channels;

namespace IoT.Gateway.Demo
{
    public class Session
    {
        public string nodeId { get; set; }

        public IChannel channel { get; set; }

        public string deviceId { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Session{");
            sb.Append("nodeId='" + nodeId + '\'');
            sb.Append(", channel=" + channel);
            sb.Append(", deviceId='" + deviceId + '\'');
            sb.Append('}');
            return sb.ToString();
        }
    }
}
