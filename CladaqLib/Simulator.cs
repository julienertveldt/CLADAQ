using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;




namespace CladaqLib
{
    public class Simulator
    {
        private double[] _Values;

        public int intBuffS { get; set; }
        public bool bRunning { get; private set; }
        public double dblSimDelay { get; set; }

        public double[] Values
        {
            get => _Values;
        
            set
            {
                if (Values.GetLength(2) != intNCh)
                {
                    intNCh = Values.GetLength(2);
                }
                _Values = value;    
            }
        }

        private DAQBuffer daqBuff;
        
        private UInt64[] intTimeBuff;
        private double[,] dblBuff;

        private System.Timers.Timer timer;
        private int intNCh = 6;




        public Simulator(DAQBuffer DaqBuffIn)
        {
        //    Thread trd1 = new Thread(StartSimulator);        // Simulataion
        //    trd1.Priority = ThreadPriority.BelowNormal;
        //    trd1.Start();

            if (dblSimDelay == 0)
            {    dblSimDelay = 10;
            }

            intTimeBuff = new UInt64[DaqBuffIn.intBuffS];
           

            timer = new System.Timers.Timer();
            timer.Interval = dblSimDelay;
            timer.AutoReset = true;
            timer.Enabled = false;
            timer.Elapsed += OnTimedEvent;

            daqBuff = DaqBuffIn;

            dblBuff = new double[intNCh, DaqBuffIn.intBuffS];

    }

        public void StartSimulator()
        {

            timer.Enabled = true;
            bRunning = true;

        }

        public void StopSimulator()
        {
            timer.Enabled =false;
            bRunning = false;
        }

        public void Close()
        {
            timer.Dispose();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            DateTime now = new DateTime();
            double nowS = new double();

            intBuffS = DAQBuffer.intBuffs;

            if (Values != null)
            // if not null check size and set size
            {
                if (intBuffS != Values.GetLength(1))
                {
                    intBuffS = Values.GetLength(1);

                }
            }

            double[,] dblPosBuff = new double[intBuffS, intNCh];

            

            Random rnd = new Random();
            for (int i = 0; i < intBuffS; i++)
            {
                DateTime nowT = DateTime.Now;
                nowS = nowT.Hour * 3600 + nowT.Minute * 60 + nowT.Second + now.Millisecond / 1000; //add 2 ms per sample to have 500Hz rate.

                intTimeBuff[i] = Convert.ToUInt64(nowS * 1000);

                for (int ii = 0; ii < intNCh; ii++)
                {
                    double dblDummy = 5 * Math.Sin(0.1 * (nowS) * 6.28) + rnd.Next(1);
                    dblPosBuff[i, ii] = dblDummy; // nowT.Second + now.Millisecond / 1000; // ii; // dblDummy;
                    //dblPosBuff[i, ii] = (nowS + i + ii)/166 - 0.5;
                }
                dblBuff[0, i] = dblPosBuff[i, 0];
                dblBuff[1, i] = dblPosBuff[i, 1];
                dblBuff[2, i] = dblPosBuff[i, 2];
                dblBuff[3, i] = 0; // dblPosBuff[i, 3];
                dblBuff[4, i] = 0; //dblPosBuff[i, 4];
                dblBuff[5, i] = 0; //dblPosBuff[i, 5];

                
            }

            lock (dblBuff)
            {
                lock (intTimeBuff)
                {
                    if (daqBuff != null)
                    {
                        daqBuff.AppendToBuffer(dblBuff, intTimeBuff);
                        //intTimeBuff = null;
                        //dblBuff = null;
                    }
                }
            }
        }

    }
}
