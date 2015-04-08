using ExcelBuilder;
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
            start.Arguments = State.Record.RemoteAddress;
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
                Console.WriteLine("074" + ee.Message);
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
                tcp.BeginConnect(record.RemoteAddress, record.RemotePort, ConnectOver, "Tcp");
                QueryPerformanceCounter(ref now);
                record.TcpConnectStart = now;
            }
            if (udp != null)
            {
                udp.BeginConnect(record.RemoteAddress, record.RemotePort, ConnectOver, "Udp");
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
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            record.LocalAddress = endPoint.Address.ToString();
            record.LocalPort = endPoint.Port;

            try
            {
                socket.EndConnect(ar);
            }
            catch (Exception ee)
            {
                Console.WriteLine("177" + ee.Message);
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

            var recvBuff = new byte[16364];
            Console.WriteLine("new 16384 bytes at 198");

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
                    Console.WriteLine("221" + ee.Message);
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
            try
            {
                aTimer.Dispose();
            }
            catch (Exception ee)
            {
                Console.WriteLine("274" + ee.Message);
            }
            try
            {
                routeProcess.Kill();
                routeProcess.Dispose();
            }
            catch (Exception ee)
            {
                Console.WriteLine("283" + ee.Message);
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            var builder = new Builder();

            var values = new List<List<dynamic>>
            {
                new List<dynamic> { "名称", State.Record.Name },
                new List<dynamic> { "时间", State.Record.Time },
                new List<dynamic> { "内容", State.Record.Content },
                new List<dynamic> { "长度", State.Record.Length },
                new List<dynamic> { "次数", State.Record.Times },
                new List<dynamic> { "时钟频率", State.Record.Frequency },
                new List<dynamic> { "远程主机地址", State.Record.RemoteAddress },
                new List<dynamic> { "远程主机端口", State.Record.RemotePort },
                new List<dynamic> { "本地主机地址", State.Record.LocalAddress },
                new List<dynamic> { "本地主机端口", State.Record.LocalPort },
                new List<dynamic> { "路由跳数", State.Record.RouteCount },
                new List<dynamic> { "路由日志", State.Record.RouteLog },
                new List<dynamic> { "路由跳数", State.Record.RouteCount },
                new List<dynamic> { "TCP连接开始计数", State.Record.TcpConnectStart },
                new List<dynamic> { "TCP连接结束计数", State.Record.TcpConnectStart },
                new List<dynamic> { "TCP关闭开始计数", State.Record.TcpConnectStart },
                new List<dynamic> { "TCP关闭结束计数", State.Record.TcpConnectStart },
                new List<dynamic> { "UDP连接开始计数", State.Record.UdpConnectStart },
                new List<dynamic> { "UDP连接结束计数", State.Record.UdpConnectStart },
                new List<dynamic> { "UDP关闭开始计数", State.Record.UdpConnectStart },
                new List<dynamic> { "UDP关闭结束计数", State.Record.UdpConnectStart },
                new List<dynamic> { "TCP发送接收计数" },
            };

            foreach (var moment in State.Record.TcpMoments)
            {
                values.Add(new List<dynamic> { moment.SendStart, moment.SendEnd, moment.RecvEnd });
            }

            values.Add(new List<dynamic> { "UDP发送接收计数" });

            foreach (var moment in State.Record.TcpMoments)
            {
                values.Add(new List<dynamic> { moment.SendStart, moment.SendEnd, moment.RecvEnd });
            }
            
            builder.AddSheet(values);
            builder.Save(State.Record.Time.ToString("yyyyMMddHHmmss") + "#" + State.Record.Name);
        }
    }
}
