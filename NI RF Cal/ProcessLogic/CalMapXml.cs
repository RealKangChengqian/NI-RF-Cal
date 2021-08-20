using NACE.Utils.Data.SnP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ProcessLogic
{
    /// <summary>
    ///用于创建、写入或读取CalibrationMap文件
    ///CalibrationMap文件用于map所有的path与所有的sweepsetting，s2pfile
    /// </summary>
    class CalMapXml
    {
        private string FileDirectory { get; set; }
        internal List<Path> signalPaths { get; } = new List<Path>();
        internal List<CalSweepSetting> sweepSettings { get; set; } = new List<CalSweepSetting>();
        internal List<PathLoss> PathLosses { get; set; } = new List<PathLoss>();
        internal void SetSweepSettings(string Name, Calibratype type, int numberofpoints)
        {

            if ((from a in sweepSettings where a.sweepSettingName == Name select a).Count() == 0)
                sweepSettings.Add(new CalSweepSetting
                {
                    sweepSettingName = Name,
                    type = type,
                    numberOfPoints = numberofpoints
                });
        }
        internal void SetSweepSettings(List<CalSweepSetting> sweepSettings) 
        {
            foreach (var item in sweepSettings)
            {
                var flags = from a in this.sweepSettings
                            where a.sweepSettingName == item.sweepSettingName
                            select a;
                if (flags.Count() == 0) this.sweepSettings.Add(item);
            }
        }
        internal void SetPath(Path path)
        {
            var flags = from a in signalPaths
                        where a.Name == path.Name
                        select a;
            if (flags.Count() == 0) signalPaths.Add(path);
            else return;
        }
        internal void SetPath(string Name, Calibratype type)
        {
            var flags = from a in signalPaths
                        where a.Name == Name
                        select a;
            if (flags.Count() == 0) signalPaths.Add(new Path { Name = Name, Type = type });
            else return;
        }
        internal void SetPath(List<Path> SignalPaths)
        {
            foreach (var item in SignalPaths)
            {
                var flags = from a in signalPaths
                            where a.Name == item.Name
                            select a;
                if (flags.Count() == 0) signalPaths.Add(item);
            }
        }
        internal void SetPathLoss(Path path, SweepSetting sweepSetting, int sweepSettingIndex, double frequency, double power, string s2pName)
        {
            PathLoss pathLoss = new PathLoss
            {
                Path = path.Name,
                SSweepSetting = new CalSweepSetting
                {
                    sweepSettingName = sweepSetting.Name,
                    numberOfPoints = sweepSetting.GetNumberOfPoint(),
                    type = sweepSetting.type
                },
                SweepSettingIndex = sweepSettingIndex,
                Frequency = frequency,
                Power = power,
                S2pName = s2pName
            };
            if (path.Type == sweepSetting.type)
                PathLosses.Add(pathLoss);
        }
        internal void SetPathLosses(List<PathLoss> PathLosses)
        {
            foreach (var item in PathLosses)
            {
                var flags = from a in this.PathLosses
                            where a.Path == item.Path&& a.SSweepSetting.sweepSettingName == item.SSweepSetting.sweepSettingName&&a.SweepSettingIndex==item.SweepSettingIndex
                            select a;
                if (flags.Count() == 0) this.PathLosses.Add(item);
            }
        }
        //将这个类里存放的数据类型以xml文件的形式存放
        public void Save(string filepath)
        {
            XElement xmlDatatime = new XElement("DateTime", DateTime.Now.ToString("s"));

            XElement xmlSignalPaths = new XElement("SignalPaths");
            foreach (var item in signalPaths)
                xmlSignalPaths.Add(new XElement("Path",
                                                  new XAttribute("PathName", item.Name), new XAttribute("Type", item.Type)));

            XElement xmlSweepSettings = new XElement("SweepSettings");
            foreach (var item in sweepSettings)
                xmlSweepSettings.Add(new XElement("SweepSetting",
                                                       new XAttribute("Name", item.sweepSettingName), new XAttribute("Type", item.type), new XAttribute("NumberOfPoints", item.numberOfPoints.ToString())));

            XElement xmlPathlosses = new XElement("Pathlosses");
            foreach (var item in PathLosses)
                xmlPathlosses.Add(new XElement("Pathloss",
                                                new XAttribute("PathName", item.Path), new XAttribute("SweepSetting", item.SSweepSetting.sweepSettingName), new XAttribute("SweepSettingIndex", item.SweepSettingIndex), new XAttribute("Frequency", item.Frequency.ToString()), new XAttribute("Power", item.Power.ToString()), new XAttribute("S2pFile", item.S2pName)));

            XElement calibration = new XElement("Calibration", xmlDatatime, xmlSignalPaths, xmlSweepSettings, xmlPathlosses);
            XDocument s2pInstructionXml = new XDocument();
            s2pInstructionXml.Add(calibration);//把根元素添加到文档中
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filepath)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath));
            if (filepath.EndsWith(".xml"))
            {
                FileInfo file = new FileInfo(filepath);
                if (file.Exists)
                    file.Create();
                s2pInstructionXml.Save(filepath);
            }
            else
            {
                if (!File.Exists(filepath + ".xml"))
                    File.Create(filepath + ".xml");
                s2pInstructionXml.Save(filepath + ".xml");
            }
        }
        public void Load(string FileDirectory)
        {
            this.FileDirectory = FileDirectory;
            var xmlPath = FindXmlFile(FileDirectory);//选取Xml文件*/
            if (xmlPath == null)
                return;
            XElement root = XElement.Load(xmlPath);
            if (root.Element("SignalPaths") != null)
            {
                SetPath((from p in root.Element("SignalPaths").Elements()
                                   select new Path
                                   {
                                       Name = p.Attribute("PathName").Value,
                                       Type = (Calibratype)(Enum.Parse(typeof(Calibratype), p.Attribute("Type").Value))
                                   }).ToList());

            }//else//每一个if后都该有一个else报出错误信息退出
            else return;
            if (root.Element("SweepSettings") != null)
            {
                this.SetSweepSettings((from s in root.Element("SweepSettings").Elements()
                                 select new CalSweepSetting
                                 {
                                     sweepSettingName = s.Attribute("Name").Value,
                                     type = (Calibratype)(Enum.Parse(typeof(Calibratype), s.Attribute("Type").Value)),
                                     numberOfPoints = int.Parse(s.Attribute("NumberOfPoints").Value)
                                 }).ToList());
            }
            else return;
            if (root.Element("Pathlosses") != null)
            {
                SetPathLosses(
                        (from pl in root.Element("Pathlosses").Elements()
                                      select new PathLoss
                                      {
                                          Path= pl.Attribute("PathName").Value,
                                          SSweepSetting =(from a in sweepSettings 
                                                         where a.sweepSettingName == pl.Attribute("SweepSetting").Value 
                                                         select a).First(),
                                          SweepSettingIndex =int.Parse( pl.Attribute("SweepSettingIndex").Value),
                                          Frequency= double.Parse(pl.Attribute("Frequency").Value),
                                          Power= double.Parse(pl.Attribute("Power").Value),
                                          S2pName = pl.Attribute("S2pFile").Value,
                                      }).ToList()
                    ) ;
            }
            else return;
        }
        private string FindXmlFile(string fileDirectory)//找到xml文件
        {
            DirectoryInfo xml = new DirectoryInfo(fileDirectory);
            var file = xml.GetFiles();
            foreach (var path in file)
            {
                if (path.Name.Substring(path.Name.LastIndexOf("."), path.Name.Length - path.Name.LastIndexOf(".")).ToLower() == ".xml")
                {
                    return (path.DirectoryName + "\\" + path.Name);
                }
            }
            return null;
        }
        internal bool JugeType(string pathName, Calibratype calibratype)
        {
            var pathType = (from t in signalPaths
                            where t.Name == pathName
                            select t.Type);
            if (pathType.FirstOrDefault() == calibratype)
                return true;
            else return false;
        }
        internal bool JugeType(string pathName, string sweepSettingName, Calibratype calibratype)//判断path与sweepsetting的类型是否相同
        {
            var pathType = (from sp in signalPaths
                            where sp.Name == pathName
                            select sp.Type).FirstOrDefault();
            var sweepSettingType = (from s in sweepSettings
                                    where s.sweepSettingName == sweepSettingName
                                    select s.type).FirstOrDefault();
            if (sweepSettingType == calibratype && sweepSettingType == pathType)
                return true;
            else return false;
        }
        internal string GetS2pFileName(string path, double frequency, double power)
        {
            return (from sf in PathLosses
                    where sf.Path == path && sf.Frequency == frequency && sf.Power == power
                    select sf.S2pName).FirstOrDefault();
        }
        internal PathLoss GetPathLoss(string path, string sweepSettingName, int sweepSettingIndex)//从Load（）函数已经存好的数据结构中，取出S2pfilename
        {
            return (from sf in PathLosses
                    where sf.Path == path && sf.SweepSettingIndex == sweepSettingIndex && sf.SSweepSetting.sweepSettingName == sweepSettingName
                    select sf).First();
        }
        internal double GetOffset(string s2pPath, double frequency)//获取S2pfile中对应的频率的值
        {
            int i = 0;
            var snpFile = new SnPfile(System.IO.Path.Combine(FileDirectory, s2pPath+".s2p"));
            double[,,] readRe = new double[snpFile.FrequencyList.Count(), 2, 2];
            if (snpFile.FrequencyList.Count() != 0)
            {
                snpFile.GetSParameters(out readRe, out double[,,] im);
                foreach (var fre in snpFile.FrequencyList)
                {
                    if (fre == frequency)
                        break;
                    else
                        i++;
                }
            }
            if (i != snpFile.FrequencyList.Count())
                return Math.Log10(Math.Pow(readRe[i, 1, 0], 2)) * (-10);
            else return 0;
        }
        internal class CalSweepSetting
        {
            public string sweepSettingName { get; set; }
            public Calibratype type { get; set; }
            public int numberOfPoints { get; set; }
        }
        internal class PathLoss
        {
            internal string Path { get; set; }
            internal CalSweepSetting SSweepSetting { get; set; }
            internal int SweepSettingIndex { get; set; }
            internal double Frequency { get; set; }
            internal double Power { get; set; }
            internal string S2pName { get; set; }
        }
    }
}
