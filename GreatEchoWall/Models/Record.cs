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
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint remoteEndPoint { get; set; }
        public int RouteCount { get; set; }
        public string RouteLog { get; set; }
        public DateTime TcpConnectStart { get; set; }
        public DateTime TcpConnectEnd { get; set; }
        public DateTime TcpCloseStart { get; set; }
        public DateTime TcpCloseEnd { get; set; }
        public List<Moment> TcpMoments { get; set; }
        public DateTime UdpConnectStart { get; set; }
        public DateTime UdpConnectEnd { get; set; }
        public DateTime UdpCloseStart { get; set; }
        public DateTime UdpCloseEnd { get; set; }
        public List<Moment> UdpMoments { get; set; }
    }
}
