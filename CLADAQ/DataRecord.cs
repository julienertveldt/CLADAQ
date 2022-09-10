using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLADAQ
{
    public class DataRecord
    {
        public int Id { get; set; }
        public string DataDate { get; set; }
        public string DataTime { get; set; }
        public string PrintDate { get; set; }
        public string PrintTime { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double PosB { get; set; }
        public double PosC { get; set; }
        public double VelCmd { get; set; }
        public double LaserPcmd { get; set; }
        public double LaserPfdbck { get; set; }
        public double FlowWatch { get; set; }
    }
}
