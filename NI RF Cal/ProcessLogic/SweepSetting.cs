using NLua;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace ProcessLogic
{
    /// <summary>
    /// 构造数据类型时元素的基类，对任意类型来说均包含frequency
    /// IsConflict作为一个抽象函数在不同的数据类型里实例化，便于在不同数据类型比较时的多态
    /// </summary>
    public interface ISweepSettingElement
    {
        double Frequency { get; set; }
        double Power { get; }
        /// <summary>
        /// 两个SweepSettingPoint进行比较，如果除了频率以外的所有参数都保持一致，认为这俩对象不冲突，否则认为冲突。用于生成S2P文件名时判断两个SweepSetting设置测量的结果是否应该放入同一个S2P文件。
        /// </summary>
        /// <param name="anotherReceiverSimplePoint">与当前对象比较的SweepSettingPoint</param>
        /// <returns>一致返回true，否则返回false</returns>
        bool IsConflict(ISweepSettingElement anotherReceiverSimplePoint);
    }
    /*
 * 这个类是Source模式下的calibrationSettings的数据结构，它因为与Vector模式下的数据结
 * 构一致，所以直接继承。
 * 对于可能会是向量类型的变量这里用列表来表示。
 */
    public class SourceCalibrationElements
    {
        public double IFBW { get; set; }
        public List<double> vectorCal_Power_powerMeter { get; set; } = new List<double>();
        public List<double> vectorCal_Power { get; set; } = new List<double>();
        public List<double> scalarCal_Power_sourceCal { get; set; } = new List<double>();
        public SourceCalibrationElements Clone()
        {
            return this.MemberwiseClone() as SourceCalibrationElements;
        }
    }
    /*
     * 这个类是Source模式下的simple里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */
    public class SourceSweepSimplePoint : ISweepSettingElement
    {
        public double Frequency { get; set; }
        public List<double> PortPower { get; set; } = new List<double>();
        public double Power { get => this.PortPower.First(); }

    public bool IsConflict(ISweepSettingElement anotherSweepPoint)
        {
            if (!(anotherSweepPoint is SourceSweepSimplePoint))
                return false;
            else
            {
                SourceSweepSimplePoint anotherSimplePoint = anotherSweepPoint as SourceSweepSimplePoint;
                return this.PortPower.First() == anotherSimplePoint.PortPower.First();
            }
        }
    }
    /*
     * 这个类是Source模式下的override里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */
    class SourceSweepOverridePoint : SourceSweepSimplePoint
    {
        public List<double> ReferenceLevel { get; set; } = new List<double>();
        public List<double> RFSARerenceLevel { get; set; } = new List<double>();
        public List<string> TXPath_5530 { get; set; } = new List<string>();
        public List<string> RXPath_5530 { get; set; } = new List<string>();
        public SourceCalibrationElements calibrationSettings { get; set; } = new SourceCalibrationElements();
        public new bool IsConflict(ISweepSettingElement anotherSweepPoint)
        {
            if (!(anotherSweepPoint is SourceSweepOverridePoint)) return false;
            else
            {
                SourceSweepOverridePoint anotherSimplePoint = anotherSweepPoint as SourceSweepOverridePoint;
                bool flag = true;
                PropertyInfo[] assemblies = this.GetType().GetProperties();
                List<PropertyInfo> templist = assemblies.ToList<PropertyInfo>();
                foreach (var item in templist)
                {
                    if (item.Name == "Frequency")
                        templist.Remove(item);
                }
                assemblies = templist.ToArray();
                foreach (var item in assemblies)
                    if (item.Name == "PortPower")
                        flag &= this.PortPower.First() == anotherSimplePoint.PortPower.First();
                    else
                        flag &= item.GetValue(this) == item.GetValue(anotherSweepPoint);
                return flag;
            }
        }
    }
    /*
    * 这个类是Vector模式下的simple里的数据结构，由若干个频率点组成。
    * 用频率点的数据类型的列表来表示。
    * 包含一个从文件中读取信息填充数据类型的 FromLuaTable 方法
    * 已测试通过
    */
    public class SimpleSourceSweepSetting : SweepSetting
    {
        public SimpleSourceSweepSetting()
        {
            this.type = Calibratype.Source;
        }
        public List<SourceSweepSimplePoint> SourceSweepSettingPoints { get; set; } = new List<SourceSweepSimplePoint>();
        public virtual void FromLuaTable(LuaTable luaTable, string name)
        {
            Name = name;
            LuaTable subSourceSettingsTable = (LuaTable)luaTable[Name + ".list"];
            SourceSweepSimplePoint lastSourceSweepSettingPoint = new SourceSweepSimplePoint();
            int i = 0;
            foreach (LuaTable point in subSourceSettingsTable.Values)
            {
                SourceSweepSimplePoint sourceSweepSettingPoint = new SourceSweepSimplePoint();
                {
                    sourceSweepSettingPoint.Frequency = Utilities.HasField(point, "freq") ? double.Parse(point["freq"].ToString()) : i > 0 ? lastSourceSweepSettingPoint.Frequency : default;
                    sourceSweepSettingPoint.PortPower = Utilities.HasField(point, "portPower") ? Utilities.MutiDoubleFromFile(point, "portPower") : i > 0 ? lastSourceSweepSettingPoint.PortPower : default;
                };
                SourceSweepSettingPoints.Add(sourceSweepSettingPoint);
                lastSourceSweepSettingPoint = sourceSweepSettingPoint;
                i++;
            }
        }
        public override int FindIndex(double frequency, double power)
        {
            return (SourceSweepSettingPoints.FindIndex(s => s.Frequency == frequency && s.PortPower.First<double>() == power));
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return SourceSweepSettingPoints[Index];
        }

        public override int GetNumberOfPoint()
        {
            return this.SourceSweepSettingPoints.Count();
        }
    }
    class OverrideSourceSweepSetting : SimpleSourceSweepSetting
    {
        public new List<SourceSweepOverridePoint> SourceSweepSettingPoints { get; set; } = new List<SourceSweepOverridePoint>();
        public override void FromLuaTable(LuaTable luaTable, string name)
        {
            Name = name;
            LuaTable subSourceSettingsTable = (LuaTable)luaTable[Name + ".list"];
            SourceSweepOverridePoint lastSourceSweepSettingPoint = new SourceSweepOverridePoint();
            int i = 0;
            foreach (LuaTable point in subSourceSettingsTable.Values)
            {
                SourceSweepOverridePoint sourceSweepSettingPoint = new SourceSweepOverridePoint();
                if (i > 0)
                {
                    //把上次的数据存起来，如果本次循环数据缺省则沿用上次数据
                    sourceSweepSettingPoint.Frequency = lastSourceSweepSettingPoint.Frequency;
                    sourceSweepSettingPoint.PortPower = lastSourceSweepSettingPoint.PortPower;
                    sourceSweepSettingPoint.ReferenceLevel = lastSourceSweepSettingPoint.ReferenceLevel;
                    sourceSweepSettingPoint.calibrationSettings = lastSourceSweepSettingPoint.calibrationSettings;
                    sourceSweepSettingPoint.RFSARerenceLevel = lastSourceSweepSettingPoint.RFSARerenceLevel;
                    sourceSweepSettingPoint.TXPath_5530 = lastSourceSweepSettingPoint.TXPath_5530;
                    sourceSweepSettingPoint.RXPath_5530 = lastSourceSweepSettingPoint.RXPath_5530;
                }
                sourceSweepSettingPoint.Frequency = Utilities.HasField(point, "freq") ? double.Parse(point["freq"].ToString()) : i > 0 ? lastSourceSweepSettingPoint.Frequency : default;
                sourceSweepSettingPoint.PortPower = Utilities.HasField(point, "portPower") ? Utilities.MutiDoubleFromFile(point, "portPower") : i > 0 ? lastSourceSweepSettingPoint.PortPower : default;
                sourceSweepSettingPoint.ReferenceLevel = Utilities.HasField(point, "referenceLevel") ? Utilities.MutiDoubleFromFile(point, "referenceLevel") : i > 0 ? lastSourceSweepSettingPoint.ReferenceLevel : default;
                sourceSweepSettingPoint.RFSARerenceLevel = Utilities.HasField(point, "RFSAReferenceLevel") ? Utilities.MutiDoubleFromFile(point, "RFSAReferenceLevel") : i > 0 ? lastSourceSweepSettingPoint.RFSARerenceLevel : default;
                sourceSweepSettingPoint.TXPath_5530 = Utilities.HasField(point, "5530_TXPath") ? Utilities.MutiStringFromFile(point, "5530_TXPath") : i > 0 ? lastSourceSweepSettingPoint.TXPath_5530 : default;
                sourceSweepSettingPoint.RXPath_5530 = Utilities.HasField(point, "5530_RXPath") ? Utilities.MutiStringFromFile(point, "5530_RXPath") : i > 0 ? lastSourceSweepSettingPoint.RXPath_5530 : default;
                sourceSweepSettingPoint.calibrationSettings = Utilities.HasField(point, "calibrationSettings") ? GetCalibrationSettings((LuaTable)point["calibrationSettings"]) : i > 0 ? lastSourceSweepSettingPoint.calibrationSettings.Clone() : default;

                SourceSweepSettingPoints.Add(sourceSweepSettingPoint);
                lastSourceSweepSettingPoint = sourceSweepSettingPoint;
                i++;
            }
        }
        private SourceCalibrationElements GetCalibrationSettings(LuaTable luaTable)
        {
            return new SourceCalibrationElements()
            {
                IFBW = Utilities.HasField(luaTable, "IFBW") ? double.Parse(luaTable["IFBW"].ToString()) : default,
                vectorCal_Power = Utilities.HasField(luaTable, "vectorCal_Power") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power") : default,
                vectorCal_Power_powerMeter = Utilities.HasField(luaTable, "vectorCal_Power_powerMeter") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power_powerMeter") : default,
                scalarCal_Power_sourceCal = Utilities.HasField(luaTable, "scalarCal_Power_sourceCal") ? Utilities.MutiDoubleFromFile(luaTable, "scalarCal_Power_sourceCal") : default,
            };
        }
        public override int GetNumberOfPoint()
        {
            return this.SourceSweepSettingPoints.Count();
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return SourceSweepSettingPoints[Index];
        }
    }
    /*
    * 这个类是Source模式下的calibrationSettings的数据结构.
    * 对于可能会是向量类型的变量这里用列表来表示。
    */
    public class ReceiverCalibrationElements
    {
        public double IFBW { get; set; }
        public List<double> vectorCal_Power_powerMeter { get; set; } = new List<double>();
        public List<double> vectorCal_Power { get; set; } = new List<double>();
        public List<double> scalarCal_Power_receiverCal { get; set; } = new List<double>();

        public ReceiverCalibrationElements Clone()
        {
            return this.MemberwiseClone() as ReceiverCalibrationElements;
        }
    }
    /*
     * 这个类是Source模式下的simple里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */

    public class ReceiverSweepSimplePoint : ISweepSettingElement
    {
        public double Frequency { get; set; }
        public List<double> ReferenceLevel { get; set; } = new List<double>();

        public double Power { get => this.ReferenceLevel.First(); }

        public bool IsConflict(ISweepSettingElement anotherPoint)
        {
            if (!(anotherPoint is ReceiverSweepSimplePoint)) return false;
            else
            {
                ReceiverSweepSimplePoint anotherSweepPoint = anotherPoint as ReceiverSweepSimplePoint;
                PropertyInfo[] assemblies = this.GetType().GetProperties();
                return this.ReferenceLevel.First() == anotherSweepPoint.ReferenceLevel.First();
            }
        }
    }
    /*
     * 这个类是Source模式下的simple里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */
    class ReceiverSweepOverridePoint : ReceiverSweepSimplePoint
    {
        public List<double> RFSARerenceLevel { get; set; } = new List<double>();
        public List<double> PortPower { get; set; } = new List<double>();
        public List<string> TXPath_5530 { get; set; } = new List<string>();
        public List<string> RXPath_5530 { get; set; } = new List<string>();
        public List<string> ComplingPath_5530 { get; set; } = new List<string>();
        public ReceiverCalibrationElements calibrationSettings { get; set; } = new ReceiverCalibrationElements();
        public new bool IsConflict(ISweepSettingElement anotherPoint)
        {
            if (!(anotherPoint is ReceiverSweepOverridePoint)) return false;
            else
            {
                ReceiverSweepOverridePoint anotherSweepPoint = anotherPoint as ReceiverSweepOverridePoint;
                bool flag = true;
                PropertyInfo[] assemblies = this.GetType().GetProperties();
                List<PropertyInfo> templist = assemblies.ToList<PropertyInfo>();
                foreach (var item in templist)
                {
                    if (item.Name == "Frequency")
                        templist.Remove(item);
                }
                assemblies = templist.ToArray();
                foreach (var item in assemblies)
                    if (item.PropertyType == typeof(List<double>))
                        flag &= ((List<double>)item.GetValue(this)).First() == ((List<double>)anotherPoint).First();
                    else if (item.PropertyType == typeof(List<string>))
                        flag &= ((List<string>)item.GetValue(this)).First() == ((List<string>)anotherPoint).First();
                    else
                        flag &= (item.GetValue(this) == item.GetValue(anotherPoint));
                return flag;
            }
        }
    }
    /*
    * 这个类是Vector模式下的simple里的数据结构，由若干个频率点组成。
    * 用频率点的数据类型的列表来表示。
    * 包含一个从文件中读取信息填充数据类型的 FromLuaTable 方法
    * 已测试通过
    */
    public class SimpleReceiverSweepSetting : SweepSetting
    {
        public List<ReceiverSweepSimplePoint> ReceiverSweepSettingPoints { get; set; } = new List<ReceiverSweepSimplePoint>();
        public SimpleReceiverSweepSetting() { this.type = Calibratype.Receiver; }
        public virtual void FromLuaTable(LuaTable luaTable, string name)
        {
            Name = name;
            var subReceiverSettingsTable = (LuaTable)luaTable[Name + ".list"];
            var lastReceiverSweepSettingPoint = new ReceiverSweepSimplePoint();
            int i = 0;
            foreach (LuaTable point in subReceiverSettingsTable.Values)
            {
                var receiverSweepSettingPoint = new ReceiverSweepSimplePoint
                {

                    Frequency = Utilities.HasField(point, "freq") ? double.Parse(point["freq"].ToString()) :
                                                            i > 0 ? lastReceiverSweepSettingPoint.Frequency :
                                                            default,
                    ReferenceLevel = Utilities.HasField(point, "referenceLevel") ? Utilities.MutiDoubleFromFile(point, "referenceLevel") :
                                                                           i > 0 ? lastReceiverSweepSettingPoint.ReferenceLevel :
                                                                           default
                };

                ReceiverSweepSettingPoints.Add(receiverSweepSettingPoint);
                lastReceiverSweepSettingPoint = receiverSweepSettingPoint;
                i++;
            }
        }
        public override int FindIndex(double frequency, double power)
        {
            return (ReceiverSweepSettingPoints.FindIndex(s => s.Frequency == frequency && s.ReferenceLevel.First<double>() == power));
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return ReceiverSweepSettingPoints[Index];
        }

        public override int GetNumberOfPoint()
        {
            return this.ReceiverSweepSettingPoints.Count();
        }
    }
    class OverrideReceiverSweepSetting : SimpleReceiverSweepSetting
    {
        public new List<ReceiverSweepOverridePoint> ReceiverSweepSettingPoints { get; set; }
        public override void FromLuaTable(LuaTable luaTable, string name)
        {
            base.FromLuaTable(luaTable, name);
            ReceiverSweepSettingPoints = (from p in base.ReceiverSweepSettingPoints
                                          select new ReceiverSweepOverridePoint
                                          {
                                              Frequency = p.Frequency,
                                              ReferenceLevel = p.ReferenceLevel,
                                          })
                                         .ToList();

            var subReceiverSettingsTable = (LuaTable)luaTable[Name + ".list"];
            var lastReceiverSweepSettingPoint = new ReceiverSweepOverridePoint();
            int i = 0;
            foreach (LuaTable point in subReceiverSettingsTable.Values)
            {
                var receiverSweepSettingPoint = ReceiverSweepSettingPoints[i];

                receiverSweepSettingPoint.RFSARerenceLevel = Utilities.HasField(point, "RFSAReferenceLevel") ? Utilities.MutiDoubleFromFile(point, "RFSAReferenceLevel") : i > 0 ? lastReceiverSweepSettingPoint.RFSARerenceLevel : default;
                receiverSweepSettingPoint.PortPower = Utilities.HasField(point, "portPower") ? Utilities.MutiDoubleFromFile(point, "portPower") : i > 0 ? lastReceiverSweepSettingPoint.PortPower : default;
                receiverSweepSettingPoint.ComplingPath_5530 = Utilities.HasField(point, "5530_CouplingPath") ? Utilities.MutiStringFromFile(point, "5530_CouplingPath") : i > 0 ? lastReceiverSweepSettingPoint.ComplingPath_5530 : default;
                receiverSweepSettingPoint.RXPath_5530 = Utilities.HasField(point, "5530_RXPath") ? Utilities.MutiStringFromFile(point, "5530_RXPath") : i > 0 ? lastReceiverSweepSettingPoint.RXPath_5530 : default;
                receiverSweepSettingPoint.TXPath_5530 = Utilities.HasField(point, "5530_TXPath") ? Utilities.MutiStringFromFile(point, "5530_TXPath") : i > 0 ? lastReceiverSweepSettingPoint.TXPath_5530 : default;
                receiverSweepSettingPoint.calibrationSettings = Utilities.HasField(point, "calibrationSettings") ? GetCalibrationSettings((LuaTable)point["calibrationSettings"]) : i > 0 ? lastReceiverSweepSettingPoint.calibrationSettings.Clone() : default;

                ReceiverSweepSettingPoints[i] = receiverSweepSettingPoint;
                lastReceiverSweepSettingPoint = receiverSweepSettingPoint;
                i++;
            }
        }
        public override int GetNumberOfPoint()
        {
            return this.ReceiverSweepSettingPoints.Count();
        }
        private ReceiverCalibrationElements GetCalibrationSettings(LuaTable luaTable)
        {
            return new ReceiverCalibrationElements()
            {
                IFBW = Utilities.HasField(luaTable, "IFBW") ? double.Parse(luaTable["IFBW"].ToString()) : default,
                vectorCal_Power = Utilities.HasField(luaTable, "vectorCal_Power") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power") : default,
                vectorCal_Power_powerMeter = Utilities.HasField(luaTable, "vectorCal_Power_powerMeter") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power_powerMeter") : default,
                scalarCal_Power_receiverCal = Utilities.HasField(luaTable, "scalarCal_Power_receiverCal") ? Utilities.MutiDoubleFromFile(luaTable, "scalarCal_Power_receiverCal") : default,
            };
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return ReceiverSweepSettingPoints[Index];
        }
    }
    public class VectorCalibrationElements
    {
        public double IFBW { get; set; }
        public List<double> vectorCal_Power_powerMeter { get; set; } = new List<double>();
        public List<double> vectorCal_Power { get; set; } = new List<double>();
        public List<double> scalarCal_Power_sourceCal { get; set; } = new List<double>();
        public virtual VectorCalibrationElements Clone()
        {
            return this.MemberwiseClone() as VectorCalibrationElements;
        }
    }
    /*
     * 这个类是Vector模式下的simple里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */

    public class VectorSweepSimplePoint : ISweepSettingElement
    {
        public double Frequency { get; set; }
        public List<double> PortPower { get; set; } = new List<double>();
        public List<double> ReferenceLevel { get; set; } = new List<double>();
        public double IFBW { get; set; }
        public double Power { get => this.PortPower.First(); }

        public bool IsConflict(ISweepSettingElement anotherPoint)
        {
            if (!(anotherPoint is VectorSweepSimplePoint)) return false;
            else
            {
                VectorSweepSimplePoint anotherSweepPoint = anotherPoint as VectorSweepSimplePoint;
                return PortPower.First() == anotherSweepPoint.PortPower.First();
            }
        }
    }
    /*
     * 这个类是Vector模式下的override里一个频率点的数据结构。
     * 对于可能会是向量类型的变量这里用列表来表示。
     */
    class VectorSweepOverridePoint : VectorSweepSimplePoint
    {
        public List<double> RFSARerenceLevel { get; set; } = new List<double>();
        public List<string> TXPath_5530 { get; set; } = new List<string>();
        public List<string> RXPath_5530 { get; set; } = new List<string>();
        public VectorCalibrationElements calibrationSettings { get; set; } = new VectorCalibrationElements();
        public new bool IsConflict(ISweepSettingElement anotherPoint)
        {
            if (!(anotherPoint is VectorSweepOverridePoint)) return false;
            else
            {
                bool flag = true;
                VectorSweepOverridePoint anotherSweepPoint = anotherPoint as VectorSweepOverridePoint;
                PropertyInfo[] assemblies = this.GetType().GetProperties();
                List<PropertyInfo> templist = assemblies.ToList<PropertyInfo>();
                foreach (var item in templist)
                {
                    if (item.Name == "Frequency")
                        templist.Remove(item);
                }
                assemblies = templist.ToArray();
                foreach (var item in assemblies)
                    if (item.PropertyType == typeof(List<double>))
                        flag &= ((List<double>)item.GetValue(this)).First() == ((List<double>)anotherPoint).First();
                    else if (item.PropertyType == typeof(List<string>))
                        flag &= ((List<string>)item.GetValue(this)).First() == ((List<string>)anotherPoint).First();
                    else
                        flag &= (item.GetValue(this) == item.GetValue(anotherPoint));
                return flag;
            }
        }
    }
    /*
     * 这个类是Vector模式下的override里的数据结构，由若干个频率点组成。
     * 用频率点的数据类型的列表来表示。
     * 包含一个从文件中读取信息填充数据类型的 FromLuaTable 方法
     */
    class OverrideVectorSweepSetting : SimpleVectorSweepSetting
    {
        public new List<VectorSweepOverridePoint> VectorSweepSettingPoints { get; set; } = new List<VectorSweepOverridePoint>();
        public override int GetNumberOfPoint()
        {
            return this.VectorSweepSettingPoints.Count();
        }
        internal override void FromLuaTable(LuaTable luaTable, string name)
        {
            Name = name;
            var subVectorSettingsTable = (LuaTable)luaTable[Name + ".list"];
            var lastVectorSweepSettingPoint = new VectorSweepOverridePoint();
            int i = 0;
            foreach (LuaTable point in subVectorSettingsTable.Values)
            {
                var vectorSweepSettingPoint = new VectorSweepOverridePoint();
                if (i > 0)
                {
                    //把上次的数据存起来，如果本次循环数据缺省则沿用上次数据
                    vectorSweepSettingPoint.Frequency = lastVectorSweepSettingPoint.Frequency;
                    vectorSweepSettingPoint.PortPower = lastVectorSweepSettingPoint.PortPower;
                    vectorSweepSettingPoint.IFBW = lastVectorSweepSettingPoint.IFBW;
                    vectorSweepSettingPoint.ReferenceLevel = lastVectorSweepSettingPoint.ReferenceLevel;
                    vectorSweepSettingPoint.calibrationSettings = lastVectorSweepSettingPoint.calibrationSettings;
                    vectorSweepSettingPoint.RFSARerenceLevel = lastVectorSweepSettingPoint.RFSARerenceLevel;
                    vectorSweepSettingPoint.TXPath_5530 = lastVectorSweepSettingPoint.TXPath_5530;
                    vectorSweepSettingPoint.RXPath_5530 = lastVectorSweepSettingPoint.RXPath_5530;
                }
                vectorSweepSettingPoint.Frequency = Utilities.HasField(point, "freq") ? double.Parse(point["freq"].ToString()) : i > 0 ? lastVectorSweepSettingPoint.Frequency : default;
                vectorSweepSettingPoint.IFBW = Utilities.HasField(point, "IFBW") ? double.Parse(point["IFBW"].ToString()) : i > 0 ? lastVectorSweepSettingPoint.IFBW : default;
                vectorSweepSettingPoint.PortPower = Utilities.HasField(point, "portPower") ? Utilities.MutiDoubleFromFile(point, "portPower") : i > 0 ? lastVectorSweepSettingPoint.PortPower : default;
                vectorSweepSettingPoint.ReferenceLevel = Utilities.HasField(point, "referenceLevel") ? Utilities.MutiDoubleFromFile(point, "referenceLevel") : i > 0 ? lastVectorSweepSettingPoint.ReferenceLevel : default;
                vectorSweepSettingPoint.RFSARerenceLevel = Utilities.HasField(point, "RFSAReferenceLevel") ? Utilities.MutiDoubleFromFile(point, "RFSAReferenceLevel") : i > 0 ? lastVectorSweepSettingPoint.RFSARerenceLevel : default;
                vectorSweepSettingPoint.TXPath_5530 = Utilities.HasField(point, "5530_TXPath") ? Utilities.MutiStringFromFile(point, "5530_TXPath") : i > 0 ? lastVectorSweepSettingPoint.TXPath_5530 : default;
                vectorSweepSettingPoint.RXPath_5530 = Utilities.HasField(point, "5530_RXPath") ? Utilities.MutiStringFromFile(point, "5530_RXPath") : i > 0 ? lastVectorSweepSettingPoint.RXPath_5530 : default;
                vectorSweepSettingPoint.calibrationSettings = Utilities.HasField(point, "calibrationSettings") ? GetCalibrationSettings((LuaTable)point["calibrationSettings"]) : i > 0 ? lastVectorSweepSettingPoint.calibrationSettings.Clone() : default;

                VectorSweepSettingPoints.Add(vectorSweepSettingPoint);
                lastVectorSweepSettingPoint = vectorSweepSettingPoint;
                i++;
            }
        }
        private VectorCalibrationElements GetCalibrationSettings(LuaTable luaTable)
        {
            return new VectorCalibrationElements()
            {
                IFBW = Utilities.HasField(luaTable, "IFBW") ? double.Parse(luaTable["IFBW"].ToString()) : default,
                vectorCal_Power = Utilities.HasField(luaTable, "vectorCal_Power") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power") : default,
                vectorCal_Power_powerMeter = Utilities.HasField(luaTable, "vectorCal_Power_powerMeter") ? Utilities.MutiDoubleFromFile(luaTable, "vectorCal_Power_powerMeter") : default,
                scalarCal_Power_sourceCal = Utilities.HasField(luaTable, "scalarCal_Power_sourceCal") ? Utilities.MutiDoubleFromFile(luaTable, "scalarCal_Power_sourceCal") : default,
            };
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return VectorSweepSettingPoints[Index];
        }
    }
    /*
     * 这个类是Vector模式下的simple里的数据结构，由若干个频率点组成。
     * 用频率点的数据类型的列表来表示。
     * 包含一个从文件中读取信息填充数据类型的 FromLuaTable 方法
     * 已测试通过
     */

    public class SimpleVectorSweepSetting : SweepSetting
    {
        public List<VectorSweepSimplePoint> VectorSweepSettingPoints { get; set; } = new List<VectorSweepSimplePoint>();
        public SimpleVectorSweepSetting() { this.type = Calibratype.Vector; }
        internal virtual void FromLuaTable(LuaTable luaTable, string name)
        {
            Name = name;
            var subVectorSettingsTable = (LuaTable)luaTable[Name + ".list"];
            var lastVectorSweepSettingPoint = new VectorSweepSimplePoint();
            int i = 0;
            foreach (LuaTable point in subVectorSettingsTable.Values)
            {
                var vectorSweepSettingPoint = new VectorSweepSimplePoint();
                if (i > 0)
                {
                    vectorSweepSettingPoint.Frequency = lastVectorSweepSettingPoint.Frequency;
                    vectorSweepSettingPoint.PortPower = lastVectorSweepSettingPoint.PortPower;
                    vectorSweepSettingPoint.IFBW = lastVectorSweepSettingPoint.IFBW;
                    vectorSweepSettingPoint.ReferenceLevel = lastVectorSweepSettingPoint.ReferenceLevel;
                }
                foreach (var item in point.Keys)
                {
                    if (item.ToString() == "portPower")
                        vectorSweepSettingPoint.PortPower = Utilities.MutiDoubleFromFile(point, item.ToString());
                    if (item.ToString() == "freq")
                        vectorSweepSettingPoint.Frequency = double.Parse(point[item].ToString());
                    if (item.ToString() == "referenceLevel")
                        vectorSweepSettingPoint.ReferenceLevel = Utilities.MutiDoubleFromFile(point, item.ToString());
                    if (item.ToString() == "IFBW")
                        vectorSweepSettingPoint.IFBW = double.Parse(point[item].ToString());
                }
                VectorSweepSettingPoints.Add(vectorSweepSettingPoint);
                lastVectorSweepSettingPoint = vectorSweepSettingPoint;
                i++;
            }
        }
        public override int FindIndex(double frequency, double power)
        {
            return (VectorSweepSettingPoints.FindIndex(s => s.Frequency == frequency && s.PortPower.First<double>() == power));
        }
        public override ISweepSettingElement GetSweepSettingElement(int Index)
        {
            return VectorSweepSettingPoints[Index];
        }

        public override int GetNumberOfPoint()
        {
            return this.VectorSweepSettingPoints.Count();
        }
    }
    public abstract class SweepSetting
    {
        public string Name;
        public Calibratype type;
        public abstract int FindIndex(double frequency, double power);
        public abstract int GetNumberOfPoint();
        public abstract ISweepSettingElement GetSweepSettingElement(int Index);
    }
    public class SweepSettings
    {
        string Path { get; }
        List<SimpleVectorSweepSetting> vectorSweepSetting { get; set; } = new List<SimpleVectorSweepSetting>();
        List<SimpleSourceSweepSetting> sourceSweepSetting { get; set; } = new List<SimpleSourceSweepSetting>();
        List<SimpleReceiverSweepSetting> receiverSweepSetting { get; set; } = new List<SimpleReceiverSweepSetting>();
        public SimpleSourceSweepSetting GetSourceSweepSetting(string Name)
        {
            return (from item in sourceSweepSetting
                    where item.Name == Name
                    select item).First();
        }
        public SimpleReceiverSweepSetting GetReceiverSweepSetting(string Name)
        {
            return (from item in receiverSweepSetting
                    where item.Name == Name
                    select item).First();
        }
        public SimpleVectorSweepSetting GetVectorSweepSetting(string Name)
        {
            return (from item in vectorSweepSetting
                    where item.Name == Name
                    select item).First();
        }
        public void Load(string FilePath)
        {
            Lua lua = new Lua();
            lua.DoFile(FilePath);
            var vectorSettingsTable = (LuaTable)lua["VectorSweepSettings"];
            var sourceSettingsTable = (LuaTable)lua["SourceCalibrationSettings"];
            var receiverSettingsTable = (LuaTable)lua["ReceiverCalibrationSettings"];
            foreach (var item in vectorSettingsTable.Keys)
            {
                var subVectorSettingsTable = (LuaTable)lua["VectorSweepSettings." + item + ".list"];
                var point = (LuaTable)(subVectorSettingsTable)[1];

                if (point.Values.Count == 4)//假设某种条件识别simple
                {
                    var simpleVectorSweepSetting = new SimpleVectorSweepSetting();//以免出现之前添加的都变成一样
                    simpleVectorSweepSetting.FromLuaTable(vectorSettingsTable, item.ToString());
                    vectorSweepSetting.Add(simpleVectorSweepSetting);
                }
                if (point.Values.Count > 4)//假设某种条件识别override
                {
                    var overrideVectorSweepSetting = new OverrideVectorSweepSetting();
                    overrideVectorSweepSetting.FromLuaTable(vectorSettingsTable, item.ToString());
                    vectorSweepSetting.Add(overrideVectorSweepSetting);
                }
            }
            foreach (var item in sourceSettingsTable.Keys)
            {
                var subSourceSettingsTable = (LuaTable)lua["SourceCalibrationSettings." + item + ".list"];
                var point = (LuaTable)(subSourceSettingsTable[1]);

                if (point.Values.Count == 2)
                {
                    var simpleSourceSweepSetting = new SimpleSourceSweepSetting();
                    simpleSourceSweepSetting.FromLuaTable(sourceSettingsTable, item.ToString());
                    sourceSweepSetting.Add(simpleSourceSweepSetting);
                }
                if (point.Values.Count > 2)
                {
                    var overrideSourceSweepSetting = new OverrideSourceSweepSetting();
                    overrideSourceSweepSetting.FromLuaTable(sourceSettingsTable, item.ToString());
                    sourceSweepSetting.Add(overrideSourceSweepSetting);
                }
            }
            foreach (var item in receiverSettingsTable.Keys)
            {
                var subReceiverSettingsTable = (LuaTable)lua["ReceiverCalibrationSettings." + item + ".list"];
                var point = (LuaTable)(subReceiverSettingsTable[1]);
                if (point.Values.Count == 2)
                {
                    var simpleReceiverSweepSetting = new SimpleReceiverSweepSetting();
                    simpleReceiverSweepSetting.FromLuaTable(receiverSettingsTable, item.ToString());
                    receiverSweepSetting.Add(simpleReceiverSweepSetting);
                }
                if (point.Values.Count > 2)
                {
                    var overrideReceiverSweepSetting = new OverrideReceiverSweepSetting();
                    overrideReceiverSweepSetting.FromLuaTable(receiverSettingsTable, item.ToString());
                    receiverSweepSetting.Add(overrideReceiverSweepSetting);
                }
            }
        }
    }
    class Utilities
    {
        //从Lua File中抓取浮点型数组变量
        public static List<double> MutiDoubleFromFile(LuaTable luaTable, string tableskr)
        {
            List<double> tempList = new List<double>();
            if (luaTable[tableskr].GetType() != typeof(LuaTable))
                tempList.Add(double.Parse(luaTable[tableskr].ToString()));
            else if (((LuaTable)luaTable[tableskr]).Values.Count > 1)
            {
                foreach (var item in ((LuaTable)luaTable[tableskr]).Values)
                    tempList.Add(double.Parse(item.ToString()));
            }
            return tempList;
        }
        //从Lua File中抓取字符串型数组变量
        public static List<string> MutiStringFromFile(LuaTable luaTable, string tableskr)
        {
            List<string> tempList = new List<string>();
            if (luaTable[tableskr].GetType() != typeof(LuaTable))
                tempList.Add(luaTable[tableskr].ToString());
            else if (((LuaTable)luaTable[tableskr]).Values.Count > 1)
            {
                foreach (var item in ((LuaTable)luaTable[tableskr]).Values)
                    tempList.Add(item.ToString());
            }
            return tempList;
        }
        public static bool HasField(LuaTable luaTable, string fieldName)
        {
            return luaTable.Keys.OfType<string>().Contains(fieldName);
        }
    }
}
