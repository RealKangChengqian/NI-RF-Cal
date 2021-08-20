namespace ProcessLogic
{
    //对每条通路校准时需要确定其校准类型，对应地，为其配置校准时（using SweepSetting）也需要一个校准类型，这里用枚举来定义。
    public enum Calibratype
    {
        Source,
        Receiver,
        Vector
    }
    //定义一条通路的数据类型，包含名称和校准类型
    public class Path
    {
        public string Name { get; set; }
        public Calibratype Type { get; set; }
    }
    //定义一种数据类型用来表示校准得到的offset数据
    //包含一个校准频率，用来索引
    //和两个2×2的矩阵分别表示二端口网络参数的实部和虚部
    public class SParameters
    {
        public double Frequency { get; set; }
        public double[,] Real { get; set; } = new double[2, 2];
        public double[,] Imag { get; set; } = new double[2, 2];
    }
}
