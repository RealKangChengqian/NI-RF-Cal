using NationalInstruments.ModularInstruments.NIRfsa;
using System;
using System.Linq;
using NationalInstruments;


namespace ProcessLogic
{
    public class SignalAnalyzer
    {
        NIRfsa rfsaSession;
        public double ReferenceLevel { get; set; }//开放性成员，可被设置

        public void Open(string resourceName)//启动SA，并且配置好CLock采用默认的OnboardClock,开放Referencelevel（rf）给使用者
        {
            Close();
            rfsaSession = new NIRfsa(resourceName, true, false);
        }
        public double GetTonePower(double startfrequency, double stopfrequency)//设置频率的上下限
        {
            rfsaSession.Configuration.Vertical.ReferenceLevel = ReferenceLevel;
            rfsaSession.Configuration.AcquisitionType = RfsaAcquisitionType.Spectrum;
            rfsaSession.Configuration.Spectrum.ConfigureSpectrumFrequencyStartStop(startfrequency, stopfrequency);//在频率的上下限内，配置频谱
            return SearchPeak();
        }
        public void Close()//关闭会话
        {
            if (rfsaSession != null)
            {
                rfsaSession.Close();
                rfsaSession = null;
            }
        }
        private double SearchPeak()//通过拿到频谱在其中拿到最大值
        {
            RfsaSpectrumInfo spectrumInfo;
            double[] data;
            PrecisionTimeSpan timespan = new PrecisionTimeSpan(10.0);//设置时间间隔;
            data = rfsaSession.Acquisition.Spectrum.ReadPowerSpectrum(timespan, out spectrumInfo);//读出
            return data.Max();
        }
    }
}
