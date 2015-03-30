using GreatEchoWall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GreatEchoWall.Views
{
    /// <summary>
    /// Counting.xaml 的交互逻辑
    /// </summary>
    public partial class Counting : Window
    {
        public StateObject State { get; set; }
        private Timer aTimer { get; set; }
        public delegate void UIDelegate(string protocol, IEnumerable<KeyValuePair<int, long>> tcpPoints, IEnumerable<KeyValuePair<int, long>> udpPoints, int tcpIndex, int udpIndex, double tcpAverage, double udpAverage);

        public Counting()
        {
            InitializeComponent();
            InitializeDataContext();

            aTimer = new Timer(1000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Start();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            IEnumerable<KeyValuePair<int, long>> tcpPoints, udpPoints;
            double tcpAverage, udpAverage;

            var tcpMoments = State.Record.TcpMoments.Where(x => x != null);
            var tcpCount = tcpMoments.Count();
            if (tcpCount != 0)
            {
                var tcpRange = tcpMoments.Skip(tcpCount <= 20 ? 0 : tcpCount - 20);
                var tcpDeltas = tcpRange.Select(x => x.RecvEnd.Ticks - x.SendEnd.Ticks);
                tcpAverage = tcpMoments.Average(x => x.RecvEnd.Ticks - x.SendEnd.Ticks);
                tcpPoints = tcpDeltas.Select((x, i) => new KeyValuePair<int, long>((tcpCount < 20 ? 0 : tcpCount - 20) + i + 1, x));
            }
            else
            {
                tcpPoints = new KeyValuePair<int, long>[] { };
                tcpAverage = -1;
            }

            var udpMoments = State.Record.UdpMoments.Where(x => x != null);
            var udpCount = udpMoments.Count();
            if (udpCount != 0)
            {
                var udpRange = udpMoments.Skip(udpCount <= 20 ? 0 : udpCount - 20);
                var udpDeltas = udpRange.Select(x => x.RecvEnd.Ticks - x.SendEnd.Ticks);
                udpAverage = udpMoments.Average(x => x.RecvEnd.Ticks - x.SendEnd.Ticks);
                udpPoints = udpDeltas.Select((x, i) => new KeyValuePair<int, long>((udpCount < 20 ? 0 : udpCount - 20) + i + 1, x));
            }
            else
            {
                udpPoints = new KeyValuePair<int, long>[] { };
                udpAverage = -1;
            }
            window.Dispatcher.BeginInvoke(new UIDelegate(UpdatePoint), new object[] { "Tcp", tcpPoints, udpPoints, tcpCount, udpCount, tcpAverage, udpAverage});
        }

        public void Start()
        {
            var tcp = State.TcpSocket;
            var udp = State.UdpSocket;
            var record = State.Record;
            if (tcp != null)
            {
                tcp.BeginConnect(record.RemoteEndPoint, ConnectOver, "Tcp");
                record.TcpConnectStart = DateTime.Now;
            }
            if (udp != null)
            {
                udp.BeginConnect(record.RemoteEndPoint, ConnectOver, "Udp");
                record.UdpConnectStart = DateTime.Now;
            }
        }

        private void ConnectOver(IAsyncResult ar)
        {
            var now = DateTime.Now;
            var record = State.Record;
            Socket socket;
            if (ar.AsyncState as string == "Tcp")
            {
                socket = State.TcpSocket;
                record.TcpConnectEnd = now;
            }
            else
            {
                socket = State.UdpSocket;
                record.UdpConnectEnd = now;
            }
            try
            {
                socket.EndConnect(ar);
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
                socket.Close();
                return;
            }
            Looping(ar.AsyncState as string);
        }

        private void Looping(string protocol)
        {
            var record = State.Record;
            Socket socket;
            if (protocol == "Tcp")
            {
                socket = State.TcpSocket;
            }
            else
            {
                socket = State.UdpSocket;
            }

            var recvBuff = new byte[1048576];
            
            for (int i = 0; i < record.Times; i++)
            {
                var moment = new Moment();
                try
                {
                    var sendBuff = Encoding.UTF8.GetBytes(record.Content);
                    moment.SendStart = DateTime.Now;
                    socket.Send(sendBuff);
                    moment.SendEnd = DateTime.Now;
                    var length = socket.Receive(recvBuff);
                    moment.RecvEnd = DateTime.Now;
                    var res = Encoding.UTF8.GetString(recvBuff, 0, length);
                    var moments = (protocol == "Tcp" ? record.TcpMoments : record.UdpMoments);
                    moments[i] = moment;
                    Console.WriteLine(sendBuff.Length + " " + length);
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.Message);
                }
            }
            socket.Close();
            aTimer.Stop();
            aTimer.Dispose();
            OnTimedEvent(null, null);
        }

        private void UpdatePoint(string protocol, IEnumerable<KeyValuePair<int, long>> tcpPoints, IEnumerable<KeyValuePair<int, long>> udpPoints, int tcpIndex, int udpIndex, double tcpAverage, double udpAverage)
        {
            lineChart.DataContext = new
            {
                TCP = tcpPoints,
                UDP = udpPoints,
            };
            tcpIndexBlock.Text = tcpIndex.ToString();
            udpIndexBlock.Text = udpIndex.ToString();
            tcpAverageBlock.Text = tcpAverage.ToString();
            udpAverageBlock.Text = udpAverage.ToString();
        }

        private void InitializeDataContext()
        {
            lineChart.DataContext = new
            {
                TCP = new List<KeyValuePair<int, long>>(),
                UDP = new List<KeyValuePair<int, long>>(),
            };
        }
    }
}
