using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NACE.Utils.Data;

namespace ProcessLogic
{
    class PinMapToolsClass
    {
        //这个类用来表示板卡和DUT的一条连接关系，并构造此连接的名称
        public class Connection
        {
            public string dutPin { get; set; }
            public string port { get; set; }
            string site { get; set; }
            string instrument { get; set; }
            string channal { get; set; }
            //复写toString方法构造连接名称
            public override string ToString()
            {
                return $"{port}TOSite{site}{dutPin}";
            }
            public Connection(string dutPin, string site, string instrument, string channal)
            {
                this.dutPin = dutPin;
                this.site = site;
                this.instrument = instrument;
                this.channal = channal;
                this.port = instrument + "-" + channal;
            }
        }
        /// <summary>
        /// 从Pinmap文件里读取connections信息
        /// pin索引port
        /// </summary>
        public class ReadPinmapFile
        {
            public List<Connection> GetConnection(string xmlPath)
            {
                char[] c;
                if (File.Exists(xmlPath))
                {
                    FileStream fileStream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read);
                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, bytes.Length);
                    c = Encoding.UTF8.GetChars(bytes);
                    fileStream.Close();
                    FileStream newfileStream = new FileStream(xmlPath.Replace(".pinmap", ".xml"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    byte[] newbytes = Encoding.UTF8.GetBytes(c);
                    newfileStream.Write(newbytes, 0, newbytes.Length);
                    newfileStream.Flush();
                    newfileStream.Close();
                }
                else
                    return null;
                List<Connection> connections = new List<Connection>();

                XDocument xd = XDocument.Load(xmlPath.Replace(".pinmap", ".xml"));
                var nsMgr = new XmlNamespaceManager(new NameTable());
                nsMgr.AddNamespace("xsd", "http://www.ni.com/TestStand/SemiconductorModule/PinMap.xsd");
                Console.WriteLine(nsMgr.LookupNamespace("xsd"));
                var v = xd.XPathSelectElements("//xsd:Connections/xsd:Connection", nsMgr);

                foreach (var item in v.ToList())
                {

                    Connection connection = new Connection(item.Attribute("pin").ToString().Substring(item.Attribute("pin").ToString().IndexOf("=") + 1).Replace("\"", ""),
                                                           item.Attribute("siteNumber").ToString().Substring(item.Attribute("siteNumber").ToString().IndexOf("=") + 1).Replace("\"", ""),
                                                           item.Attribute("instrument").ToString().Substring(item.Attribute("instrument").ToString().IndexOf("=") + 1).Replace("\"", ""),
                                                           item.Attribute("channel").ToString().Substring(item.Attribute("channel").ToString().IndexOf("=") + 1).Replace("\"", "")
                                                           );
                    connections.Add(connection);
                }
                return connections;
            }
            //把连接名与管脚名对应，便于以后的设置
            public string[] PathsToPins(string[] paths)
            {
                List<string> Pins = new List<string>();
                foreach (var item in paths)
                {
                    Pins.Add(item.Substring(item.LastIndexOf("TO") + 1));
                }
                return Pins.ToArray();
            }
            public string PinToPort(List<Connection> connections, string pin)
            {
                string haha = null;
                foreach (var item in connections)
                {
                    if (item.dutPin == pin)
                        haha = item.port;
                }
                return haha;
            }
        }
    }
}
