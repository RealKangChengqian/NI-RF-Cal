using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessLogic
{
    class Program
    {
        static void Main(string[] args)
        {

            /// <summary>
            ///  Version 1.0：
            /// Completed the test of the CalibrationResult module words.
            /// The code associated with the calibration function is replaced by a constant.
            ///  Version 2.0：
            ///  Completed the coding and combining of the hardware-related classes
            ///  Version 3.0：
            ///  using above-mentioned classes complete the coding of Calibration module
            ///  Version3.1 To Do:
            ///  To Complete the coding of the VectorCalibration module words and combine it to be called by the Calibration module
            ///  Version4.0 To Do:
            ///  To Complete the top level calling 
            ///  To Complete the Windows form of the software using event or the WinForm APIs
            /// </summary>

            /*
             * 模拟人为设置每个路径的校准参数
             */
            SweepSettings sweepSettings = new SweepSettings();
            sweepSettings.Load("example.lua");
            var receiverSweepSetting = sweepSettings.GetReceiverSweepSetting("sweep_receiver_overridePowerAsArray");
            var sourceSweepSetting = sweepSettings.GetSourceSweepSetting("sweep_source_simplePowerAsArray");

            Path receiverPath = new Path() { Name = "path1", Type = Calibratype.Receiver };
            Path sourcePath = new Path() { Name = "path2", Type = Calibratype.Source };
            CalibrationSetting sourceCalibrationSetting1 = new CalibrationSetting(sourcePath, sourceSweepSetting, 2);
            CalibrationSetting sourceCalibrationSetting2 = new CalibrationSetting(sourcePath, sourceSweepSetting, 0);
            ReceiverCalibrationSetting receiverCalibrationSetting3 = new ReceiverCalibrationSetting(receiverPath, receiverSweepSetting, 0, sourcePath);
            CalbrationSettings calbrationSettings = new CalbrationSettings();
            calbrationSettings.Add(sourceCalibrationSetting1);
            calbrationSettings.Add(sourceCalibrationSetting2);
            calbrationSettings.Add(receiverCalibrationSetting3);
            /*
             * 模拟人为设置  文件夹路径
             */
            //todo: replace folderPath witch tdms filePath
            string filePath = @"C:\Users\NI xi'an\Desktop\test.tdms";
            if (Directory.Exists(filePath.Substring(0, filePath.IndexOf(".tdms"))))
                Directory.Delete(filePath.Substring(0, filePath.IndexOf(".tdms")),true);
            /*
             * 模拟上层调用
             */
            ////根据设置参数生成XML文件
            //calbrationSettings.SaveXmlFile(filePath);
            //循环进行校准，每完成一次保存一次结果（按默认顺序来）
            //todo: calbrationSettings to support foreach
            Console.WriteLine("Opening instruments, please wait...");
            SignalGenerator signalGenerator = new SignalGenerator();
            signalGenerator.Open("VST_5646R_C1_S07");
            PowerMeter powerMeter = new PowerMeter();
            powerMeter.Open("COM3");
            SignalAnalyzer signalAnalyzer = new SignalAnalyzer();
            signalAnalyzer.Open("VST_5646R_C1_S07");
            foreach (var item in calbrationSettings.calibrationSettings)
            {
                if (item.Key.SweepSetting.type == Calibratype.Source)
                {
                    SourceCalibration calbration = new SourceCalibration(item,signalGenerator,powerMeter);
                    calbration.PerformAction().Save(filePath);
                }
                else if(item.Key.SweepSetting.type==Calibratype.Receiver)
                {
                    var keyValue = item.Key as ReceiverCalibrationSetting;
                    signalAnalyzer.ReferenceLevel = keyValue.SweepSetting.GetSweepSettingElement(keyValue.SweepSettingIndex).Power;
                    ReceiverCalibration calibration = new ReceiverCalibration(item, signalAnalyzer, signalGenerator, receiverPath);
                    double frequency = calibration.calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibration.calibrationSettingElement.Key.SweepSettingIndex).Frequency;
                    double portPower = calibration.calibrationSettingElement.Key.SweepSetting.GetSweepSettingElement(calibration.calibrationSettingElement.Key.SweepSettingIndex).Power;
                    var sourceS2ppath = (from itemm in calbrationSettings.calibrationSettings
                                where itemm.Key.Path == keyValue.sourcePath &&
                                itemm.Key.SweepSetting.GetSweepSettingElement(itemm.Key.SweepSettingIndex).Frequency == frequency &&
                                itemm.Key.SweepSetting.GetSweepSettingElement(itemm.Key.SweepSettingIndex).Power == portPower
                                         select itemm.Value).First();
                    calibration.s2pFilePath = System.IO.Path.Combine(filePath.Substring(0, filePath.IndexOf(".tdms")),sourceS2ppath);
                    calibration.PerformAction().Save(filePath);
                }
                //进行一次校准并把结果保存到指定的S2P文件中

            }

            CalibrationResults results = new CalibrationResults(calbrationSettings);
            results.Save(filePath);
            results.DeleteFoldor(filePath.Substring(0, filePath.IndexOf(".tdms")));
            CalibrationResults calibrationresult = new CalibrationResults();
            calibrationresult.Load(filePath);
            Console.WriteLine("Get Source Calibration information:");
            var dict = calibrationresult.GetSourceCalInfo();
            foreach (var entry in dict)
            {
                Console.Write($"{entry.Key}: " + "{");
                foreach (var path in entry.Value)
                    Console.Write($"{path}, ");
                Console.WriteLine("}");
            }
            Console.WriteLine("Get Receiver Calibration information:");
            dict = calibrationresult.GetReceiverCalInfo();
            foreach (var entry in dict)
            {
                Console.Write($"{entry.Key}: " + "{");
                foreach (var path in entry.Value)
                    Console.Write($"{path}, ");
                Console.WriteLine("}");
            }
            Console.WriteLine($"Offset of {sourcePath.Name} at 1020000000Hz and 0dBm: {calibrationresult.FetchSourceOffset("path2", 1020000000, 0)}");
            Console.WriteLine($"Offset of {receiverPath.Name} at sweep_receiver_overridePowerAsArray[0]: {calibrationresult.FetchReceiverOffset("path1", "sweep_receiver_overridePowerAsArray", 0)}");
            Console.ReadKey();
        }
    }
}
