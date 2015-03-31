using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GreatEchoWall.Models
{
    public class Record
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public string Content { get; set; }
        public int Length { get; set; }
        public int Times { get; set; }
        public long Frequency { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public int RouteCount { get; set; }
        public string RouteLog { get; set; }
        public long TcpConnectStart { get; set; }
        public long TcpConnectEnd { get; set; }
        public long TcpCloseStart { get; set; }
        public long TcpCloseEnd { get; set; }
        public Moment[] TcpMoments { get; set; }
        public long UdpConnectStart { get; set; }
        public long UdpConnectEnd { get; set; }
        public long UdpCloseStart { get; set; }
        public long UdpCloseEnd { get; set; }
        public Moment[] UdpMoments { get; set; }
    }
}
