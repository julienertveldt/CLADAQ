using commlpiLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CladaqLib
{
    public class DAQ : INotifyPropertyChanged
    {
        private MlpiConnection PLC_Con = new MlpiConnection();

        protected double[,] dblAcqCh;
        protected double[] dblCh1;                              // Pos X
        protected double[] dblCh2;                              // Pos Y
        protected double[] dblCh3;                              // Pos Z
        protected double[] dblCh4;                              // Vel cmd
        protected double[] dblCh5;                              // Laser Cmd
        protected double[] dblCh6;                              // Laser Fdbck
        protected double[] dblCh7;                              // Medicoat FlowWatch
        protected UInt64[] uiTime;

        private int intBuffS;
        private uint intNCh;

        private System.Timers.Timer timer;

        // define buffers comming from MLPI
        protected static double[,] dblPosBuff;            // CNC positions + laser channel
        public static string[] strTimeBuff;                        // CNC timestamp
        protected static UInt64[] intTimeBuff;
        protected static UInt64[] intTimeBuffOld;                     //previous cnc timestamp
        public static string[] strDateBuff;

        protected DAQBuffer daqBuff;

        public double dblAcqDelay { get; set; }
        public bool bRunning { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public DAQ(DAQBuffer daqBuffIn, int intBuffSIn, uint intNChIn, uint intAcqDelay)
        {
            intBuffS = intBuffSIn;
            intNCh = intNChIn;

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

            if (intAcqDelay == 0)
            {
                intAcqDelay = 10;
            }

            timer = new System.Timers.Timer();
            timer.Interval = intAcqDelay;
            timer.AutoReset = true;
            timer.Enabled = false;
            timer.Elapsed += OnAcquireTimedEvent;

        }

        protected void OnAcquireTimedEvent(Object source, EventArgs myEventArgs)          // Acquisition timerµ
        //protected void OnAcquireTimedEvent(Object state)
        {

            if (PLC_Con.IsConnected) //&& blPLCRunning
            {
                timer.Stop();
                //if (bDebugLog)
                //{
                //    TimeSpan t1 = GlobalUI.sw.Elapsed;
                //    FormTools.AppendText(uiForm, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Polling time buffer" + Environment.NewLine);
                //}


                uiTime = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.tTimeBufSent_gb");//tTimeBuf1_gb

                if ((uiTime.SequenceEqual(intTimeBuffOld)) == false)
                {
                    double[] tempBuf = new double[intBuffS];
                    //if (bDebugLog)
                    //{
                    //    TimeSpan t1 = GlobalUI.sw.Elapsed;
                    //    FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Reading new axis position" + Environment.NewLine);
                    //}

                    string dummyPath = "";
                    for (int ii = 1; ii < (intNCh + 1); ii += 1)
                    {
                        dummyPath = "Application.PlcVarGlobal.reBuf" + ii.ToString() + "_Sent_gb";

                        tempBuf = PLC_Con.Logic.ReadVariableBySymbol(dummyPath);
                        System.Buffer.BlockCopy(tempBuf, 0, dblAcqCh, intBuffS * (ii - 1) * 8, intBuffS * 8);
                    }

                    //temporary fix to avoid Null strTimeBuff
                    DateTime now = DateTime.Now;
                    for (int i = 0; i < intBuffS; i += 1)
                    {
                        intTimeBuff[i] = uiTime[i];
                        strTimeBuff[i] = uiTime[i].ToString();

                        strDateBuff[i] = now.ToString(@"yyyy-MM-dd");
                        strTimeBuff[i] = now.ToString(@"HH\:mm\:ss\.FFFFFF");
                    }

                    intTimeBuffOld = uiTime;

                    lock (dblAcqCh)
                    {
                        lock (intTimeBuff)
                        {
                            if (daqBuff != null)
                            {
                                daqBuff.AppendToBuffer(dblAcqCh, intTimeBuff);
                                //intTimeBuff = null;
                                //dblBuff = null;
                            }
                        }
                    }

                }

                OnPropertyChanged();
                timer.Start();
            }

        }

        public void Start()
        {

            timer.Enabled = true;
            bRunning = true;

        }

        public void Stop()
        {
            timer.Enabled = false;
            bRunning = false;
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
            timer.Dispose();
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    } // class DAQ

}// namespace
