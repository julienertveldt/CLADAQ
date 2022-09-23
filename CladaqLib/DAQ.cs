using commlpiLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Opc.UaFx;
using Opc.UaFx.Client;      // opcua.traeger.de

namespace CladaqLib
{
    public class DAQ : INotifyPropertyChanged
    {
        private MlpiConnection PLC_Con = new MlpiConnection();

        private static OpcClient clientMTX;

        protected double[,] dblAcqCh;
        protected UInt64[] uiTime;

        private int intBuffS;
        private uint intNCh;
        private uint intAcqDelay;

        private System.Timers.Timer timer;

        // define buffers comming from MLPI
        protected static double[,] dblPosBuff;            // CNC positions + laser channel
        public static string[] strTimeBuff;                        // CNC timestamp
        protected static UInt64[] intTimeBuff;
        protected static UInt64[] intTimeBuffOld;                     //previous cnc timestamp
        public static string[] strDateBuff;

        protected DAQBuffer daqBuff;

        private string _Type;

        public double dblAcqDelay { get; set; }
        public bool bRunning { get; set; }
        public string Type { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public DAQ(DAQBuffer daqBuffIn, int intBuffSIn, uint intNChIn, uint intAcqDelayIn, string strTypeIn)
        {
            intBuffS = intBuffSIn;
            intNCh = intNChIn;
            intAcqDelay = intAcqDelayIn;

            dblAcqCh = new double[intNCh, intBuffS];
            uiTime = new UInt64[intBuffS];

            // define buffers comming from MLPI
            dblPosBuff = new double[intBuffS, intNCh];            // CNC positions + laser channel
            strTimeBuff = new string[intBuffS];                        // CNC timestamp
            intTimeBuff = new UInt64[intBuffS];
            intTimeBuffOld = new UInt64[intBuffS];                     //previous cnc timestamp
            strDateBuff = new string[intBuffS];

            daqBuff = daqBuffIn;
            _Type = strTypeIn;

            if (intAcqDelay < 5)
            {
                intAcqDelay = 20;
            }

            timer = new System.Timers.Timer();
            timer.Interval = intAcqDelay;
            timer.AutoReset = true;
            timer.Enabled = false;
            
            if (_Type == "mlpi")
            {   timer.Elapsed += OnAquireMLPI;
            }else
                if (_Type == "MTX")
            { 
                timer.Elapsed += OnAquireMTX;
            }
            

        }

        protected void OnAquireMLPI(Object source, System.Timers.ElapsedEventArgs e)          // Acquisition timerµ
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
                
                if (timer != null && bRunning)
                {
                    timer.Start(); // restart

                }
            }

        }

        protected void OnAquireMTX(Object source, System.Timers.ElapsedEventArgs e)          // Acquisition timerµ
        {
            if (clientMTX.State > 0)
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
                    //cpBar19.Value = intCG;
                    //cpBar19.Text = intCG.ToString();
                    // string msg = String.Format("Current carrier gas setpoint is {0} L/min", intCG);
                    // FormTools.AppendText(this, tbLog, "> " + msg + Environment.NewLine);
                }

                try
                {
                    var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActCallChain,01,FilePosition"); // array
                    //var dummy = clientMTX.ReadNode("ns=27;s=NC.Chan.ActCallChain,01,BlockNo");
                    int[] intValues = (int[])dummy.Value;
                    //lblOPCVal1.Text = intValues[0].ToString();



                    var dum3 = clientMTX.ReadNode("ns=27;s=NC.Chan.ActNcBlock,01");
                    //lblOPCVal2.Text = dum3.ToString();
                }
                catch (Exception ex)
                { }
            }
        }
        public void Start()
        {

            timer.Enabled = true;

            if (timer.Enabled) { bRunning = true; }

        }

        public void Stop()
        {
            //timer.Enabled = false;
            timer.Dispose();
            bRunning = false;
        }

        public bool Connect(string strAdress)
        {
            if (_Type == "mlpi")
            {
                PLC_Con.Connect(strAdress + " -timeout_connect=500" + " -user=boschrexroth" + " -password=boschrexroth");
                return PLC_Con.IsConnected ;
            }
            else if (_Type == "MTX")
            {
                if (clientMTX == null)
                {
                    clientMTX = new OpcClient(strAdress);
                }
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
                } catch (Exception ex)
                {
                    return false;
                    throw;
                }

                return (clientMTX.State > 0);
            }
            else
            {
                return false;
            }
            
        }
        public bool IsConnected()
        {
            bool ret = false;   
            //checks if connection exists
            if (_Type == "mlpi")
            {
                if (PLC_Con != null)
                {
                    ret = PLC_Con.IsConnected;
                }
            }
            else if (_Type == "MTX")
            {
                if (clientMTX != null)
                {
                    ret =  (clientMTX.State>0) ;
                }
            }
            return ret;
        }

        public void Disconnect()
        {
            if (PLC_Con != null)
            {
                PLC_Con.Disconnect();
            }
            

            if (clientMTX != null)
            {
                clientMTX.Disconnect();
            }
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
