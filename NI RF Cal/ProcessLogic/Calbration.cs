using NACE.Utils.Data;
using System.Collections.Generic;
using System.Linq;
using NACE.Utils.Data.SnP;
using System.IO;
using System.IO.Compression;
using System;

namespace ProcessLogic
{
    abstract class  Calbration
    {
        internal KeyValuePair<CalibrationSetting,string> calibrationSettingElement { get; set; }
        public abstract CalibrationResult PerformAction();
    }
    class SourceCalibration : Calbration
    {
        SignalGenerator signalGenerator { get; set; } = new SignalGenerator();
        PowerMeter PowerMeter { get; set; } = new PowerMeter();
        public override CalibrationResult PerformAction()
        {
            //TODO:未来在WinForm重写
            Console.WriteLine("Source Calibration:");
            Console.WriteLine($"Please connect the Power Meter to: {calibrationSettingElement.Key.Path.Name.ToString()}, and press ENTER to continue");
        tips:
            var temp = Console.ReadKey();
            char key = temp.KeyChar;
            if (key.Equals('\r'))
            {
                //拿取波形发生器应设置的功率，由SweepSetting提供
                double GeneratePower = calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Power;
                //仪器在外边已打开
                //硬件做相应动作
                signalGenerator.Generate(calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Frequency, GeneratePower);
                //获取功率计测量得到的功率
                double MeasurePower=PowerMeter.Measure();
                //计算得到路径上的功率损失,衰减是正数
                double offset = GeneratePower - MeasurePower;
                //S2P数值
                double S2pValue = Math.Sqrt(Math.Pow(10, -(offset / 10)));
                SParameters sParameters = new SParameters
                {
                    Frequency = calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Frequency,
                    Real = new double[,]
                                {
                                    { 0, S2pValue },
                                    { S2pValue, 0 },
                                },
                    Imag = new double[,]
                                {
                                    { 0, 0 },
                                    { 0, 0 },
                                }
                };
                return new CalibrationResult(calibrationSettingElement.Value, sParameters);
            }
            else
                goto tips;
        }
        public SourceCalibration(KeyValuePair<CalibrationSetting, string> skr, SignalGenerator sg, PowerMeter pm) 
        {
            calibrationSettingElement = skr;
            signalGenerator = sg;
            PowerMeter = pm;
        }
    }
    internal class CalibrationSetting
    {
        public CalibrationSetting(Path path, SweepSetting sweepSetting, int sweepSettingIndex) 
        {
            Path = path; SweepSetting = sweepSetting;SweepSettingIndex = sweepSettingIndex;
        }
        public Path Path { get; set; }
        public SweepSetting SweepSetting { get; set; }
        public int SweepSettingIndex { get; set; }
    }
    internal class ReceiverCalibrationSetting : CalibrationSetting
    {
        public Path sourcePath;
        public ReceiverCalibrationSetting( Path path, SweepSetting sweepSetting, int sweepSettingIndex, Path sourcePath) :base(path,sweepSetting,sweepSettingIndex)
        {
            this.sourcePath = sourcePath;
        }
    }
    class CalbrationSettings
    {
        //此字典作为一个寿命较长的临时变量来存贮已经用过某个S2p文件名的一个SweepSettingPoint
        //便于后来新加进来的元素与之比较并决定是否创建新的S2PfileName
        internal Dictionary<ISweepSettingElement, string> DictToCompare { get; set; } = new Dictionary<ISweepSettingElement, string>();
        //主要操作的数据类型，用于存储加进来的设置与S2P文件名
        internal Dictionary<CalibrationSetting, string> calibrationSettings { get; set; } = new Dictionary<CalibrationSetting, string>();
        //添加一个元素，添加时生成S2pFileName

        public CalbrationSettings() { }
        internal void Add(CalibrationSetting calSetting) 
        {
            string S2pFileName;
            if (calSetting.Path.Type != calSetting.SweepSetting.type) return;//如果传入的路径类型和sweepsetting类型不匹配不做任何操作
            if (calSetting.SweepSetting is SimpleVectorSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as SimpleVectorSweepSetting;
            else if (calSetting.SweepSetting is SimpleSourceSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as SimpleSourceSweepSetting;
            else if (calSetting.SweepSetting is SimpleReceiverSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as SimpleReceiverSweepSetting;
            else if (calSetting.SweepSetting is OverrideVectorSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as OverrideVectorSweepSetting;
            else if (calSetting.SweepSetting is OverrideSourceSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as OverrideSourceSweepSetting;
            else if (calSetting.SweepSetting is OverrideReceiverSweepSetting) calSetting.SweepSetting = calSetting.SweepSetting as OverrideReceiverSweepSetting;
            if (DictToCompare.Count == 0)
            {
                DictToCompare.Add(calSetting.SweepSetting.GetSweepSettingElement(calSetting.SweepSettingIndex), $"{calSetting.Path.Name}.{calSetting.SweepSetting.Name}_0");
                S2pFileName = $"{calSetting.Path.Name}.{calSetting.SweepSetting.Name}_0";
            }
            else
            {
                var haha = from item in DictToCompare
                           where item.Key.IsConflict(calSetting.SweepSetting.GetSweepSettingElement(calSetting.SweepSettingIndex))
                           select item;
                if (haha.Count() == 0)
                {
                    int j = (from item in DictToCompare
                             where item.Value.StartsWith($"{calSetting.Path.Name}.{calSetting.SweepSetting.Name}")
                             select item).Count();
                    DictToCompare.Add(calSetting.SweepSetting.GetSweepSettingElement(calSetting.SweepSettingIndex), $"{calSetting.Path.Name}.{calSetting.SweepSetting.Name}_{j}");
                    S2pFileName = $"{calSetting.Path.Name}.{calSetting.SweepSetting.Name}_{j}";
                }
                else
                {
                    S2pFileName = haha.First().Value;
                }
            }
            calibrationSettings.Add(calSetting,S2pFileName);
        }
        //获取一个路径设置的S2P文件名
        internal string GetS2pFileName(CalibrationSetting calibrationSetting) 
        {
            return (from item in calibrationSettings where item.Key.Equals(calibrationSetting) select item.Value).First();
        }
    }
    class ReceiverCalibration : Calbration
    {
        //这部分只实现具体的校准的行为，设备的开关与初始化于此无关
        Path SourcePath { get; set; }//指定Path,获取指定Path的sourceoffset，其中的Type可用于验证（未写）
        public SignalAnalyzer RFsa { get; set; } = new SignalAnalyzer();
        public SignalGenerator RFsg { get; set; } = new SignalGenerator();
        public string s2pFilePath { get; set; }

        private CalibrationResult CalibrateReceiver()
        {
            //TODO:未来在WinForm重写
            Console.WriteLine("Receiver Calibration:");
            Console.WriteLine($"Please connect the RFSA to: {calibrationSettingElement.Key.Path.Name.ToString()}, and press ENTER to continue");
        tips:
            var temp = Console.ReadKey();
            char key = temp.KeyChar;
            if (key.Equals('\r'))
            {
                CalMapXml calMap = new CalMapXml();
                double frequency = calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Frequency;
                double generatePower = calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Power;
                double portPower = generatePower;
                double analyzerPower = RFsa.GetTonePower(frequency * 0.9, frequency * 1.1);
                double offSet = generatePower - analyzerPower - Math.Log10(Math.Pow(GetSourceOffset(frequency), 2)) * (-10);
                var s2pValue = Math.Sqrt(Math.Pow(10, -(offSet / 10)));
                SParameters sParameters = new SParameters
                {
                    Frequency = calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibrationSettingElement.Key.SweepSettingIndex).Frequency,
                    Real = new double[,]
                                    {
                                    { 0, s2pValue },
                                    { s2pValue, 0 },
                                    },
                    Imag = new double[,]
                                    {
                                    { 0, 0 },
                                    { 0, 0 },
                                    }
                };
                return new CalibrationResult(calibrationSettingElement.Value, sParameters);
            }
            else
                goto tips;
        }
        public override CalibrationResult PerformAction()
        {
            return CalibrateReceiver();
        }
        public ReceiverCalibration(KeyValuePair<CalibrationSetting, string> SettingElement, SignalAnalyzer SA,SignalGenerator SG,Path Sp)
        {
            SourcePath = Sp;
            RFsa = SA;
            RFsg = SG;
            calibrationSettingElement = SettingElement;
        }
        public double GetSourceOffset(double Frequency)
        {
            int i = 0;
            var snpFile = new SnPfile(s2pFilePath+".s2p");
            double[,,] readRe = new double[snpFile.FrequencyList.Count(), 2, 2];
            if (snpFile.FrequencyList.Count() != 0)
            {
                snpFile.GetSParameters(out readRe, out double[,,] im);
                foreach (var fre in snpFile.FrequencyList)
                {
                    if (fre == Frequency)
                        break;
                    else
                        i++;
                }
            }
            if (i != snpFile.FrequencyList.Count())
                return readRe[i, 1, 0];
            else return 0;
        }
    }
    class CalibrationResult
    {
        string S2pFileName { get; set; }
        SParameters Results { get; set; }
        public void Save(string filePath)
        {
            string FolderPath = filePath.Substring(0, filePath.IndexOf(".tdms"));
            DirectoryInfo directoryInfo = new DirectoryInfo(FolderPath);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            ComplexMatrix[] complexMatrix = { new ComplexMatrix() { Re = Results.Real, Im = Results.Imag } };
            double[] frequency = { Results.Frequency };
            var snpFile = new SnPfile(System.IO.Path.Combine(FolderPath, S2pFileName + ".s2p"));
            snpFile.SetData(frequency, complexMatrix);
            snpFile.SaveToFile(System.IO.Path.Combine(FolderPath, S2pFileName + ".s2p"));
        }
        public CalibrationResult(string S2pName, SParameters Results)
        {
            S2pFileName = S2pName;
            this.Results = Results;
        }
    }
    class CalibrationResults
    {
        CalbrationSettings calbrationSettings { get; set; }
        CalMapXml calMap { get; set; } = new CalMapXml();
        internal void SaveXmlFile(string FilePath)
        {
            FilePath = FilePath.Substring(0, FilePath.IndexOf(".tdms"));
            foreach (var item in calbrationSettings.calibrationSettings)
            {
                double Power;
                List<double> tempPower;
                System.Reflection.PropertyInfo[] assemblies = item.Key.SweepSetting.GetSweepSettingElement(item.Key.SweepSettingIndex).GetType().GetProperties();
                var elements = from a in assemblies
                               where a.Name == "PortPower"
                               select a.GetValue(item.Key.SweepSetting.GetSweepSettingElement(item.Key.SweepSettingIndex));
                if (elements.Count() != 0)
                {
                    tempPower = (List<double>)elements.First();
                    Power = tempPower.First();
                }
                else
                {
                    var skr = from a in assemblies
                              where a.Name == "ReferenceLevel"
                              select a.GetValue(item.Key.SweepSetting.GetSweepSettingElement(item.Key.SweepSettingIndex));
                    if (skr.Count() != 0)
                    {
                        tempPower = (List<double>)skr.First();
                        Power = tempPower.First();
                    }
                    else
                        Power = 0.001;
                }
                calMap.SetPath(item.Key.Path);
                calMap.SetSweepSettings(item.Key.SweepSetting.Name, item.Key.SweepSetting.type, item.Key.SweepSetting.GetNumberOfPoint());
                calMap.SetPathLoss(item.Key.Path, item.Key.SweepSetting, item.Key.SweepSettingIndex, item.Key.SweepSetting.GetSweepSettingElement(item.Key.SweepSettingIndex).Frequency, Power, item.Value);
            }
            calMap.Save(System.IO.Path.Combine(FilePath, "CalibrationMap.xml"));
        }
        /// <summary>
        /// 创建XML文件，并将已生成文件夹打包
        /// </summary>
        /// <param name="filePath">要打包文件全路径（.tdms）</param>
        public void Save(string filePath)
        {
            //TODO:calculate the folder path then pass to CreateFromDirectory
            string folderPath = filePath.Substring(0,filePath.IndexOf(".tdms"));
            //TODO: call calibrationSettings.SaveXMLFile
            SaveXmlFile(filePath);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            ZipFile.CreateFromDirectory(folderPath, folderPath + ".tdms");
        }
        public void DeleteFoldor(string filepath)//删除校准生成的文件夹以及读取解压时时生成的文件夹
        {
            System.IO.Directory.Delete(filepath,true);
        }
        /// <summary>
        /// 打开一个压缩包，将内容读取到内存
        /// </summary>
        /// <param name="filePath">压缩包路径（.tdms）</param>
        public void Load(string filePath)
        {
            if (filePath == null)
                return;
            string FileDirectory = filePath.Substring(0, filePath.LastIndexOf(".tdms"));
            if (Directory.Exists(FileDirectory))
                Directory.Delete(FileDirectory,true);
            ZipFile.ExtractToDirectory(filePath, filePath.Substring(0, filePath.LastIndexOf(".tdms")));
            calMap = new CalMapXml();
            calMap.Load(FileDirectory);
        }
        public double FetchSourceOffset(string path, string sweepSettingName, int sweepSettingPointIndex)
        {
            if (!calMap.JugeType(path, sweepSettingName, Calibratype.Source))
                return 0;//返回0表示输入异常，输入的path与sweepsetting的Type不一致
            var pathLoss = calMap.GetPathLoss(path, sweepSettingName, sweepSettingPointIndex);
            return calMap.GetOffset(pathLoss.S2pName, pathLoss.Frequency);
        }
        public double FetchSourceOffset(string path, double frequency, double power)
        {
            if (!calMap.JugeType(path, Calibratype.Source))
                return 0;
            var s2pFileName = calMap.GetS2pFileName(path, frequency, power);
            return calMap.GetOffset(s2pFileName, frequency);
        }
        public double FetchReceiverOffset(string path, string sweepSettingName, int sweepSettingPointIndex)
        {
            if (!calMap.JugeType(path, sweepSettingName, Calibratype.Receiver))
                return 0;//返回0表示输入异常，输入的path与sweepsetting的Type不一致
            var pathLoss = calMap.GetPathLoss(path, sweepSettingName, sweepSettingPointIndex);
            return calMap.GetOffset(pathLoss.S2pName, pathLoss.Frequency);
        }
        public double FetchReceiverOffset(string path, double frequency, double referenceLevel)
        {
            if (!calMap.JugeType(path, Calibratype.Receiver))
                return 0;

            var s2pFileName = calMap.GetS2pFileName(path, frequency, referenceLevel);
            return calMap.GetOffset(s2pFileName, frequency);
        }
        public IDictionary<string, IEnumerable<string>> GetReceiverCalInfo()//返回字典的每个entry的key是SweepSettingName，value是使用了这个sweepsetting的所有path
        {
            return GetCalInfo(Calibratype.Receiver);
        }
        public IDictionary<string, IEnumerable<string>> GetSourceCalInfo()//返回字典的每个entry的key是SweepSettingName，value是使用了这个sweepsetting的所有path
        {
            return GetCalInfo(Calibratype.Source);
        }
        internal CalibrationResults() { }
        internal CalibrationResults(CalbrationSettings settings) 
        {
            calbrationSettings = settings;
        }
        private IDictionary<string, IEnumerable<string>> GetCalInfo(Calibratype calibratype)
        {
            var allReceiverSweepSettingNames = from s in calMap.sweepSettings
                                               where s.type == calibratype
                                               select s.sweepSettingName;
            var usedPathlosses = from p in calMap.PathLosses
                                 where allReceiverSweepSettingNames.Contains(p.SSweepSetting.sweepSettingName)
                                 select p;
            return usedPathlosses.ToLookup(p => p.SSweepSetting.sweepSettingName, p => p.Path).ToDictionary(x => x.Key, x => x.ToList().Distinct());
        }
        ~CalibrationResults()
        {

        }
    }
}
