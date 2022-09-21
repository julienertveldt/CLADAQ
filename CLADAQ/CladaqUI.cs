﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics; //for stopwatch functionality
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using helperclasses;
using commlpiLib;
using System.Timers;
using CsvHelper;
using System.IO;            // for streamWriter
using System.Globalization; // for CultureInfo
using System.Xml;
using ScottPlot;
//using Opc.Ua;
//using Opc.Ua.Client;
//using Opc.Ua.Configuration;
using System.Threading.Tasks;

using Opc.UaFx;
using Opc.UaFx.Client;      // opcua.traeger.de
using CladaqLib;

namespace CLADAQ
{
    // form has to be first class
    public partial class CLADAQ : Form
    { 

        // constants (verified)
        protected static object lockObj = new Object();
        public static CultureInfo culture = new CultureInfo("EN-US");

        //constants (to check their need/properties)
        private static bool init_run = false;
    
        public static bool bDebugLog = true;

        protected static bool bSimValues = false;


        // =============== To be replaced ==========
        // Atention intBuffS also declared in Global
        public static int intBuffS = 100;                                // position value buffer sent by PLC: same as in GlobVarContant XM22

        private static int intAcqS;// = Global.intAcqS;                  // acquisition buffer to be written to file

        private static int intAcqDelay; // = Global.intAcqDelay;         // ms delay for acquisition timer
        private static int intIdxEnd;// = intBuffS;                      // number of values in pos data buffer
        private static int intNumBuffs;// = Global.intNumBuffs;          // number of (cyclic) buffers to use.
        private static int intPlotS;// = Global.intPlotS;                // number of points to plot
        private static int intPlotSkip;// = Global.intPlotSkip;          // plot 1 out of X samples

        public static int intSimFS = 500;                                    // Simulation frequency
        public static uint intNCh = 6;

        private static System.Windows.Forms.Timer dispTimer;            // Display refresh timer
        private static System.Windows.Forms.Timer acqTimer;                    // Acquisition refresh timer
        //private static System.Timers.Timer acqTimer;
        public static System.Windows.Forms.Timer simTimer;              // Simulation data generation timer
        private int intSimDelay;    // = intBuffS;                      // simulation timer delay (set equal to intBuffS)
        // ===================================

        //private static System.Threading.Timer aTimer;                 // Acquisition timer 


        private static string strFilePath = "C:/temp/test.csv";     

        private static OpcClient clientMTX = new OpcClient("opc.tcp://192.168.142.250:4840/");
        //private static OpcClient clientXM = new OpcClient("opc.tcp://192.168.142.3:4840/"); //XM adres
        //private static OpcClient clientMTX = new OpcClient("opc.tcp://localhost/");

        //private static Opc.Ua.client;

        //variables
        private double[] dblAxisPos = new double[20];               // axis positions
        private int job_Nr;                                         // threading task job (read, write)
        private MlpiConnection PLC_Con = new MlpiConnection();
        private object Par = new object();
        private object Data = new object();

        private bool writeCSV = false;                              // run acquisition of data without writing at launch

        private int[] intSimTRange = new int[intBuffS];
        //= Enumerable.Range(1, intIdxEnd).ToArray();
        
        // Already moved to DAQBuffer Class
        //private int intBuffPos = 0;                                 //current position in the position value buffer
        //private int intAcqBuffPos = 0;                              //current position in the acquired data buffer --> goes in CSV file
        //private int Idcount = 0;
        //private int b = 0;                                          //buffer index
        //private int intDispCounter = 0;                             //plot index



        private bool blPLCRunning = false;
        private bool bClientMTXConnected = false;

        // define buffers comming from MLPI
        protected static double[,] dblPosBuff = new double[intBuffS, intNCh];            // CNC positions + laser channel
        public static string[] strTimeBuff = new string[intBuffS];                        // CNC timestamp
        protected static UInt64[] intTimeBuff = new UInt64[intBuffS];
        protected static UInt64[] intTimeBuffOld = new UInt64[intBuffS];                     //previous cnc timestamp
        public static string[] strDateBuff = new string[intBuffS];


        // Display buffers
        protected double[,] dblAcqCh = new double[intNCh,intBuffS];
        protected double[] dblCh1 = new double[intBuffS];                             // Pos X
        protected double[] dblCh2 = new double[intBuffS];                             // Pos Y
        protected double[] dblCh3 = new double[intBuffS];                             // Pos Z
        protected double[] dblCh4 = new double[intBuffS];                             // Vel cmd
        protected double[] dblCh5 = new double[intBuffS];                             // Laser Cmd
        protected double[] dblCh6 = new double[intBuffS];                             // Laser Fdbck
        protected double[] dblCh7 = new double[intBuffS];                             // Medicoat FlowWatch
        protected UInt64[] uiTime = new UInt64[intBuffS];

        private string[] arOpcVal = new string[10];

        public double buffer = new double();

        public static DAQBuffer daqBuff;

        public static Simulator simData;

        // Display GUI static fields
        private static int intDispDelay; //= Global.intDispDelay;                        // ms delay for screen refresh


        // define internal CLADAQ buffers
        protected static List<DataRecord>[] listAcqBuffer = new List<DataRecord>[intNumBuffs];                  // data buffer to write
        protected List<string> csvString = new List<string>();                                           //CSV string to write

        // plot options
        protected bool bPlotOn = false;
        private bool bAutoSize = true;
        private double dblYMax = 10;
        private double dblYMin = 0;

        
        public event EventHandler ValueChanged;

        //public static CLADAQ mainForm;


        

        public CLADAQ()
        {
            CladaqLib.Global.InitializeApp();

            // Central buffer & Acquisition
            //intBuffS = Global.intBuffS;                // position value buffer sent by PLC: same as in GlobVarContant XM22
            intAcqS = Global.intAcqS;
            intAcqDelay = Global.intAcqDelay;          
            intIdxEnd = intBuffS;                      
            intNumBuffs = Global.intNumBuffs;
            
            // Plotting & GUI
            intPlotS = Global.intPlotS;                
            intPlotSkip = Global.intPlotSkip;
            intDispDelay = Global.intDispDelay;
            
            // Simulator
            intSimDelay = intBuffS;

        InitializeComponent();                      // Standard WinForms method for building Form
        }

        public class GlobalUI
        {
            public static Thread[] trd = new Thread[4];
            public static Stopwatch sw = new Stopwatch();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void CLADAQ_Load(object sender, EventArgs e)
        {
            lbStatus.Text = "Loading GUI content ...";

            // Create a timer and set an interval.
            dispTimer = new System.Windows.Forms.Timer();
            dispTimer.Interval = intDispDelay;
            dispTimer.Tick += new EventHandler(this.OnDispTimedEvent);

            //acqTimer = new System.Threading.Timer(new System.Threading.TimerCallback(this.OnAcquireTimedEvent));
            //acqTimer.Change(0, intAcqDelay);

            if (acqTimer == null)
            {
                acqTimer = new System.Windows.Forms.Timer();
                acqTimer.Interval = intAcqDelay;
                acqTimer.Tick += new EventHandler(this.OnAcquireTimedEvent);
                //acqTimer = new System.Timers.Timer();
                //acqTimer.Enabled = false;
                //acqTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnAcquireTimedEvent);
                //acqTimer.AutoReset = true;
                //acqTimer.Interval = intAcqDelay;
            }
          
               

            //simTimer = new System.Windows.Forms.Timer();
            //simTimer.Interval = intSimDelay;
            //simTimer.Tick += new EventHandler(this.OnSimTimedEvent);

            cpBar1.Text = "";
            lbBar1.Text = "Spindle";
            cpBar2.Text = "";
            lbBar2.Text = "Z";
            cpBar3.Text = "";
            lbBar3.Text = "X";
            cpBar4.Text = "";
            lbBar4.Text = "Y";
            cpBar5.Text = "";
            lbBar5.Text = "B";
            cpBar6.Text = "";
            lbBar6.Text = "C";
            cpBar7.Text = "";
            lbBar7.Text = "Laser";
            cpBar8.Text = "";
            lbBar8.Text = "Optics";
            cpBar9.Text = "";
            lbBar9.Text = "Nozzle";
            cpBar10.Text = "";
            lbBar10.Text = "Room";
            cpBar11.Text = "";
            lbBar11.Text = "TPW";
            cpBar12.Text = "";
            lbBar12.Text = "Table";
            cpBar13.Text = "";
            lbBar13.Text = "Spindle Load";
            cpBar14.Text = "";
            lbBar14.Text = "Spindle RPM";
            cpBar15.Text = "";
            lbBar15.Text = "Feed m/s";
            cpBar16.Text = "";
            lbBar16.Text = "Laser Power (W)";
            cpBar17.Text = "";
            lbBar17.Text = "PFR (%)";
            cpBar18.Text = "";
            lbBar18.Text = "Shielding (l/min)";
            tb_Ip_Address.Text = "192.168.142.3";
            cpBar19.Text = "";
            lbBar19.Text = "Carrier (l/min)";
            cpBar19.Maximum = 25;

            cbSimulate.Checked = bSimValues;
            cbWriteCSV.Checked = writeCSV;
            cbAutoSize.Checked = bAutoSize;
            //writeCSV = cbWriteCSV.Checked;

            //chart 1
            var ser1 = chart1.Series[0];
            ser1.Name = "VelCmd";
            ser1.Font = new Font("Arial", 8, FontStyle.Italic);
            ser1.ChartType = SeriesChartType.FastLine;
            ser1.Color = Color.CadetBlue;
            ser1.LabelBackColor = Color.Transparent;
            ser1.LabelForeColor = Color.DarkGray;
            double[] val1 = new double[] { 0, 3, 4, 9, 3, -2, 3, -4, -8, 6, 4, 5, 9, -1 };
            ser1.BorderWidth = 1;
            for (int ii = 1; ii < val1.Length - 1; ii++)
            {
                ser1.Points.AddY(val1[ii]);
            }

            var ser2 = this.chart1.Series.Add("");
            ser2.Name = "P Cmd";
            ser2.Font = new Font("Arial", 8, FontStyle.Italic);
            ser2.ChartType = SeriesChartType.FastLine;
            ser2.Color = Color.OrangeRed;
            ser2.LabelBackColor = Color.Transparent;
            ser2.LabelForeColor = Color.DarkGray;
            ser2.BorderWidth = 1;
            for (int ii = val1.Length - 1; ii > 0; ii--)
            {
                ser2.Points.AddY(val1[ii]);
            }

            var l1 = chart1.Legends[0];
            l1.Docking = Docking.Bottom;
            l1.Alignment = StringAlignment.Center;

            //chart 2
            var ser3 = chart2.Series[0];
            ser3.Name = "Pos X-Y";
            ser3.Font = new Font("Arial", 8, FontStyle.Italic);
            ser3.ChartType = SeriesChartType.FastLine;
            ser3.Color = Color.CadetBlue;
            ser3.LabelBackColor = Color.Transparent;
            ser3.LabelForeColor = Color.DarkGray;
            ser3.BorderWidth = 1;


            var l2 = chart2.Legends[0];
            l2.Docking = Docking.Bottom;
            l2.Alignment = StringAlignment.Center;

            tbLog.ScrollBars = ScrollBars.Vertical;
            tbLog.WordWrap = false;

            //this.mainForm = this;

            this.ValueChanged += this.CallUpdateBuffer;
            

            if (bSimValues)
                intIdxEnd = intBuffS;

            //for (int i = 0; i < intNumBuffs; i++)
            //{
            //    listAcqBuffer[i] = new List<DataRecord>();
            //}     


            DateTime now = DateTime.UtcNow;
            tbFilePath.Text = string.Concat("c:/temp/sim", now.ToString(@"_yyyy-MM-dd-HH-mm-ss-FF", culture), ".csv");

            this.treeView2.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView2_NodeMouseClick);

            Browser.PopulateTreeView(this.treeView2);

            daqBuff = new DAQBuffer(intNumBuffs, intAcqS);

            simData = new Simulator(daqBuff);
            simData.dblSimDelay = 20;

            lbStatus.Text = "Ready ...";

            Thread.Sleep(200);
           
        }


        public void CLADAQ_Shown(object sender, EventArgs e)
        {
            var autoEvent = new AutoResetEvent(false);

            Thread trd0 = GlobalUI.trd[0] = Thread.CurrentThread;             // GUI
            Thread trd1 = GlobalUI.trd[1] = new Thread(Task_1_DoWork);        // Simulataion
            Thread trd2 = GlobalUI.trd[2] = new Thread(Task_2_DoWork);        // Acquisition
            Thread trd3 = GlobalUI.trd[3] = new Thread(Task_3_DoWork);

            trd0.Priority = ThreadPriority.BelowNormal;
            trd1.Priority = ThreadPriority.AboveNormal;
            trd2.Priority = ThreadPriority.AboveNormal;
            trd3.Priority = ThreadPriority.BelowNormal;

            trd1.Start();
            //trd2.Start();
            trd3.Start();

            GlobalUI.sw = new Stopwatch();
            GlobalUI.sw.Start();

            Thread thread = Thread.CurrentThread;

            if (bDebugLog)
            {
                string msg = String.Format("Thread ID of GUI: {0} " + Environment.NewLine, thread.ManagedThreadId);
                FormTools.AppendText(this, tbLog, ">  " + msg + Environment.NewLine);
            }

            //lock (lockObj)

            Thread.Sleep(100);

        }

        private void Task_1_DoWork()                // simulation thread
        {

            Thread.Sleep(200);
        }

        private void Task_2_DoWork()                 //  acquisition thread
        {
            //this.buffer = dblPosBuff[intBuffS - 1];
            while (true)
            {
                if (PLC_Con.IsConnected && blPLCRunning)
                {
                    //this.OnValueChanged(null);
                }

                Thread.Sleep(100);
            }

        }

        private void Task_3_DoWork()
        {
            while (false)
            {
                try
                {
                    //do your thing;
                }
                catch (Exception ex)
                { }

                Thread.Sleep(2000);
            }
        }

        private void ReadWrite(params object[] list) // Run read or write task
        {
            string msg;
            Thread thread = Thread.CurrentThread;
            lock (lockObj)

                job_Nr = Convert.ToInt32(list[0]);
            if (list.Length > 1)
            {
                Par = Convert.ToString(list[1]);
                Data = list[2];
            }
            {
                msg = String.Format("Thread ID: {0}", thread.ManagedThreadId);
            }
            TimeSpan t1 = GlobalUI.sw.Elapsed;
            //TimeSpan t2 = (t1.Ticks - (t1.Ticks % 10000));
            FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"hh\:mm\:ss\.") + " Task 1 by " + msg + Environment.NewLine);

            if (PLC_Con.IsConnected)
            {
                switch (job_Nr)
                {
                    case 1:      //read variables

                        Diagnosis diagnosis = PLC_Con.System.GetDisplayedDiagnosis();
                        // add timestamp
                        string strFormattedOutput = "";
                        strFormattedOutput += "Time: "
                            + diagnosis.dateTime.day.ToString("00") + "."
                            + diagnosis.dateTime.month.ToString("00") + "."
                            + diagnosis.dateTime.year.ToString("00") + " - "
                            + diagnosis.dateTime.minute.ToString("00") + ":"
                            + diagnosis.dateTime.hour.ToString("00") + ":"
                            + diagnosis.dateTime.second.ToString("00") + System.Environment.NewLine;
                        // add current state
                        strFormattedOutput += "State: " + diagnosis.state.ToString() + System.Environment.NewLine;
                        // add dispatcher of diagnosis
                        strFormattedOutput += "Despatcher: " + diagnosis.despatcher.ToString() + System.Environment.NewLine;
                        // add error number
                        strFormattedOutput += "Number: 0x" + String.Format("{0:X}", diagnosis.number) + System.Environment.NewLine;
                        // add description
                        strFormattedOutput += "Text: " + diagnosis.text;
                        // print to console
                        //Console.WriteLine(strFormattedOutput);
                        FormTools.AppendText(this, tbLog, strFormattedOutput + Environment.NewLine);

                        break;

                    case 2:     //'Write variable - par = "Application.GVL_x.var_x"

                        try
                        {
                            string strPar = Convert.ToString(Par);
                            PLC_Con.Logic.WriteVariableBySymbol(strPar, Data);
                            FormTools.AppendText(this, tbLog, "> PLC motion started succesfull." + Environment.NewLine);
                        }
                        catch (Exception ex)
                        {
                            FormTools.AppendText(this, tbLog, "> Could not start motion on PLC." + Environment.NewLine);

                        }



                        break;
                }

            }
            else
            {

            }

        }

        public void CLADAQ_FormClosing(object sender, FormClosingEventArgs e)
        {
            //aTimer.Dispose();
            try
            {
                simTimer.Dispose();
                if (clientMTX != null)
                {
                    clientMTX.Disconnect();
                }
                //csv.Flush();
                //csv.Dispose();

            } catch (Exception ex)
            { }


            GlobalUI.trd[1].Abort();
            GlobalUI.trd[2].Abort();
            GlobalUI.trd[3].Abort();

        }

        private void tbLog_TextChanged(object sender, EventArgs e)
        {

        }

        private void bt_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                PLC_Con.Connect(tb_Ip_Address.Text + " -timeout_connect=500" + " -user=boschrexroth" + " -password=boschrexroth");

                if (PLC_Con.IsConnected)
                {
                    tb_Ip_Address.BackColor = Color.Lime;


                    if (init_run != true)
                    {
                        init_run = true;
                    }

                    FormTools.AppendText(this, tbLog, "> " + "Connected" + Environment.NewLine);
                    lbStatus.Text = "Connected";
                }
            }
            catch (Exception ex)
            {
                init_run = false;
                tb_Ip_Address.BackColor = Color.Red;
                lbStatus.Text = "Error on connecting";

                FormTools.AppendText(this, tbLog, "> " + "Error connecting:" + Environment.NewLine);
                FormTools.AppendText(this, tbLog, "> " + ex.ToString() + Environment.NewLine);
            }
        }

        private void bt_Start_Click(object sender, EventArgs e)
        {


            ReadWrite(2, "Application.HmiVarGlobal.dwHmiBitControl_gb", "1");
            blPLCRunning = true;

            FormTools.AppendText(this, tbLog, "> " + "Start signal to PLC sent" + Environment.NewLine);
            //while (PLC_Con.IsConnected)
            //{
            //    // ReadWrite(1);
            //    Thread.Sleep(1000);
            //}

            lbStatus.Text = "PLC Started";

        }

        private void bt_Stop_Click(object sender, EventArgs e)
        {
            try
            {
                ReadWrite(2, "Application.HmiVarGlobal.dwHmiBitControl_gb", "2");
                FormTools.AppendText(this, tbLog, "> " + "PLC stopped" + Environment.NewLine);
                lbStatus.Text = "PLC stopped";

                PLC_Con.Disconnect();
                FormTools.AppendText(this, tbLog, "> " + "Succesfully disconected" + Environment.NewLine);
                tb_Ip_Address.BackColor = Color.White;
                lbStatus.Text = "Disconnected";
                blPLCRunning = false;
            }
            catch (Exception ex)
            {
                FormTools.AppendText(this, tbLog, "> " + "Error disconnecting:" + Environment.NewLine);
                FormTools.AppendText(this, tbLog, "> " + ex.ToString() + Environment.NewLine);
                lbStatus.Text = "Error diconnecting";
            }

            dispTimer.Stop();
        }

        public void OnDispTimedEvent(Object source, EventArgs myEventArgs)          // acquisition timer
        {

            DataPoint dp = new DataPoint();

            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            //Thread thread = Thread.CurrentThread;
            //string msg = String.Format("Thread ID: {0}\n", thread.ManagedThreadId);
            //FormTools.AppendText(this, tbLog, ">  " + msg);

            //lb_Pos_As1.Text = dblAxisPos[15].ToString();
            //lb_Pos_As1.Text = dblAxisPos[16].ToString();

            //lb_Pos_As1.Text = dblPosBuff[intIdxEnd - 1, 0].ToString();
            //lb_Pos_As2.Text = dblPosBuff[intIdxEnd - 1, 1].ToString();

            dispTimer.Stop();

            for (int i = 0; i < intBuffS; i = i + 10)
            {
                if (strTimeBuff[i] != null)
                {
                    if (chart1.Series[0].Points.Count == 0)
                    { dp.XValue = 0; }
                    else
                    { dp = chart1.Series[0].Points.Last(); }

                    // Define plot channels

                    // Positions X & Y
                    //chart1.Series[0].Points.AddXY(dp.XValue + 1, dblCh1[i]); //dblCh1
                    //chart1.Series[1].Points.AddXY(dp.XValue + 1, dblCh2[i]); //dblCh2

                    // Vel & Power
                    chart1.Series[0].Points.AddXY(dp.XValue + 1, uiTime[i]);
                    //chart1.Series[0].Points.AddXY(dp.XValue + 1, dblAcqCh[1-1,i] * 6 / 1000); //dblCh1 dblAcqCh[1-1,i] * 6 / 1000
                    //chart1.Series[1].Points.AddXY(dp.XValue + 1, dblAcqCh[6-1,i]); //dblCh2

                    //chart2.Series[0].Points.AddXY(dblCh1[i], dblCh2[i]);
                    chart2.Series[0].Points.AddXY(dblAcqCh[1-1,i], dblAcqCh[2-1,i]); //dblCh1
                    //chart2.Series[1].Points.AddXY(dp.XValue + 1, dblCh2[i]); //dblCh2
                }
            }
            if (strTimeBuff[1] != null)
            {
                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:fffffff\.") + " : Time buffer returns zero. " + Environment.NewLine);
            }

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                while (chart1.Series[i].Points.Count > intPlotS)
                {
                    chart1.Series[i].Points.RemoveAt(0);
                }
            }

            while (chart2.Series[0].Points.Count > intPlotS)
            {
                chart2.Series[0].Points.RemoveAt(0);
            }

            if (bAutoSize)
            {
                try
                {
                    dp = chart1.Series[0].Points.FindMaxByValue("Y1", 0);
                    chart1.ChartAreas[0].AxisY.Maximum = Math.Round(dp.YValues[0] * 1.1) + 1;

                    dp = chart1.Series[1].Points.FindMinByValue("Y1", 0);
                    chart1.ChartAreas[0].AxisY.Minimum = Math.Sign(dp.YValues[0]) * Math.Abs(Math.Round(dp.YValues[0])) * 1.1; //-0.001 quick fix to avoid Ymin = Ymax

                    dp = chart2.Series[0].Points.FindMaxByValue("Y1", 0);
                    chart2.ChartAreas[0].AxisY.Maximum = Math.Round(dp.YValues[0] * 1.1) + 1;

                    dp = chart2.Series[0].Points.FindMinByValue("Y1", 0);
                    chart2.ChartAreas[0].AxisY.Minimum = Math.Sign(dp.YValues[0]) * Math.Abs(Math.Round(dp.YValues[0])) * 1.1 - 1; //-0.001 quick fix to avoid Ymin = Ymax

                    dp = chart2.Series[0].Points.FindMaxByValue("X", 0);
                    chart2.ChartAreas[0].AxisX.Maximum = Math.Round(dp.XValue * 1.1) + 1;

                    dp = chart2.Series[0].Points.FindMinByValue("X", 0);
                    chart2.ChartAreas[0].AxisX.Minimum = Math.Sign(dp.XValue * Math.Abs(Math.Round(dp.XValue))) * 1.1 - 1;
                }
                catch (Exception exc)
                { }
            }
            else
            {
                //dp = chart1.Series[0].Points.FindMaxByValue("X", 0);
                //chart1.ChartAreas[0].AxisX.Maximum = dp.XValue;
                //chart1.ChartAreas[0].AxisX.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Maximum = dblYMax;
                chart1.ChartAreas[0].AxisY.Minimum = dblYMin;

                chart2.ChartAreas[0].AxisY.Maximum = 1600;
                chart2.ChartAreas[0].AxisY.Minimum = 0;
                chart2.ChartAreas[0].AxisX.Maximum = 800;
                chart2.ChartAreas[0].AxisX.Minimum = 0;
            }

            try
            {
                dp = chart1.Series[0].Points.FindMaxByValue("X", 0);
                chart1.ChartAreas[0].AxisX.Maximum = Math.Round(dp.XValue);

                dp = chart1.Series[0].Points.FindMinByValue("X", 0);
                chart1.ChartAreas[0].AxisX.Minimum = Math.Round(dp.XValue);
            }
            catch (Exception exc)
            { }

            // OPCUA display

            if (bClientMTXConnected == true)
            {
                var statusNode = clientMTX.BrowseNode("ns=27;s=NC.CplPermVariable,@CG") as OpcVariableNodeInfo;

                if (statusNode != null)
                {
                    var statusValues = statusNode.DataType.GetEnumMembers();

                    var currentStatus = clientMTX.ReadNode(statusNode.NodeId);
                    var currentStatusValue = null as OpcEnumMember;

                    foreach (var statusValue in statusValues)
                    {
                        if (statusValue.Value == currentStatus.As<int>())
                        {
                            currentStatusValue = statusValue;
                            break;
                        }
                    }

                    int intCG = currentStatus.As<int>();
                    cpBar19.Value = intCG;
                    cpBar19.Text = intCG.ToString();
                   // string msg = String.Format("Current carrier gas setpoint is {0} L/min", intCG);
                   // FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
                }

                try
                {
                    var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActNcBlock,02");
                    lblOPCVal1.Text = dummy.ToString();

                    var dum2 = clientMTX.ReadNode("ns=27;s=NC.Chan.ActCallChain,02,BlockN");
                    lblOPCVal2.Text = dum2.ToString();
                }
                catch (Exception ex)
                { }
            }


            if (bDebugLog)
            {
                Thread thread = Thread.CurrentThread;
                lock (lockObj)
                {
                    TimeSpan t1 = GlobalUI.sw.Elapsed;
                    string msg = String.Format(t1.ToString(@"ss\:fffffff\.") + " Disp event from Thread ID: {0}\n", thread.ManagedThreadId);
                    FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
                }
            }

            dispTimer.Enabled = true;


            //int intStatMTX = String.Compare(clientMTX.State.ToString(), "Connected");
            //if (intStatMTX == 0)
            //{
            //    string str = clientMTX.ReadNode("ns=2;s=PLC.GlobalDAQ.dtSysTime_gb").ToString(); //ns=2;s=PLC.GlobalDAQ.dtSysTime_gb
            //    lblOPCVal1.Text = ("System time: " + str);

            //    str = clientMTX.ReadNode("ns=2;s=PLC.GlobalDAQ.dwLaserProgTon_gb").ToString(); //ns=2;s=PLC.GlobalDAQ.dtSysTime_gb
            //    lblOPCVal2.Text = ("Laser on time: " + str);
            //}
            //else
            //{
            //    lblOPCStatus.Text = "Connection lost";
            //}

            //Thread.Sleep(100);

        }



        protected virtual void OnAcquireTimedEvent(Object source, EventArgs myEventArgs)          // Acquisition timerµ
        //protected void OnAcquireTimedEvent(Object state)
        {
            if (PLC_Con.IsConnected && !bSimValues) //&& blPLCRunning
            {
                if (bDebugLog)
                {
                    TimeSpan t1 = GlobalUI.sw.Elapsed;
                    FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Polling time buffer" + Environment.NewLine);
                }


                uiTime = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.tTimeBufSent_gb");//tTimeBuf1_gb

                if ((uiTime.SequenceEqual(intTimeBuffOld)) == false)
                {
                    double[] tempBuf = new double[intBuffS];
                    if (bDebugLog)
                    {
                        TimeSpan t1 = GlobalUI.sw.Elapsed;
                        FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Reading new axis position" + Environment.NewLine);
                    }

                    string dummyPath = "";
                    for (int ii = 1; ii < (intNCh+1); ii += 1)
                    {
                        dummyPath = "Application.PlcVarGlobal.reBuf" + ii.ToString() +  "_Sent_gb";

                        tempBuf = PLC_Con.Logic.ReadVariableBySymbol(dummyPath);
                        System.Buffer.BlockCopy(tempBuf, 0, dblAcqCh, intBuffS * (ii-1) * 8, intBuffS*8);
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
                    //this.OnValueChanged(null);
                    CallUpdateBuffer(null,null);



                    ulong dummy = intTimeBuff[1];
                }
            }

            

        }

        [Category("Action")]
        [Description("Trigger acquisition when buffer value is changed")]

        protected virtual void OnValueChanged(EventArgs e)
        {
            //EventHandler handler = triggerAcquisition;
            // (2)
            // Raise the event
            if (ValueChanged != null)
                ValueChanged(this, e);
            //handler?.Invoke(this, e);
        }


        protected void CallUpdateBuffer(object sender, EventArgs e)
        {

            try
            {
                daqBuff.AppendToBuffer(dblAcqCh, intTimeBuff);
                if (bDebugLog)
                {
                    Thread thread = Thread.CurrentThread;
                    lock (lockObj)
                    {

                        TimeSpan t1 = GlobalUI.sw.Elapsed;
                        string msg = String.Format(t1.ToString(@"ss\:FF\.") + " :  Appended to buffer.");
                        FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
                        msg = String.Format(" Acquisition event from Thread ID: {0}\n", thread.ManagedThreadId);
                        FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FF\.") + " :" + " Error appending to buffer." + Environment.NewLine);
                throw;
            }         
        }

        private void cbSimulate_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSimulate.Checked == false)
            {
                //simTimer.Stop();
                //simTimer.Dispose();
                //dispTimer.Stop();

                simData.StopSimulator();
                bSimValues = false;

                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FF\.") + " :" + " Stopped data simulation." + Environment.NewLine);

            }
            else
            {
                //cbAcquisition.Checked = false;
                //var autoEvent = new AutoResetEvent(false);

                //// program wait for buffer to be filled
                //simTimer.Start();
                //dispTimer.Start();

                simData.StartSimulator();  
                bSimValues = true;

                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FF\.") + " :" + " Started data simulation." + Environment.NewLine);
            }
        }

        private void cbAcquisition_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAcquisition.Checked == true)
            {
                // Start the timer
                if (acqTimer != null)
                {
                    acqTimer.Start();
                    //acqTimer = new System.Threading.Timer( new System.Threading.TimerCallback(this.OnAcquireTimedEvent));
                    //acqTimer.Change(0, intAcqDelay);
                }
                cbSimulate.Checked = false;
                dispTimer.Start();

            }
            else
            {
                acqTimer.Dispose();
                dispTimer.Stop();

            }

        }

        private void tbFilePath_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string strFile = tbFilePath.Text;
                DateTime localDate = DateTime.Now;


                int idx1 = strFile.IndexOf(".");
                if (idx1 > 0)
                    strFile = strFile.Remove(idx1);

                String strDate = localDate.ToString(@"_yyyy-MM-dd-HH-mm-ss-FF", culture);

                //string strNewFile = string.Concat(strFile, strDate, ".csv");
                string strNewFile = string.Concat(strFile, ".csv");
                //int idx2 = strNewFile.LastIndexOf("_");

                strFile = strNewFile;
                strFilePath = strNewFile;
                tbFilePath.Text = strFile;
            }

        }

        private void cbAutoSize_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAutoSize.Checked)
                bAutoSize = true;
            else
                bAutoSize = false;
        }

        private void cbWriteCSV_CheckedChanged(object sender, EventArgs e)
        {
            if (cbWriteCSV.Checked)
            {
                strFilePath = tbFilePath.Text;
                //writer = new StreamWriter(strFilePath, true);
                //writer.AutoFlush = true;
                //csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                //;

                int res = daqBuff.StartWriter(strFilePath);

                if (res > 0)
                { 
                    FormTools.AppendText(this, tbLog, ">  CSV stream sucesfully opened on file: " + strFilePath + Environment.NewLine);
                    writeCSV = true;    
                }
                else { 
                    FormTools.AppendText(this, tbLog, ">  CSV stream could NOT be opened at: " + strFilePath + Environment.NewLine);
                    writeCSV = false;
                }
            }
            else
            {
                int res = daqBuff.CloseWriter();
                writeCSV = false;
                FormTools.AppendText(this, tbLog, ">  CSV stream closed at: " + strFilePath + Environment.NewLine);
            }
        }

        private void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView2.Items.Clear();
            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                    {new ListViewItem.ListViewSubItem(item, "Directory"),
             new ListViewItem.ListViewSubItem(item,
                dir.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                listView2.Items.Add(item);
            }
            foreach (FileInfo file in nodeDirInfo.GetFiles())
            {
                item = new ListViewItem(file.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                    { new ListViewItem.ListViewSubItem(item, "File"),
             new ListViewItem.ListViewSubItem(item,
                file.LastAccessTime.ToShortDateString())};

                item.SubItems.AddRange(subItems);
                listView2.Items.Add(item);
            }

            listView2.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void btnLoadProps_Click(object sender, EventArgs e)
        {
            //Create xml reader
            XmlReader xmlFile = XmlReader.Create("C:/temp/testProps.xml", new XmlReaderSettings());
            System.Data.DataSet dataSet = new System.Data.DataSet();
            //Read xml to dataset
            dataSet.ReadXml(xmlFile);
            //Pass empdetails table to datagridview datasource
            dgvProperties.DataSource = dataSet.Tables["channel"];

            dgvProperties.Columns["id"].Visible = false;

            DataGridViewCell cellName = new DataGridViewTextBoxCell();
            cellName.Style.BackColor = Color.LightGray;

            dgvProperties.Columns["name"].CellTemplate = cellName;
            dgvProperties.Columns["name"].DisplayIndex = 1;
            dgvProperties.Columns["value"].DisplayIndex = 2;
            dgvProperties.Columns["type"].DisplayIndex = 3;
            //Close xml reader
            xmlFile.Close();

        }

        private void btnPlot_Click(object sender, EventArgs e)
        {
            var plt = new ScottPlot.Plot(600, 400);

            Random rand = new Random(0);
            int pointCount = (int)1e6;
            int lineCount = 5;

            for (int i = 0; i < lineCount; i++)
                plt.PlotSignal(DataGen.RandomWalk(rand, pointCount));

            plt.Title("Signal Plot Quickstart (5 million points)");
            plt.YLabel("Vertical Units");
            plt.XLabel("Horizontal Units");
            FormTools.AppendText(this, tbLog, "> " + "Done plotting" + Environment.NewLine);

        }

        private async void btnOPCConnect_Click(object sender, EventArgs e)
        {

            //await connectOPC();

            //tbLog.Refresh();

            // Configuration property.
            clientMTX.Configuration.ClientConfiguration.DefaultSessionTimeout = 10000; // 10s
            clientMTX.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates = true;


            //load existing certificate
            //var certificate = OpcCertificateManager.LoadCertificate(@"D:\OneDrive - Vrije Universiteit Brussel\Soft-dev\CLADAQ\opc-info\certs\CLADAQ.der");

            //generate new certificate
            var certificate = OpcCertificateManager.CreateCertificate(clientMTX);

            //Save a certificate in any path:

            OpcCertificateManager.SaveCertificate("CLADAQcert.der", certificate);

            //Set the Client certificate:
            clientMTX.Certificate = certificate;

            //The certificate has to be stored in the Application Store:

            if (!clientMTX.CertificateStores.ApplicationStore.Contains(certificate))
                clientMTX.CertificateStores.ApplicationStore.Add(certificate);

            //If no or an invalid certificate is used, a new certificate is generated / used by default.If the Client shall only use the mentioned certificate this function has to be deactivated.For deactivating the function set the property AutoCreateCertificate to the value false:

            clientMTX.CertificateStores.AutoCreateCertificate = true;


            //try
            //{
                clientMTX.Connect();
                lblOPCStatus.Text = "Connected to MiCLAD ";
                bClientMTXConnected = true;


            // }
            // catch (Exception ex)
            // {
            //   lblOPCStatus.Text = "Error connecting to MiCLAD";
            //   FormTools.AppendText(this, tbLog, "> Could not connect to MiCLAD MTX." + Environment.NewLine);
            //   FormTools.AppendText(this, tbLog, "> Error message:" + ex.ToString() + Environment.NewLine);
           // }

        }
    }
}





//public void OnSimTimedEvent(Object source, EventArgs myEventArgs)         // Simulation timer
//{
//    DateTime now = new DateTime();
//    double nowS = new double();

//    Random rnd = new Random();
//    for (int i = 0; i < intBuffS; i++)
//    {
//        DateTime nowT = DateTime.Now;
//        nowS = nowT.Hour * 3600 + nowT.Minute * 60 + nowT.Second + now.Millisecond / 1000; //add 2 ms per sample to have 500Hz rate.

//        intTimeBuff[i] = Convert.ToUInt64(nowS * 1000);

//        for (int ii = 0; ii < intNCh; ii++)
//        {
//            dblPosBuff[i, ii] = 5 * Math.Sin(10 / 1000 * (nowS) * 6.28 + rnd.Next(1));
//            //dblPosBuff[i, ii] = (nowS + i + ii)/166 - 0.5;
//        }
//        dblAcqCh[0, i] = dblPosBuff[i, 0];
//        dblAcqCh[1, i] = dblPosBuff[i, 1];
//        dblAcqCh[2, i] = dblPosBuff[i, 2];
//        dblAcqCh[3, i] = 0; // dblPosBuff[i, 3];
//        dblAcqCh[4, i] = 0; //dblPosBuff[i, 4];
//        dblAcqCh[5, i] = 0; //dblPosBuff[i, 5];

//        Thread.Sleep(1000 / intSimFS);
//    }


//    this.OnValueChanged(null);      //trigger buffer event if acq buffer full

//    double dblPosEnd = dblPosBuff[intIdxEnd - 1, intNCh - 1];

//    Thread thread = Thread.CurrentThread;

//    if (bDebugLog)
//    {
//        TimeSpan t1 = GlobalUI.sw.Elapsed;
//        string msg = String.Format(t1.ToString(@"ss\:fffff\.") + " Sim event from Thread ID: {0}\n", thread.ManagedThreadId);
//        FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
//    }


//    //}
//    //catch (Exception ex)
//    //{ }
//}