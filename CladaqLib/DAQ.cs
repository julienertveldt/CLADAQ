using commlpiLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CladaqLib
{
    public class DAQ
    {
        static private MlpiConnection PLC_Con = new MlpiConnection();

        static protected double[,] dblAcqCh;
        protected double[] dblCh1;                              // Pos X
        protected double[] dblCh2;                              // Pos Y
        protected double[] dblCh3;                              // Pos Z
        protected double[] dblCh4;                              // Vel cmd
        protected double[] dblCh5;                              // Laser Cmd
        protected double[] dblCh6;                              // Laser Fdbck
        protected double[] dblCh7;                              // Medicoat FlowWatch
        static protected UInt64[] uiTime;

        static private int intBuffS;
        static private uint intNCh;
        private uint intAcqDelay;


        // define buffers comming from MLPI
        protected static double[,] dblPosBuff;            // CNC positions + laser channel
        public static string[] strTimeBuff;                        // CNC timestamp
        protected static UInt64[] intTimeBuff;
        protected static UInt64[] intTimeBuffOld;                     //previous cnc timestamp
        public static string[] strDateBuff;

        static protected DAQBuffer daqBuff;

        public double dblAcqDelay { get; set; }
        public bool bRunning { get; set; }

        static Thread thread;

        public DAQ(DAQBuffer daqBuffIn, int intBuffSIn, uint intNChIn, uint intAcqDelayIn, string strTypeIn)
        {
            if (thread == null) //ToDo singleton class for single threading for both eth protocols: mlpi & MTX
            {
                thread = new Thread(new ThreadStart(ThreadMain));
                thread.Priority = ThreadPriority.AboveNormal;

                intBuffS = intBuffSIn;
                intNCh = intNChIn;
                intAcqDelay = intAcqDelayIn;

                dblAcqCh = new double[intNCh, intBuffS];
                dblCh1 = new double[intBuffS];                             // Pos X
                dblCh2 = new double[intBuffS];                             // Pos Y
                dblCh3 = new double[intBuffS];                             // Pos Z
                dblCh4 = new double[intBuffS];                             // Vel cmd
                dblCh5 = new double[intBuffS];                             // Laser Cmd
                dblCh6 = new double[intBuffS];                             // Laser Fdbck
                dblCh7 = new double[intBuffS];                             // Medicoat FlowWatch
                uiTime = new UInt64[intBuffS];

                // define buffers comming from MLPI
                dblPosBuff = new double[intBuffS, intNCh];            // CNC positions + laser channel
                strTimeBuff = new string[intBuffS];                        // CNC timestamp
                intTimeBuff = new UInt64[intBuffS];
                intTimeBuffOld = new UInt64[intBuffS];                     //previous cnc timestamp
                strDateBuff = new string[intBuffS];

                daqBuff = daqBuffIn;               
            }
        }

        private static void ThreadMain() 
        {
            List<double[]> dblAcqCh;

            while (true)
            {
                if (PLC_Con.IsConnected) //&& blPLCRunning
                {
                    uiTime = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.tTimeBufSent_gb");//tTimeBuf1_gb

                    if (uiTime.SequenceEqual(intTimeBuffOld) == false)
                    {
                        dblAcqCh = new List<double[]>();

                        for (int ii = 1; ii < (intNCh + 1); ii += 1) // read channel by channel
                        {
                            string dummyPath = "Application.PlcVarGlobal.reBuf" + ii.ToString() + "_Sent_gb"; 
                            dblAcqCh.Add(PLC_Con.Logic.ReadVariableBySymbol(dummyPath));
                        }

                        daqBuff.AppendToBuffer(dblAcqCh, uiTime);

                        intTimeBuffOld = uiTime;
                    }                    
                }
            }
        
        }  

        public void Start()
        {
            // ToDo thread restart, if it has been started and suspended, otherwise crash
            if (!bRunning)
            {
                thread.Start();
                bRunning = true;
            }
        }

        [Obsolete]
        public void Stop()
        {
            //ToDo implement
            if (bRunning)
            {
                thread.Suspend();
            }
        }


        public bool Connect(string strAdress)
        {
            PLC_Con.Connect(strAdress + " -timeout_connect=500" + " -user=boschrexroth" + " -password=boschrexroth");

            return PLC_Con.IsConnected;
        }

        public void Disconnect()
        {
            PLC_Con.Disconnect();
        }

        public void Close()
        {
            //ToDo
        }  

    } // class DAQ

}// namespace
