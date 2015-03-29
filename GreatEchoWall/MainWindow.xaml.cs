using GreatEchoWall.Models;
using GreatEchoWall.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
                    TcpMoments = new Moment[times],
                    Time = DateTime.Now,
                    Times = times,
                }
            };

            counting = new Counting();
            counting.State = state;
            counting.Show();

            if (tcpBox.IsChecked ?? false)
            {
                try
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    state.Socket = socket;
                    socket.BeginConnect(remoteEndPoint, TcpConnectOver, state);
                    state.Record.TcpConnectStart = DateTime.Now;
                    Console.WriteLine(DateTime.Now.Ticks + " Connect Called!");
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
            }

            if (udpBox.IsChecked ?? false)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

        }

        private void TcpConnectOver(IAsyncResult ar)
        {
            var now = DateTime.Now;
            var state = ar.AsyncState as StateObject;
            var socket = state.Socket;
            var record = state.Record;
            record.TcpConnectEnd = now;
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

            counting.Start();

            //for (int i = 0; i < record.Times; i++)
            //{
            //    record.TcpMoments[i] = new Moment();
            //    try
            //    {
            //        var sendBuff = Encoding.UTF8.GetBytes(record.Content);
            //        var recvBuff = new byte[1048576];
            //        record.TcpMoments[i].SendStart = DateTime.Now;
            //        socket.Send(sendBuff);
            //        record.TcpMoments[i].SendEnd = DateTime.Now;
            //        Console.WriteLine(DateTime.Now.Ticks + " Send Over!");
            //        var length = socket.Receive(recvBuff);
            //        Console.WriteLine(DateTime.Now.Ticks + " Receive Over!");
            //        record.TcpMoments[i].RecvEnd = DateTime.Now;
            //        var res = Encoding.UTF8.GetString(recvBuff, 0, length);
            //        Console.WriteLine(DateTime.Now.Ticks + " Res: " + res);
            //    }
            //    catch (Exception ee)
            //    {
            //        MessageBox.Show(ee.Message);
            //    }
            //}
            //socket.Close();
        }

        //private void TcpSendOver(IAsyncResult ar)
        //{
        //    var dic = ar.AsyncState as dynamic;
        //    var socket = dic.Socket as Socket;
        //    var buff = dic.Buff as byte[];
        //    try
        //    {
        //        socket.EndSend(ar);
        //        socket.BeginReceive(buff, 0, 1048576, SocketFlags.None, TcpRecvOver, ar.AsyncState);
        //        Console.WriteLine("Receive Called!");
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
        //    }
        //}

        //private void TcpRecvOver(IAsyncResult ar)
        //{
        //    var dic = ar.AsyncState as dynamic;
        //    var socket = dic.Socket as Socket;
        //    var buff = dic.Buff as byte[];
        //    var tick = dic.Tick as long?;
        //    try
        //    {
        //        var length = socket.EndReceive(ar);
        //        socket.Close();
        //        var res = Encoding.UTF8.GetString(buff, 0, length);
        //        Console.WriteLine(length);
        //        Console.WriteLine(res);
        //        var newtick = DateTime.Now.Ticks;
        //        Console.WriteLine(newtick - tick);
        //        Console.WriteLine("Close Called!");
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
        //    }
        //}

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            nameBox.Text = "";
            serverIpBox.Text = "58.96.191.131";
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
