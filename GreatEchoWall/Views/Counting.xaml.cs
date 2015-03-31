using GreatEchoWall.Helpers;
using GreatEchoWall.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
        private Process routeProcess { get; set; }
        public delegate void UIDelegate(string protocol, IEnumerable<KeyValuePair<int, long>> tcpPoints, IEnumerable<KeyValuePair<int, long>> udpPoints, int tcpIndex, int udpIndex, double tcpAverage, double udpAverage, int routeCount, string routeLog);

        [DllImport("kernel32.dll")]
        private extern static bool QueryPerformanceCounter(ref long x);

        public Counting()
        {
            InitializeComponent();
            InitializeDataContext();

            aTimer = new Timer(1000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Start();
        }

        public void Route()
        {
            ProcessStartInfo start = new ProcessStartInfo("Tracert.exe");
            start.Arguments = State.Record.RemoteEndPoint.Address.ToString();
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardInput = true;
            start.UseShellExecute = false;
            routeProcess = Process.Start(start);
            StreamReader reader = routeProcess.StandardOutput;
            string line;
            State.Record.RouteCount = -6;
            try
            {
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    State.Record.RouteCount += 1;
                    State.Record.RouteLog += line + "\n";
                }
                routeProcess.WaitForExit();
                routeProcess.Close();
                reader.Close();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
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
                var tcpDeltas = tcpRange.Select(x => x.RecvEnd - x.SendEnd).Select(x => x * 1000000 / State.Record.Frequency);
                tcpAverage = tcpMoments.Average(x => x.RecvEnd - x.SendEnd) * 1000000 / State.Record.Frequency;
                tcpPoints = tcpDeltas.Select((x, i) => new KeyValuePair<int, long>((tcpCount <= 20 ? 0 : tcpCount - 20) + i + 1, x));
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
                var udpDeltas = udpRange.Select(x => x.RecvEnd - x.SendEnd).Select(x => x * 1000000 / State.Record.Frequency);
                udpAverage = udpMoments.Average(x => x.RecvEnd - x.SendEnd) * 1000000 / State.Record.Frequency;
                udpPoints = udpDeltas.Select((x, i) => new KeyValuePair<int, long>((udpCount <= 20 ? 0 : udpCount - 20) + i + 1, x));
            }
            else
            {
                udpPoints = new KeyValuePair<int, long>[] { };
                udpAverage = -1;
            }

            var routeCount = State.Record.RouteCount;
            var routeLog = State.Record.RouteLog;

            window.Dispatcher.BeginInvoke(new UIDelegate(UpdatePoint), new object[] { "Tcp", tcpPoints, udpPoints, tcpCount, udpCount, tcpAverage, udpAverage, routeCount, routeLog});

            if (tcpCount == State.Record.Times || udpCount == State.Record.Times || routeCount > 0)
            {
                Storage.save(State.Record);
            }

            if (tcpCount == State.Record.Times && udpCount == State.Record.Times && routeCount > 0)
            {
                aTimer.Stop();
                aTimer.Dispose();
            }
        }

        public void Start()
        {
            var tcp = State.TcpSocket;
            var udp = State.UdpSocket;
            var record = State.Record;
            long now = 0;
            if (tcp != null)
            {
                tcp.BeginConnect(record.RemoteEndPoint, ConnectOver, "Tcp");
                QueryPerformanceCounter(ref now);
                record.TcpConnectStart = now;
            }
            if (udp != null)
            {
                udp.BeginConnect(record.RemoteEndPoint, ConnectOver, "Udp");
                QueryPerformanceCounter(ref now);
                record.UdpConnectStart = now;
            }
        }

        private void ConnectOver(IAsyncResult ar)
        {
            long now = 0;
            var record = State.Record;
            Socket socket;
            if (ar.AsyncState as string == "Tcp")
            {
                socket = State.TcpSocket;
                QueryPerformanceCounter(ref now);
                record.TcpConnectEnd = now;
            }
            else
            {
                socket = State.UdpSocket;
                QueryPerformanceCounter(ref now);
                record.UdpConnectEnd = now;
            }
            record.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;

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

            long now = 0;
            
            for (int i = 0; i < record.Times; i++)
            {
                var moment = new Moment();
                try
                {
                    var sendBuff = Encoding.UTF8.GetBytes(record.Content);
                    QueryPerformanceCounter(ref now);
                    moment.SendStart = now;
                    socket.Send(sendBuff);
                    QueryPerformanceCounter(ref now);
                    moment.SendEnd = now;
                    var length = socket.Receive(recvBuff);
                    QueryPerformanceCounter(ref now);
                    moment.RecvEnd = now;
                    var res = Encoding.UTF8.GetString(recvBuff, 0, length);
                    var moments = (protocol == "Tcp" ? record.TcpMoments : record.UdpMoments);
                    moments[i] = moment;
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.Message);
                }
            }

            if (protocol == "Tcp")
            {
                QueryPerformanceCounter(ref now);
                record.TcpCloseStart = now;
                socket.Close();
                QueryPerformanceCounter(ref now);
                record.TcpConnectEnd = now;
            }
            else
            {
                QueryPerformanceCounter(ref now);
                record.UdpCloseStart = now;
                socket.Close();
                QueryPerformanceCounter(ref now);
                record.UdpConnectEnd = now;
            } 
        }

        private void UpdatePoint(string protocol, IEnumerable<KeyValuePair<int, long>> tcpPoints, IEnumerable<KeyValuePair<int, long>> udpPoints, int tcpIndex, int udpIndex, double tcpAverage, double udpAverage, int routeCount, string routeLog)
        {
            lineChart.DataContext = new
            {
                TCP = tcpPoints,
                UDP = udpPoints,
            };
            tcpIndexBlock.Text = tcpIndex.ToString();
            udpIndexBlock.Text = udpIndex.ToString();
            tcpAverageBlock.Text = tcpAverage.ToString("f2");
            udpAverageBlock.Text = udpAverage.ToString("f2");
            routeBlock.Text = routeLog;
        }

        private void InitializeDataContext()
        {
            lineChart.DataContext = new
            {
                TCP = new List<KeyValuePair<int, long>>(),
                UDP = new List<KeyValuePair<int, long>>(),
            };
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            aTimer.Dispose();
            State.TcpSocket.Dispose();
            State.UdpSocket.Dispose();
            try
            {
                routeProcess.Kill();
                routeProcess.Dispose();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }
    }
}
