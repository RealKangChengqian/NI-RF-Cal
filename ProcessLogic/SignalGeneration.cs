using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using System;
using System.Collections.Generic;
using NationalInstruments.ModularInstruments.NIRfsg;
using System.Text;
using System.Threading;

namespace ProcessLogic

{
    public class SignalGenerator
    {
        NIRfsg _rfsgSession;

        public void Open(string resourceName)
        {
            _rfsgSession = new NIRfsg(resourceName, true, false);
        }

        public void Close()
        {
            _rfsgSession.Close();
        }
        public void Generate(double frequency, double power)
        {
            if (_rfsgSession.CheckGenerationStatus() == RfsgGenerationStatus.InProgress)
                _rfsgSession.Abort();
            _rfsgSession.RF.Configure(frequency, power);
            _rfsgSession.Initiate();
        }

        public void StopGeneration()
        {
            if (_rfsgSession != null)
                _rfsgSession.RF.OutputEnabled = false;
        }

    }
}
