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
//using commlpiLib;
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
        protected static object lockObj = new Object();                     // for threading
        public static CultureInfo culture = new CultureInfo("EN-US");
  
        public static bool bDebugLog = false;
        protected static bool bSimValues = false;

        private static int intBuffS;// = Global.intBuffS;                   // position value buffer sent by PLC: same as in GlobVarContant XM22
        private static uint intNCh; // = Global.intNCh;
        private static int intAcqS;// = Global.intAcqS;                     // acquisition buffer to be written to file

        private static uint intAcqDelay; // = Global.intAcqDelay;            // ms delay for acquisition timer
        private static int intIdxEnd;// = intBuffS;                         // number of values in pos data buffer
        private static int intNumBuffs;// = Global.intNumBuffs;             // number of (cyclic) buffers to use.
        private static int intPlotS;// = Global.intPlotS;                   // number of points to plot
        private static int intPlotSkip;// = Global.intPlotSkip;             // plot 1 out of X samples

        private static System.Windows.Forms.Timer dispTimer;                // Display refresh timer

        private static string strFilePath = "C:/temp/test.csv";     

        private static OpcClient clientMTX = new OpcClient("opc.tcp://192.168.142.250:4840/");

        public static DAQBuffer daqBuff;
        public static DAQ daqAcq;
        public static Simulator simData;

        // Display GUI static fields
        private static int intDispDelay; //= Global.intDispDelay;                        // ms delay for screen refresh

        // ===================================
        // Variables

        // for read-write task in PLC not used at the moment
        private int job_Nr;                                         // threading task job (read, write)
        private object Par = new object();
        private object Data = new object();

        private bool writeCSV = false;                              // run acquisition of data without writing at launch
        private bool blPLCRunning = false;
        private bool bClientMTXConnected = false;

        // plot options
        protected bool bPlotOn = false;
        private bool bAutoSize = true;
        private double dblYMax = 10;
        private double dblYMin = 0;

        private DataPoint dp = new DataPoint();

        public CLADAQ()
        {
            CladaqLib.Global.InitializeApp();

            // Central buffer & Acquisition
            intBuffS = Global.intBuffS;                // position value buffer sent by PLC: same as in GlobVarContant XM22
            intAcqS = Global.intAcqS;
            intAcqDelay = Global.intAcqDelay;            
            intNumBuffs = Global.intNumBuffs;
            intNCh = Global.intNCh;


            // Plotting & GUI
            intPlotS = Global.intPlotS;                
            intPlotSkip = Global.intPlotSkip;
            intDispDelay = Global.intDispDelay;
            
            InitializeComponent();                      // Standard WinForms method for building Form
        }

        public class GlobalUI
        {
            public static Thread[] trd = new Thread[4];
            public static Stopwatch sw = new Stopwatch();
        }

        private void CLADAQ_Load(object sender, EventArgs e)
        {
            lbStatus.Text = "Loading GUI content ...";

        // Create a display timer and set an interval.
            dispTimer = new System.Windows.Forms.Timer();
            dispTimer.Interval = intDispDelay;
            dispTimer.Tick += new EventHandler(this.OnDispTimedEvent);
         
        // UI initialization
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

            chart1.ChartAreas[0].AxisX.Interval = 25; // Intervals of 1/10 samples plotted

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

                  
            if (bSimValues)
                intIdxEnd = intBuffS;

           
            DateTime now = DateTime.UtcNow;
            tbFilePath.Text = string.Concat("c:/temp/sim", now.ToString(@"_yyyy-MM-dd-HH-mm-ss-FF", culture), ".csv");

            this.treeView2.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView2_NodeMouseClick);

            Browser.PopulateTreeView(this.treeView2);

            daqBuff = new DAQBuffer(intNumBuffs, intAcqS);                      // buffer for CSV writing

            daqAcq = new DAQ(daqBuff, intBuffS, intNCh, Global.intAcqDelay);    //acquisition buffer 

            simData = new Simulator(daqBuff);
            simData.dblSimDelay = 20;

            lbStatus.Text = "Ready ...";
           
        }


        public void CLADAQ_Shown(object sender, EventArgs e)
        {
            var autoEvent = new AutoResetEvent(false);

            //Thread trd0 = GlobalUI.trd[0] = Thread.CurrentThread;             // GUI
            //Thread trd1 = GlobalUI.trd[1] = new Thread(Task_1_DoWork);        // Acquisition
            //Thread trd2 = GlobalUI.trd[2] = new Thread(Task_2_DoWork);        // Buffer writer
            //Thread trd3 = GlobalUI.trd[3] = new Thread(Task_3_DoWork);

            //trd0.Priority = ThreadPriority.BelowNormal;
            //trd1.Priority = ThreadPriority.AboveNormal;
            //trd2.Priority = ThreadPriority.AboveNormal;
            //trd3.Priority = ThreadPriority.BelowNormal;

            //trd1.Start();
            //trd2.Start();
            //trd3.Start();
            

            GlobalUI.sw = new Stopwatch();
            GlobalUI.sw.Start();

            Thread thread = Thread.CurrentThread;

            if (bDebugLog)
            {
                string msg = String.Format("Thread ID of GUI: {0} " + Environment.NewLine, thread.ManagedThreadId);
                FormTools.AppendText(this, tbLog, ">  " + msg + Environment.NewLine);
            }

        }

        private void Task_1_DoWork()                 // acquisition thread
        {

        }

        private void Task_2_DoWork()                 //  writing buffer thread
        {

        }

        private void Task_3_DoWork()                // simulator thread
        {
            
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
        }

        public void CLADAQ_FormClosing(object sender, FormClosingEventArgs e)
        {
            //aTimer.Dispose();
            try
            {
                simData.Close();
                daqAcq.Close();

                if (clientMTX != null)
                {
                    clientMTX.Disconnect();
                }
                //csv.Flush();
                //csv.Dispose();

            } catch (Exception ex)
            { }


            //GlobalUI.trd[1].Abort();
            //GlobalUI.trd[2].Abort();
            //GlobalUI.trd[3].Abort();

        }

        private void bt_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                bool blConnect = daqAcq.Connect(tb_Ip_Address.Text);

                if (blConnect)
                {
                    FormTools.AppendText(this, tbLog, "> " + "Connected" + Environment.NewLine);
                    lbStatus.Text = "Connected";
                    tb_Ip_Address.BackColor = Color.Lime;
                }
                else
                {
                    tb_Ip_Address.BackColor = Color.Red;
                    lbStatus.Text = "Error on connecting";

                    FormTools.AppendText(this, tbLog, "> " + "Error connecting:" + Environment.NewLine);
                }
                
            }
            catch (Exception ex)
            {
                throw;
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

                daqAcq.Disconnect();
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
            dispTimer.Stop();
            

            List<DataRecord> drDisp = daqBuff.GetLastDataRecords();

            if (drDisp != null)
            {
                string time = drDisp[drDisp.Count-1].DataTime;
                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:fffffff\.") + " : Time of acq value. " + time.ToString() + Environment.NewLine);
            

            string[] strTimeBuff = { };

                for (int i = 0; i < intAcqS; i = i + 10)
                {
                    //if (strTimeBuff[i] != null)
                    {
                        if (chart1.Series[0].Points.Count == 0)
                        {
                            dp.XValue = 0;
                        }
                        else
                        {
                            dp = chart1.Series[0].Points.Last();
                        }

                        // Define plot channels

                        // Positions X & Y
                        //chart1.Series[0].Points.AddXY(dp.XValue + 1, drDisp[i].PosX); //dblCh1
                        //chart1.Series[1].Points.AddXY(dp.XValue + 1, drDisp[i].PosY); //dblCh2

                        // Vel & Power
                        chart1.Series[0].Points.AddXY(dp.XValue + 1, drDisp[i].VelCmd * 6 / 1000); //dblCh1 dblAcqCh[1-1,i] * 6 / 1000
                        chart1.Series[1].Points.AddXY(dp.XValue + 1, drDisp[i].LaserPcmd); //dblCh2

                        // Chart 2: XY plot
                        chart2.Series[0].Points.AddXY(drDisp[i].PosX, drDisp[i].PosY);

                    }
                }
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
                    this.ResizeChart();
                }
                catch (Exception exc)
                { }
            }
            else
            {

                chart1.ChartAreas[0].AxisY.Maximum = dblYMax;
                chart1.ChartAreas[0].AxisY.Minimum = dblYMin;

                chart2.ChartAreas[0].AxisY.Maximum = 1600;
                chart2.ChartAreas[0].AxisY.Minimum = 0;
                chart2.ChartAreas[0].AxisX.Maximum = 800;
                chart2.ChartAreas[0].AxisX.Minimum = 0;
            }

            try
            {
                // scrolling with X values (timer)
                dp = chart1.Series[0].Points.FindMaxByValue("X", 0);
                chart1.ChartAreas[0].AxisX.Maximum = Math.Round(dp.XValue);

                dp = chart1.Series[0].Points.FindMinByValue("X", 0);
                chart1.ChartAreas[0].AxisX.Minimum = Math.Round(dp.XValue);

                
            }
            catch (Exception exc)
            { }

            // OPCUA display --> To be moved in new DAQBuffer object

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
                    var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActCallChain,01,FilePosition"); // array
                    //var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActCallChain,01,BlockNo");
                    int[] intValues = (int[])dummy.Value;
                    lblOPCVal1.Text = intValues[0].ToString();
                    


                    var dum3 = clientMTX.ReadNode("ns=27;s=NC.Chan.ActNcBlock,01");
                    lblOPCVal2.Text = dum3.ToString();
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

            dispTimer.Start();

        }

        private void ResizeChart()
        {
            dp = chart1.Series[0].Points.FindMaxByValue("Y1", 0);
            chart1.ChartAreas[0].AxisY.Maximum = Math.Round(dp.YValues[0] * 1.1) + 1;

            dp = chart1.Series[1].Points.FindMinByValue("Y1", 0);
            chart1.ChartAreas[0].AxisY.Minimum = Math.Sign(Math.Round(dp.YValues[0]) * Math.Abs(dp.YValues[0])) * 1.1 - 1; //-0.001 quick fix to avoid Ymin = Ymax

            dp = chart2.Series[0].Points.FindMaxByValue("Y1", 0);
            chart2.ChartAreas[0].AxisY.Maximum = Math.Round(dp.YValues[0] * 1.1) + 1;

            dp = chart2.Series[0].Points.FindMinByValue("Y1", 0);
            chart2.ChartAreas[0].AxisY.Minimum = Math.Sign(Math.Round(dp.YValues[0]) * Math.Abs(dp.YValues[0])) * 1.1 - 1; //-0.001 quick fix to avoid Ymin = Ymax

            dp = chart2.Series[0].Points.FindMaxByValue("X", 0);
            chart2.ChartAreas[0].AxisX.Maximum = Math.Round(dp.XValue * 1.1) + 1;

            dp = chart2.Series[0].Points.FindMinByValue("X", 0);
            chart2.ChartAreas[0].AxisX.Minimum = Math.Sign(Math.Round(dp.XValue * Math.Abs(dp.XValue))) * 1.1 - 1;
        }

        private void cbSimulate_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSimulate.Checked == false)
            {
                simData.StopSimulator();
                dispTimer.Stop();
                bSimValues = false;

                TimeSpan t1 = GlobalUI.sw.Elapsed;
                FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FF\.") + " :" + " Stopped data simulation." + Environment.NewLine);

            }
            else
            {
                dispTimer.Start();
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
                cbSimulate.Checked = false;
                // Start the timer
                daqAcq.Start();
                              
                dispTimer.Start();

            }
            else
            {
                int intCount = 0;
                bool bDisp = true;
                while (daqBuff.bWriting && intCount < 500)
                {
                    if (bDisp)
                    {
                        TimeSpan t1 = GlobalUI.sw.Elapsed;
                        FormTools.AppendText(this, tbLog, "> " + t1.ToString(@"ss\:FF\.") + " :" + " Waiting to close buffer..." + Environment.NewLine);
                        bDisp = false;
                        intCount = intCount + 1;
                    }   
                    if ((intCount % 100)==0)
                    { bDisp = true; }
                    //Thread.Sleep(50);

                }
                if (daqBuff.bWriting == false) // wait for buffer to be written before closing.
                {
                    daqAcq.Stop();
                    dispTimer.Stop();
                }

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
                if (daqBuff.bWriting == false) // wait for buffer to be written before closing.
                {
                    int res = daqBuff.CloseWriter();
                    writeCSV = false;
                    FormTools.AppendText(this, tbLog, ">  CSV stream closed at: " + strFilePath + Environment.NewLine);
                }
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

        private void btnOPCConnect_Click(object sender, EventArgs e)
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



// Example do PLC task
// [remove here]
//if (PLC_Con.IsConnected)
//{
//    switch (job_Nr)
//    {
//        case 1:      //read variables

//            Diagnosis diagnosis = PLC_Con.System.GetDisplayedDiagnosis();
//            // add timestamp
//            string strFormattedOutput = "";
//            strFormattedOutput += "Time: "
//                + diagnosis.dateTime.day.ToString("00") + "."
//                + diagnosis.dateTime.month.ToString("00") + "."
//                + diagnosis.dateTime.year.ToString("00") + " - "
//                + diagnosis.dateTime.minute.ToString("00") + ":"
//                + diagnosis.dateTime.hour.ToString("00") + ":"
//                + diagnosis.dateTime.second.ToString("00") + System.Environment.NewLine;
//            // add current state
//            strFormattedOutput += "State: " + diagnosis.state.ToString() + System.Environment.NewLine;
//            // add dispatcher of diagnosis
//            strFormattedOutput += "Despatcher: " + diagnosis.despatcher.ToString() + System.Environment.NewLine;
//            // add error number
//            strFormattedOutput += "Number: 0x" + String.Format("{0:X}", diagnosis.number) + System.Environment.NewLine;
//            // add description
//            strFormattedOutput += "Text: " + diagnosis.text;
//            // print to console
//            //Console.WriteLine(strFormattedOutput);
//            FormTools.AppendText(this, tbLog, strFormattedOutput + Environment.NewLine);

//            break;

//        case 2:     //'Write variable - par = "Application.GVL_x.var_x"

//            try
//            {
//                string strPar = Convert.ToString(Par);
//                PLC_Con.Logic.WriteVariableBySymbol(strPar, Data);
//                FormTools.AppendText(this, tbLog, "> PLC motion started succesfull." + Environment.NewLine);
//            }
//            catch (Exception ex)
//            {
//                FormTools.AppendText(this, tbLog, "> Could not start motion on PLC." + Environment.NewLine);

//            }



//            break;
//    }

//}
//else
//{

//}