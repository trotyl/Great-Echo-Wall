using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEchoWall.Models
{
    public class Moment
    {
        public long SendStart { get; set; }
        public long SendEnd { get; set; }
        public long RecvEnd { get; set; }
    }
}
