using NationalInstruments.ModularInstruments.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ProcessLogic
{
    class PowerMeter
    {
        private ni568x powerMeter { get; set; } = null;//仪器句柄，完成一个session
        internal void Open(string InstrumentText)
        {
            powerMeter = new ni568x(InstrumentText, false, false);
            //定死它测量单位是Dbm
            powerMeter.ConfigureUnits(1);
        }
        internal double Measure()
        {
            if (this.powerMeter == null)
                return double.NaN;
            else
            {
                double MeasureResult;
                powerMeter.Read(5000, out MeasureResult);
                return MeasureResult;
            }
        }
        internal void Close()
        {
            if (this.powerMeter == null)
                return;
            //丢弃
            powerMeter.Dispose();

        }
    }
}
