# Analysis of calibration tool

## prerequisite
### Hardware
* NI Power Meter (for scalar calibration)
* SOLT kit (for vector calibration)
* NI VST PXIe-5646R

### Software
* NI-RFSA
* NI-RFSG
* NI-568X

## User Scenario
### Top use case
```puml
left to right direction
actor "test engineer" as TE
rectangle "RF Calibration Tool.exe"{
    usecase  "Do calibration" as UC1
}
rectangle "RfCalResult.API.dll"{
    usecase "Apply calibration result" as UC2
}

TE --> UC1
TE --> UC2
```
### Use case of RF Calibration Tool
```puml
left to right direction
actor "test engineer" as TE
rectangle "Do calibration" {
    usecase "Define Sweep File" as UC1
    usecase "Do Vector Calibration" as UC2
    usecase "Do Scalar calibration" as UC3
}
TE --> UC1
TE --> UC2
TE --> UC3
```
### Use case of RfCalResul.API.dll

## Logical view
```puml
enum CalType
{
    Source,
    Receiver,
    Vector
}
class PowerMeter
class SignalGenerator
class SignalAnalyzer
class Switch
class VectorNetworkAnalyzer
class SweepSettings
{
    -Dictionary<string, SourceSweepSetting> SourceSweepSettings
    -Dictionary<string, ReceiverSweepSetting> ReceiverSeepSettings
    -Dictionary<string, VectorSweepSetting> VectorSweepSettings
    +readonly string Path
    --
    +void Load(string path)
    +void Save(string path)
    +SourceSweepSetting GetSourceSweepSetting(string name)
    +ReceiverSweepSetting GetReceiverSweepSetting(string name)
    +VectorSweepSetting GetReceiverSweepSetting(string name)
}
class SweepSetting
{
    +string Name
    --
    +int FindIndex(double frequency, double power)//根据frequency和power查找SweepPoint在列表中的位置
}
class SourceSweepSetting
class ReceiverSweepSetting
class VectorSweepSetting
class CalibrationResults
{
    -CalibrationSettings calibrationSettings
    --
    +void Load(string path)
    +void Save(string path)
    +void Add(CalibrationResult calibrationResult)
    +IDictionary<string, IEnumerable<string>> GetSourceCalInfo()//返回字典的每个entry的key是SweepSettingName，value是使用了这个sweepsetting的所有path
    +double FetchSourceOffset(string path, string sweepSettingName, int sweepSettingPointIndex)
    +double FetchSourceOffset(string path, double frequency, double power) 
    +double FetchReceiverOffset(string path, string sweepSettingName, int sweepSettingIndex)
    +double FetchReceiverOffset(string path, double frequency, double referenceLevel)
    +IDictionary<string, IEnumerable<string>> GetReceiverCalInfo()//返回字典的每个entry的key是SweepSettingName，value是使用了这个sweepsetting的所有path
    --
    +double[] FetchOffset（string SweepSettingName, double freq,string pathName）
    +double[] FetchOffset（string SweepSettingName, double freq,string pinName)
    +string[] Paths2Pins(string[]paths)
    +string[] Pinmap2Pins(string[]paths)
    +string[] Pinmap2Paths(string[]paths)
}
class CalibrationResult
{
    + string S2pFileName
    + SParameters Result
    --
    +Save(string folderPath)
}
class SnpFile
class SoltKit
class Embedding
{
    +string snpFilePath
}
class CalibrationSettings
{
    -Dictionary<CalibrationSetting, string> calibrationSettings
    --
    +void AddCalibrationSetting(CalibrationSetting calibrationSetting)
    string GetS2pFileName(CalibrationSetting calibrationSetting)
}

class CalibrationSetting
{
    + Path Path
    + SweepSetting SweepSetting
    + int SweepSettingIndex
}

class abstract Calibration 
{
    // a process
    +CalibrationSetting CalibrationSetting
    +string S2pFileName
    --
    +CalibrationResult PerformAction()
}
class SourceCalibration 
{
    // a process
    +SignalGenerator SignalGenerator
    +PowerMeter PowerMeter
    --
    +CalibrationResult PerformAction()
}
class ReceiverCalibration 
{
    // a process
    +SignalGenerator SignalGenerator
    +SignalAnalyzer SignalAnalyzer
    --
    +CalibrationResult PerformAction()
}
class SourceCalibration
{
    +PowerMeter powerMeter
    +SignalGenerator signalGenerator
}
class ReceiverCalibration
{
    +Path toneSourcePath
    +SignalAnalyzer signalAnalyzer
}
class VectorCalibration
{
    +SoltKit soltKit
}
class SParameters
class Pinmap
class Path
{
    +string Name
    +CalType type
    +string sourceEndId
    +string sinkEndId
    +Embedding embedding
    +Embedding de-embedding
}
class Calibrator
{
    +PowerMeter[] powerMeters
    +SignalAnalyzer[] signalAnalyzers
    +SignalGenerator[] signalGenerators
    +VectorNetworkAnalyzer[] vectorNetworkAnalyzers
    +Path[] paths
    +Pinmap pinmap
    +SweepSettings sweepSettings
    +CalibrationSettings CalibrationSettings
    
    +Initialize()

    +void PerformAction()



}

Calibration <|-- SourceCalibration
Calibration <|-- ReceiverCalibration
Calibration <|-- VectorCalibration
Calibrator *-- Calibration
Calibrator *-- Pinmap
Calibrator *-- Path
Calibrator *-- SweepSettings
SweepSetting <|-- SourceSweepSetting
SweepSetting <|-- ReceiverSweepSetting
SweepSetting <|-- VectorSweepSetting
SweepSettings o-- SweepSetting
Calibration --> SweepSetting
Calibration --> Path
Calibration --> CalibrationResult
SourceCalibration o-- PowerMeter
SourceCalibration o-- SignalGenerator
ReceiverCalibration o-- SignalAnalyzer
ReceiverCalibration o-- ToneSource
VectorCalibration o-- SoltKit
VectorCalibration o-- VectorNetworkAnalyzer
Path o-- Embedding
CalibrationResults --> SweepSettings
```
## Development View
![avatar](assets/Development_View.png)