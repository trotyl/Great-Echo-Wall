using GreatEchoWall.Models;
using GreatEchoWall.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreatEchoWall
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Counting counting;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            box.SelectAll();
            box.PreviewMouseDown -= TextBox_PreviewMouseDown;
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var box = sender as TextBox;
            box.Focus();
            e.Handled = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            box.PreviewMouseDown += TextBox_PreviewMouseDown;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            var name = string.IsNullOrEmpty(nameBox.Text) ? "未命名" : nameBox.Text;
            var time = DateTime.Now.ToString("yyyyMMddHHmmss");
            Console.WriteLine(DateTime.Now.Ticks + " Test Start!");

            IPAddress remoteAddress;
            if (!IPAddress.TryParse(serverIpBox.Text, out remoteAddress))
            {
                MessageBox.Show("服务器IP地址（" + serverIpBox.Text + "）不是有效的IP地址！");
                return;
            }

            int remotePort;
            if (!int.TryParse(serverPortBox.Text, out remotePort) || remotePort <= 0 || remotePort >= 65536)
            {
                MessageBox.Show("服务器端口号（" + serverPortBox.Text + "）不是有效的端口号！");
                return;
            }

            if (!(tcpBox.IsChecked ?? false) && !(udpBox.IsChecked ?? false))
            {
                MessageBox.Show("未选择协议类型！");
                return;
            }

            int times;
            if (!int.TryParse(timesBox.Text, out times) || times <= 0 || times >= 65536)
            {
                MessageBox.Show("传输次数（" + timesBox.Text + "）非法！");
                return;
            }

            int length;
            string message, messageNotation;
            if (!(isRandomBox.IsChecked ?? false))
            {
                if (string.IsNullOrEmpty(messageBox.Text))
                {
                    MessageBox.Show("传输内容（" + messageBox.Text + "）为空！");
                    return;
                }
                message = messageNotation = messageBox.Text;
                length = message.Length;
            }
            else
            {
                if (!int.TryParse(lengthBox.Text, out length) || length <= 0 || length >= 1048576)
                {
                    MessageBox.Show("随机内容长度（" + lengthBox.Text + "）非法！");
                    return;
                }
                message = new string('X', length);
                messageNotation = "$Random$" + length.ToString();
            }

            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);

            var state = new StateObject
            {
                Record = new Record
                {
                    Content = message,
                    Length = length,
                    Name = name,
                    RemoteEndPoint = remoteEndPoint,
                    RouteCount = 0,
                    RouteLog = "",
                    TcpMoments = new Moment[times],
                    UdpMoments = new Moment[times],
                    Time = DateTime.Now,
                    Times = times,
                }
            };

            if (tcpBox.IsChecked ?? false)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                state.TcpSocket = socket;
            }

            if (udpBox.IsChecked ?? false)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                state.UdpSocket = socket;
            }

            counting = new Counting();
            counting.State = state;
            counting.Show();
            Thread bg = new Thread(counting.Start);
            bg.Start();

            //ProcessStartInfo start = new ProcessStartInfo("Tracert.exe");
            //start.Arguments = serverIpBox.Text;
            //start.CreateNoWindow = true;
            //start.RedirectStandardOutput = true;
            //start.RedirectStandardInput = true;
            //start.UseShellExecute = false;
            //Process p = Process.Start(start);
            //StreamReader reader = p.StandardOutput;
            //string line;
            //while (!reader.EndOfStream)
            //{
            //    line = reader.ReadLine();
            //    state.Record.RouteCount += 1;
            //    state.Record.RouteLog += line + "\n";
            //}
            //state.Record.RouteCount -= 6;
            //Console.WriteLine(state.Record.RouteLog);
            //p.WaitForExit();
            //p.Close();
            //reader.Close();
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            nameBox.Text = "";
            serverIpBox.Text = "127.0.0.1";
            serverPortBox.Text = "7";
            tcpBox.IsChecked = true;
            udpBox.IsChecked = true;
            timesBox.Text = "100";
            messageBox.Text = "This is a test message.";
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            messageBox.IsEnabled = false;
            lengthBox.IsEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            messageBox.IsEnabled = true;
            lengthBox.IsEnabled = false;
        }
    }
}
