using GreatEchoWall.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GreatEchoWall.Helpers
{
    public static class Storage
    {


        public static void save(Record record)
        {
            var log = new Log
            {
                Name = record.Name,
                Time = record.Time,
                Content = record.Content,
                Times = record.Times,
                Server = new EndPoint {
                    Ip = record.RemoteEndPoint.Address.ToString(),
                    Port = record.RemoteEndPoint.Port,
                },
                Client = new EndPoint {
                    Ip = record.LocalEndPoint.Address.ToString(),
                    Port = record.LocalEndPoint.Port,
                },
                Route = new Route
                {
                    Count = record.RouteCount,
                    Log = record.RouteLog,
                },
                Timetable = new Tableset
                {
                    Tcp = new Timetable
                    {
                        ConnectStart = record.TcpConnectStart,
                        ConnectEnd = record.TcpConnectEnd,
                        CloseStart = record.TcpCloseStart,
                        CloseEnd = record.TcpCloseEnd,
                        List = new List<Moment>(record.TcpMoments),
                    },
                    Udp = new Timetable
                    {
                        ConnectStart = record.UdpConnectStart,
                        ConnectEnd = record.UdpConnectEnd,
                        CloseStart = record.UdpCloseStart,
                        CloseEnd = record.UdpCloseEnd,
                        List = new List<Moment>(record.UdpMoments),
                    }
                }
            };
            var json = JsonConvert.SerializeObject(log, Formatting.Indented);
            var data = Encoding.UTF8.GetBytes(json);
            var filename = log.Time.ToString("yyyyMMddHHmmss") + "#" + log.Name + ".json";
            using (var fs = new FileStream("/GEW/SingleRecord/" + filename, FileMode.Create))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
            }
        }

        public static void save(IEnumerable<Record> records)
        {

        }

        class Log
        {
            public string Name { get; set; }
            public DateTime Time { get; set; }
            public string Content { get; set; }
            public int Times { get; set; }
            public EndPoint Server { get; set; }
            public EndPoint Client { get; set; }
            public Route Route { get; set; }
            public Tableset Timetable { get; set; }
        }

        class EndPoint
        {
            public string Ip { get; set; }
            public int Port { get; set; }
        }

        class Route
        {
            public int Count { get; set; }
            public string Log { get; set; }
        }

        class Tableset
        {
            public Timetable Tcp { get; set; }
            public Timetable Udp { get; set; }
        }

        class Timetable
        {
            public DateTime ConnectStart { get; set; }
            public DateTime ConnectEnd { get; set; }
            public DateTime CloseStart { get; set; }
            public DateTime CloseEnd { get; set; }
            public List<Moment> List { get; set; }
        }
    }
}
