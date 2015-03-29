using GreatEchoWall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public delegate void UIDelegate(int i, long delta, double average);

        public Counting()
        {
            InitializeComponent();
            InitializeDataContext();
        }

        public void Start()
        {
            var socket = State.Socket;
            var record = State.Record;
            for (int i = 0; i < record.Times; i++)
            {
                record.TcpMoments[i] = new Moment();
                try
                {
                    var sendBuff = Encoding.UTF8.GetBytes(record.Content);
                    var recvBuff = new byte[1048576];
                    record.TcpMoments[i].SendStart = DateTime.Now;
                    socket.Send(sendBuff);
                    record.TcpMoments[i].SendEnd = DateTime.Now;
                    var length = socket.Receive(recvBuff);
                    record.TcpMoments[i].RecvEnd = DateTime.Now;
                    var res = Encoding.UTF8.GetString(recvBuff, 0, length);
                    var tmp = record.TcpMoments;
                    var average = record.TcpMoments.Where(x => x != null).Average(x => x.RecvEnd.Ticks - x.SendEnd.Ticks);
                    window.Dispatcher.BeginInvoke(new UIDelegate(AddPoint), new object[] { i, record.TcpMoments[i].RecvEnd.Ticks - record.TcpMoments[i].SendEnd.Ticks, average });
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.Message);
                }
            }
            socket.Close();
        }

        private void AddPoint(int i, long delta, double average)
        {
            var context = lineChart.DataContext as dynamic;
            var tcps = context.TCP as List<KeyValuePair<int, long>>;
            tcps.Add(new KeyValuePair<int, long>(i + 1, delta));
            if (tcps.Count > 20)
            {
                tcps.RemoveAt(0);
            }
            lineChart.DataContext = null;
            lineChart.DataContext = context;

            indexBlock.Text = (i + 1).ToString();
            averageBlock.Text = average.ToString();
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
