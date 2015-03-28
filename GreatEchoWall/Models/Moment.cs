using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEchoWall.Models
{
    public class Moment
    {
        public DateTime SendStart { get; set; }
        public DateTime SendEnd { get; set; }
        public DateTime RecvEnd { get; set; }
    }
}
