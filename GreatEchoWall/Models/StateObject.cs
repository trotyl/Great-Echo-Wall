using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GreatEchoWall.Models
{
    public class StateObject
    {
        public Socket Socket { get; set; }
        public Record Record { get; set; }
    }
}
