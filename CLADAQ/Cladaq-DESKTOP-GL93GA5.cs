using System;
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


namespace CLADAQ
{
    // form has to be first class
    public partial class CLADAQ : Form
    {
        // objects
        private static object lockObj = new Object();
        public static CLADAQ cladaq1 = new CLADAQ();

        //constants
        private static bool init_run = false;

        private static UInt32 dwHmiBitStatus_gb; //check type in Indraworks

        private static bool bDebugLog = true;

        private static bool bSimValues = false;
        public static bool writeCSV = false;                          // run acquisition of data

        private static System.Windows.Forms.Timer dispTimer;          // Display refresh timer
        private static System.Windows.Forms.Timer acqTimer;           // Acquisition refresh timer
        public static System.Windows.Forms.Timer simTimer;                  // Simulation data generation timer
        //private static System.Threading.Timer aTimer;               // Acquisition timer 

        private static int intBuffS = 5 * 20;                           // position value buffer sent by PLC: same as in GlobVarContant XM22
        private static int intAcqS = intBuffS * 10;                   // acquisition buffer to be written to file
        private static int intDispDelay = 1000 / 4;                     // ms delay for screen refresh
        private static int intAcqDelay = 10;                          // ms delay for acquisition timer
        private static int intIdxEnd = intBuffS;                      // number of values in pos data buffer
        private static int intNumBuffs = 2;                           // number of (cyclic) buffers to use.
        private static int intPlotS = intAcqS / 2;                            // number of points to plot
        private static int intPlotSkip = 10;                          // plot 1 out of X samples

        private static CultureInfo culture = new CultureInfo("EN-US");

        private static string strFilePath = "C:/temp/cube2/lay_.csv";

        private static int intAcqChans = 6;                          //X,Y,Z,B,C <Double>
        private static int intLaserChans = 2;                        //LaserOut, LaserIn <Double>

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

        private int[] intSimTRange = new int[intBuffS];
        //= Enumerable.Range(1, intIdxEnd).ToArray();

        private int intBuffPos = 0;                                 //current position in the position value buffer
        private int intAcqBuffPos = 0;                              //current position in the acquired data buffer --> goes in CSV file
        private int Idcount = 0;
        private int b = 0;                                          //buffer index
        private int count = 0;                                      //plot index OLD version
        private int intDispCounter = 0;                             //plot index
        private int intSimDelay = intBuffS;                         // simulation timer delay (set equal to intBuffS)


        private bool blPLCRunning = false;
        private bool bClientMTXConnected = false;

        // define buffers comming from MLPI

        private double[,] dblPosBuff = new double[intBuffS, intAcqChans];            // CNC positions + laser channel
        private string[] strTimeBuff = new string[intBuffS];                        // CNC timestamp
        private UInt64[] intTimeBuff = new UInt64[intBuffS];
        private UInt64[] intTimeBuffOld = new UInt64[intBuffS];                     //previous cnc timestamp
        private string[] strDateBuff = new string[intBuffS];


        private double[] dblCh1 = new double[intBuffS];                             // Pos X
        private double[] dblCh2 = new double[intBuffS];                             // Pos Y
        private double[] dblCh3 = new double[intBuffS];                             // Pos Z
        private double[] dblCh4 = new double[intBuffS];                             // Vel cmd
        private double[] dblCh5 = new double[intBuffS];                             // Laser Cmd
        private double[] dblCh6 = new double[intBuffS];                             // Laser Fdbck
        private double[] dblCh7 = new double[intBuffS];                             // Medicoat FlowWatch
        private UInt64[] uiTime = new UInt64[intBuffS];

        private string[] arOpcVal = new string[10];

        // define internal CLADAQ buffers
        private List<dataRecord>[] listAcqBuffer = new List<dataRecord>[intNumBuffs];                  // data buffer to write
        private List<string> csvString = new List<string>();        //CSV string to write

        private StreamWriter writer;
        private CsvWriter csv;

        // plot options
        private bool bPlotOn = false;
        private bool bAutoSize = true;
        private double dblYMax = 10;
        private double dblYMin = 0;


        //public DataBuffer datBuff = new DataBuffer();
        //private DataBuffer datBuff;

        public CLADAQ()
        {
            InitializeComponent();
        }

        public class Global
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

            acqTimer = new System.Windows.Forms.Timer();
            acqTimer.Interval = intAcqDelay;
            acqTimer.Tick += new EventHandler(this.OnAcquireTimedEvent);

            simTimer = new System.Windows.Forms.Timer();
            simTimer.Interval = intSimDelay;
            simTimer.Tick += new EventHandler(this.OnSimTimedEvent);

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
            lbBar16.Text = "Laser Power";
            cpBar17.Text = "";
            lbBar17.Text = "Powder flow";
            cpBar18.Text = "";
            lbBar18.Text = "Shielding";
            tb_Ip_Address.Text = "192.168.142.3";

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

            /* val1 = [0, 0];
             * for (int ii = 1; ii < val1.Length - 1; ii++)
            {
                ser3.Points.AddXY(val1[ii],val1[ii]);
            }*/
            var l2 = chart2.Legends[0];
            l2.Docking = Docking.Bottom;
            l2.Alignment = StringAlignment.Center;

            tbLog.ScrollBars = ScrollBars.Vertical;
            tbLog.WordWrap = false;

            //datBuff = new DataBuffer();

            this.mainForm = this;

            this.ValueChanged += this.UpdateBuffer;

            if (bSimValues)
                intIdxEnd = intBuffS;

            for (int i = 0; i < intNumBuffs; i++)
            {
                listAcqBuffer[i] = new List<dataRecord>();
            }     


            DateTime now = DateTime.UtcNow;
            tbFilePath.Text = string.Concat("c:/temp/sim", now.ToString(@"_yyyy-MM-dd-HH-mm-ss-FF", culture), ".csv");

            this.treeView2.NodeMouseClick +=
                new TreeNodeMouseClickEventHandler(this.treeView2_NodeMouseClick);

            PopulateTreeView();

            lbStatus.Text = "Ready ...";


        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        public void CLADAQ_Shown(object sender, EventArgs e)
        {
            var autoEvent = new AutoResetEvent(false);

            Thread trd0 = Global.trd[0] = Thread.CurrentThread;             // GUI
            Thread trd1 = Global.trd[1] = new Thread(Task_1_DoWork);        // Simulataion
            Thread trd2 = Global.trd[2] = new Thread(Task_2_DoWork);        // Acquisition
            Thread trd3 = Global.trd[3] = new Thread(Task_3_DoWork);

            trd0.Priority = ThreadPriority.BelowNormal;
            trd1.Priority = ThreadPriority.AboveNormal;
            trd2.Priority = ThreadPriority.AboveNormal;
            trd3.Priority = ThreadPriority.BelowNormal;

            trd1.Start();
            //trd2.Start();
            trd3.Start();

            Global.sw = new Stopwatch();
            Global.sw.Start();

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
            //DataBuffer datBuff = new DataBuffer();

            //if (this.cbSimulate.Checked)
            //{
            // Create a timer and set a 100 ms interval.
            //    var autoEvent = new AutoResetEvent(false);
            //    simTimer = new System.Threading.Timer(OnSimTimedEvent, autoEvent, 1000, 1000);
            //}

            Thread.Sleep(200);
        }


        private void Task_2_DoWork()                 //  acquisition thread
        {

            //this.buffer = dblPosBuff[intBuffS - 1];
            while (true)
            {
                if (PLC_Con.IsConnected && blPLCRunning)
                {
                    //FormTools.AppendText(this, tbLog, "> " + "Start reading axis position" + Environment.NewLine);
                    ////try
                    ////{
                    ////    dwHmiBitStatus_gb = PLC_Con.Logic.ReadVariableBySymbol("Application.HmiVarGlobal.dwHmiBitStatus_gb");
                    ////    dblAxisPos[15] = PLC_Con.Motion.Axes[15].GetActualPosition();
                    ////    dblAxisPos[16] = PLC_Con.Motion.Axes[16].GetActualPosition();
                    ////dblPosBuff[0, 0] = dblAxisPos[15];
                    ////dblPosBuff[0, 1] = dblAxisPos[16];

                    //dblCh1 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf1_Sent_gb");
                    //dblCh2 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf2_Sent_gb");
                    //dblCh3 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf3_Sent_gb");
                    //dblCh4 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf4_Sent_gb");
                    //dblCh5 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf5_Sent_gb");
                    //dblCh6 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf6_Sent_gb");
                    //uiTime = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.tTimeBufSent_gb");

                    //for (int i = 0; i < intBuffS; i += 1)
                    //{
                    //    //dblPosBuff[i, 0] = test[i];
                    //    //dblPosBuff[i, 1] = test1[i];
                    //    intTimeBuff[i] = uiTime[i] ;
                    //    strTimeBuff[i] = uiTime[i].ToString();
                    //}


                    //    FormTools.AppendText(this, tbLog, "> " + "Done reading axis position" + Environment.NewLine);
                    ////}
                    ////catch (Exception ex)
                    ////{
                    ////    FormTools.AppendText(this, tbLog, "> " + "Cannot read positions. Check connection." + Environment.NewLine);
                    ////}

                    //string specifier = "F";

                    ////FormTools.SetText(this, lb_Pos_As1, dblAxisPos[15].ToString(specifier));
                    ////FormTools.SetText(this, lb_Pos_As2, dblAxisPos[16].ToString(specifier));

                    ////FormTools.SetText(this, lb_DiagText_As1, PLC_Con.Motion.Axes[15].GetDiagnosisText());
                    ////FormTools.SetText(this, lb_DiagText_As2, PLC_Con.Motion.Axes[16].GetDiagnosisText());

                    this.OnValueChanged(null);

                }

                Thread.Sleep(50);

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
            TimeSpan t1 = Global.sw.Elapsed;
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


        private void Task3()
        {
            while (false)
            {
                //   string msg;
                //   Thread thread = Thread.CurrentThread;
                //   lock (lockObj)
                //   {
                //       msg = String.Format("   Thread ID: {0}\n", thread.ManagedThreadId);
                //   }
                //   TimeSpan t1 = Global.sw.Elapsed;
                //   FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"HH\:mm\:ss\.") + "Task 3 by " + msg + Environment.NewLine);
                //   Thread.Sleep(1000);
            }
        }

        public void CLADAQ_FormClosing(object sender, FormClosingEventArgs e)
        {
            //aTimer.Dispose();
            try
            {
                simTimer.Dispose();
                //csv.Flush();
                //csv.Dispose();

            } catch (Exception ex)
            { }


            Global.trd[1].Abort();
            Global.trd[2].Abort();
            Global.trd[3].Abort();

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


            //ReadWrite(2, "Application.HmiVarGlobal.dwHmiBitControl_gb", "1");
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

            dispTimer.Stop();

            //lb_Pos_As1.Text = dblAxisPos[15].ToString();
            //lb_Pos_As1.Text = dblAxisPos[16].ToString();

            //lb_Pos_As1.Text = dblPosBuff[intIdxEnd - 1, 0].ToString();
            //lb_Pos_As2.Text = dblPosBuff[intIdxEnd - 1, 1].ToString();

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
                    chart1.Series[0].Points.AddXY(dp.XValue + 1, dblCh5[i] * 6 / 1000); //dblCh1
                    chart1.Series[1].Points.AddXY(dp.XValue + 1, dblCh6[i]); //dblCh2

                    //chart2.Series[0].Points.AddXY(dblCh1[i], dblCh2[i]);
                    chart2.Series[0].Points.AddXY(dblCh1[i], dblCh2[i]); //dblCh1
                    //chart2.Series[1].Points.AddXY(dp.XValue + 1, dblCh2[i]); //dblCh2
                }
            }
            if (strTimeBuff[1] != null)
            {
                TimeSpan t1 = Global.sw.Elapsed;
                FormTools.AppendText(mainForm, tbLog, "> " + t1.ToString(@"ss\:fffffff\.") + " : Time buffer returns zero. " + Environment.NewLine);
            }


            while (chart1.Series[0].Points.Count > intPlotS)
            {
                chart1.Series[0].Points.RemoveAt(0);
                chart1.Series[1].Points.RemoveAt(0);
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

                chart2.ChartAreas[0].AxisY.Maximum = 850;
                chart2.ChartAreas[0].AxisY.Minimum = 650;
                chart2.ChartAreas[0].AxisX.Maximum = 500;
                chart2.ChartAreas[0].AxisX.Minimum = 300;
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

            if (bDebugLog)
            {
                Thread thread = Thread.CurrentThread;
                lock (lockObj)
                {
                    TimeSpan t1 = Global.sw.Elapsed;
                    string msg = String.Format(t1.ToString(@"ss\:fffffff\.") + " Disp event from Thread ID: {0}\n", thread.ManagedThreadId);
                    FormTools.AppendText(mainForm, tbLog, "> " + msg + Environment.NewLine);
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

        //}
        //public class DataBuffer : CLADAQ
        //    {

        public double buffer = new double();

        public CLADAQ mainForm;


        public void OnSimTimedEvent(Object source, EventArgs myEventArgs)         // Simulation timer
        {
            DateTime now = new DateTime();
            double nowS = new double();
            nowS = now.Second + Convert.ToDouble(now.Millisecond / 1000.0);

            for (int i = 0; i < intBuffS; i++)
            {
                now = DateTime.Now;
                strDateBuff[i] = now.ToString(@"yyyy-MM-dd");
                strTimeBuff[i] = now.ToString(@"HH\:mm\:ss\.FFFFFF");

                nowS = now.Second + Convert.ToDouble(i) * 0.001;
                //nowS += i*0.01;

                for (int ii = 0; ii < intAcqChans; ii++)
                {
                    dblPosBuff[i, ii] = 5 * Math.Sin(0.1 * (nowS) * 6.28 + 0.4 * ii);
                    //dblPosBuff[i, ii] = (nowS + i + ii)/166 - 0.5;
                }
                dblCh1[i] = dblPosBuff[i, 0];
                dblCh2[i] = dblPosBuff[i, 1];
                dblCh3[i] = dblPosBuff[i, 2];
                dblCh4[i] = dblPosBuff[i, 3];
                dblCh5[i] = dblPosBuff[i, 4];
                dblCh6[i] = dblPosBuff[i, 5];

            }


            this.OnValueChanged(null);      //trigger buffer event if acq buffer full

            double dblPosEnd = dblPosBuff[intIdxEnd - 1, intAcqChans - 1];

            Thread thread = Thread.CurrentThread;

            if (bDebugLog)
            {
                TimeSpan t1 = Global.sw.Elapsed;
                string msg = String.Format(t1.ToString(@"ss\:fffffff\.") + " Sim event from Thread ID: {0}\n", thread.ManagedThreadId);
                FormTools.AppendText(mainForm, tbLog, "> " + msg + Environment.NewLine);
            }


            //}
            //catch (Exception ex)
            //{ }
        }


        // not in use any more
        protected virtual void OnAcquireTimedEvent(Object source, EventArgs myEventArgs)          // Acquisition timer
        {
            if (PLC_Con.IsConnected && !bSimValues) //&& blPLCRunning
            {

                TimeSpan t1 = Global.sw.Elapsed;
                //FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Polling time buffer" + Environment.NewLine);


                uiTime = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.tTimeBufSent_gb");//tTimeBuf1_gb

                if ((uiTime.SequenceEqual(intTimeBuffOld)) == false)
                {
                    t1 = Global.sw.Elapsed;
                    FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + "Reading new axis position" + Environment.NewLine);
                    dblCh1 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf1_Sent_gb");
                    dblCh2 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf2_Sent_gb");
                    dblCh3 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf3_Sent_gb");
                    dblCh4 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf4_Sent_gb");
                    dblCh5 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf5_Sent_gb");
                    dblCh6 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf6_Sent_gb");
                    //dblCh7 = PLC_Con.Logic.ReadVariableBySymbol("Application.PlcVarGlobal.reBuf7_Sent_gb");

                    for (int i = 0; i < intBuffS; i += 1)
                    {
                        //dblPosBuff[i, 0] = test[i];
                        //dblPosBuff[i, 1] = test1[i];
                        intTimeBuff[i] = uiTime[i];
                        strTimeBuff[i] = uiTime[i].ToString();
                    }
                    intTimeBuffOld = uiTime;
                    this.OnValueChanged(null);

                    ulong dummy = intTimeBuff[1];
                }


                //FormTools.AppendText(this, tbLog, "> " + "Done reading axis position" + Environment.NewLine);
                //}
                //catch (Exception ex)
                //{
                //    FormTools.AppendText(this, tbLog, "> " + "Cannot read positions. Check connection." + Environment.NewLine);
                //}

                string specifier = "F";

                //FormTools.SetText(this, lb_Pos_As1, dblAxisPos[15].ToString(specifier));
                //FormTools.SetText(this, lb_Pos_As2, dblAxisPos[16].ToString(specifier));

                //FormTools.SetText(this, lb_DiagText_As1, PLC_Con.Motion.Axes[15].GetDiagnosisText());
                //FormTools.SetText(this, lb_DiagText_As2, PLC_Con.Motion.Axes[16].GetDiagnosisText());



            }

            if (bClientMTXConnected)
            {
                try
                {
                    var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActNcBlock,01");
                    lblOPCVal1.Text = dummy.ToString();

                    var dum2 = clientMTX.ReadNode("ns=27;NC.Chan.ActCallChain,01,BlockN");
                    lblOPCVal2.Text = dum2.ToString();
                }
                catch(Exception ex)
                { }
            }
                //try
                //{
                //Thread thread = Thread.CurrentThread;
                //string msg = String.Format("Thread ID: {0}\n", thread.ManagedThreadId);

                //aTimer.Stop();
                //listAcqBuffer.AddRange(dblPosBuff);
                //intAcqBuffPos = intAcqBuffPos + intBuffS;

                //TimeSpan t1 = Global.sw.Elapsed;
                //FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + " : Acquired timed buffer. \n" + Environment.NewLine);
                //}
                //catch (Exception ex)
                //{ }
            }

        [Category("Action")]
        [Description("Trigger acquisition when buffer value is changed")]
        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(EventArgs e)
        {
            //EventHandler handler = triggerAcquisition;
            // (2)
            // Raise the event
            if (ValueChanged != null)
                ValueChanged(this, e);
            //handler?.Invoke(this, e);
        }

        protected void UpdateBuffer(object sender, EventArgs e)
        {
            for (int i = 0; i < intBuffS; i++)
            {
                //listAcqBuffer[b].Add(new dataRecord { PosX = dblPosBuff[i, 0], PosY = dblPosBuff[i, 1], PosZ = dblPosBuff[i, 2], PosB = dblPosBuff[i, 3], PosC = dblPosBuff[i, 4] , DataDate = strDateBuff[i],
                //    DataTime = strTimeBuff[i]}  );
                if (bSimValues)
                {
                    listAcqBuffer[b].Add(new dataRecord
                    {
                        PosX = dblPosBuff[i, 1],
                        PosY = dblPosBuff[i, 2],
                        PosZ = dblPosBuff[i, 3],
                        PosB = dblPosBuff[i, 4],
                        PosC = dblPosBuff[i, 5],
                        VelCmd = 0,
                        LaserPcmd = 1,
                        LaserPfdbck = 2,
                        DataDate = strDateBuff[i],
                        DataTime = strTimeBuff[i],
                        FlowWatch = 0
                    }); ;
                }
                else
                {
                    listAcqBuffer[b].Add(new dataRecord
                    {
                        PosX = dblCh1[i],
                        PosY = dblCh2[i],
                        PosZ = dblCh3[i],
                        PosB = 0,
                        PosC = 0,
                        VelCmd = dblCh4[i],
                        LaserPcmd = dblCh5[i],
                        LaserPfdbck = dblCh6[i],
                        DataDate = strDateBuff[i],
                        DataTime = strTimeBuff[i],
                        FlowWatch = dblCh7[i]
                    });
                }

            }

            intAcqBuffPos = intAcqBuffPos + intBuffS;

            int intLastInd = listAcqBuffer[b].Count;
            dataRecord drLastDR = listAcqBuffer[b].ElementAt(intLastInd - 1);
            double dblLastVal = drLastDR.LaserPfdbck;


            TimeSpan t1 = Global.sw.Elapsed;

            if (bDebugLog)
            {
                Thread thread = Thread.CurrentThread;
                lock (lockObj)
                {
                    string msg = String.Format(" : Acquired buffer from event in thread ", thread.ManagedThreadId);
                    FormTools.AppendText(mainForm, tbLog, "> " + msg + Environment.NewLine);
                }
                // + "    Laser fdbck = " + dblLastVal.ToString()
            }

            if (listAcqBuffer[b].Count >= (intAcqS))       // write to CSV if CSV buffer is full
            {

                lock (listAcqBuffer[b]) ;
                var b_old = b;
                b = b + 1;      //select new buffer for acquisition
                if (b > intNumBuffs - 1)
                    b = 0;

                if (bDebugLog)
                {
                    FormTools.AppendText(this, tbLog, "> Using buffer :" + b.ToString() + Environment.NewLine);
                }

                var records = new List<dataRecord>();

                foreach (dataRecord dR in listAcqBuffer[b_old].ToList())
                {
                    Idcount = Idcount + 1;
                    DateTime localDate = DateTime.Now;
                    dR.PrintDate = localDate.ToString(@"yyyy-MM-dd", culture);
                    dR.PrintTime = localDate.ToString(@"HH\:mm\:ss\.FFFFFF", culture);
                    records.Add(dR);
                }


                if (bPlotOn)
                {
                    int[] intDispCount = new int[intAcqS];
                    double dummy2 = new double();

                    for (int i = 0; i < intAcqS; i += 1)
                    {
                        intDispCount[i] = intDispCounter + 1;
                        intDispCounter = intDispCount[i];
                        List<dataRecord> dummy = listAcqBuffer[b_old].GetRange(i, 1);

                        dummy2 = dummy.Select(r => r.PosX).ToArray()[0];
                        chart1.Series[0].Points.AddXY(intDispCounter, dummy2);
                        dummy2 = dummy.Select(r => r.PosY).ToArray()[0];
                        chart1.Series[1].Points.AddXY(intDispCounter, dummy2);
                    }
                }




                if (writeCSV == true)
                {
                    lock (records) csv.WriteRecords(records);
                    csv.Flush();

                    t1 = Global.sw.Elapsed;
                    FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FFFFFF\.") + " :" + " Done writing buffer to CSV." + Environment.NewLine);

                    listAcqBuffer[b_old].RemoveRange(0, intAcqS);
                }
            }
        }

        protected class dataRecord
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

        public static class FormTools
        {
            delegate void SetTextCallback(Form f, Control ctrl, string text);
            delegate void AppendTextCallback(Form f, TextBox ctrl, string text); //only valid for textbox
                                                                                 /// <summary>
                                                                                 /// Set text property of various controls
                                                                                 /// </summary>
                                                                                 /// <param name="form">The calling form</param>
                                                                                 /// <param name="ctrl"></param>
                                                                                 /// <param name="text"></param>
            public static void SetText(Form form, Control ctrl, string text)
            {
                // InvokeRequired required compares the thread ID of the 
                // calling thread to the thread ID of the creating thread. 
                // If these threads are different, it returns true. 
                if (ctrl.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    form.Invoke(d, new object[] { form, ctrl, text });
                }
                else
                {
                    ctrl.Text = text;
                }
            }
            public static void AppendText(Form form, TextBox ctrl, string text)
            {
                // InvokeRequired required compares the thread ID of the 
                // calling thread to the thread ID of the creating thread. 
                // If these threads are different, it returns true. 

                if (ctrl.InvokeRequired)
                {
                    AppendTextCallback d = new AppendTextCallback(AppendText);
                    form.Invoke(d, new object[] { form, ctrl, text });
                }
                else
                {
                    ctrl.AppendText(text);
                }
            }
        }

        private void cbSimulate_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSimulate.Checked == false)
            {
                simTimer.Stop();
                simTimer.Dispose();
                dispTimer.Stop();

            }
            else
            {
                cbAcquisition.Checked = false;
                var autoEvent = new AutoResetEvent(false);

                // program wait for buffer to be filled
                simTimer.Start();
                dispTimer.Start();
            }
        }

        private void cbAcquisition_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAcquisition.Checked == true)
            {
                // Start the timer
                cbSimulate.Checked = false;
                acqTimer.Start();
                dispTimer.Start();

                //chart1.Series[0].Points.Clear();
                //chart1.Series[1].Points.Clear();
                //chart2.Series[0].Points.Clear();



            }
            else
            {
                acqTimer.Stop();
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
                writer = new StreamWriter(strFilePath, true);
                writer.AutoFlush = true;
                csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                writeCSV = true;
                FormTools.AppendText(this, tbLog, ">  CSV stream sucesfully opened on file: " + strFilePath + Environment.NewLine);
            }
            else
            {

                csv.Flush();
                try
                {
                    csv.Dispose();
                    writer.Dispose();
                    FormTools.AppendText(this, tbLog, ">  CSV stream sucesfully closed." + Environment.NewLine);

                }
                catch (Exception)
                {
                    FormTools.AppendText(this, tbLog, ">  Error on closing CSV stream:" + Environment.NewLine);
                    throw;
                }

                writeCSV = false;
            }
        }

        private void PopulateTreeView()
        {
            TreeNode rootNode;



            //DirectoryInfo info = new DirectoryInfo(@"../..");
            DirectoryInfo info = new DirectoryInfo(@"E:\data-cladaq\");
            if (info.Exists)
            {
                rootNode = new TreeNode(info.Name);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                treeView2.Nodes.Add(rootNode);
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                    aNode.ImageIndex = 0;
                }
                else
                { aNode.ImageIndex = 1; }
                nodeToAddTo.Nodes.Add(aNode);
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


            try
            {
                clientMTX.Connect();
                lblOPCStatus.Text = "Connected to MiCLAD ";
                bClientMTXConnected = true;

                //var temperature = clientMTX.ReadNode("ns=2;s=Temperature");
                //Console.WriteLine("Current Temperature is {0} °C", temperature);
            }
            catch (Exception ex)
            {
                lblOPCStatus.Text = "Error connecting to MiCLAD";
                FormTools.AppendText(this, tbLog, "> Could not connect to MiCLAD MTX." + Environment.NewLine);
                FormTools.AppendText(this, tbLog, "> Error message:" + ex.ToString() + Environment.NewLine);
            }

        }

       

    }
}