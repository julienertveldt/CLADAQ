using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CsvHelper; 
using System.IO; //StreamWriter
using System.Globalization; //CultureInfo
using CLADAQ;
using System.Threading.Tasks; //Async tasks
using System.Web.UI;

public class DAQBuffer
    {
        
    //public fields

    public string CsvPath { get; set; }
    public static bool bWriting { get; set; }
    //public bool bWriteFile { get; set; }
    //public int intNumBuffs { get; set; }            //number of buffers to use cyclically
    public static int intAcqBuffPos { get; set; }
    public static int intLastInd { get; set; }
    public static int intAcqS { get; set; }                //Maximum of samples in buffer before writing to file

    //private fields
    private static List<DataRecord>[] listAcqBuffer;                 // data buffer to write
    //private List<string> csvString;                           // CSV string to write (replaced by DataRecord)

    public static int intBuffs;
    
    private static bool writeCSV;
    
    private static List<int> list_b_old = new List<int>(); //old buffer index

    private static int b;          //buffer index

    private static string lasttime;

    private static CsvWriter csv;
    private static StreamWriter writer;
    private static List<DataRecord> records;           // for CSV writer
    private static List<DataRecord> listAcqReturn;     // for UI interaction

    private static object objLock = new object();

    private static Thread thread = new Thread(new ThreadStart(ThreadWriteRecords));

    // Constructor
    public DAQBuffer()
    : this(5,1000)
    { }

    public DAQBuffer(int buffs, int buffSize)
    {
        intBuffs = buffs;
        intAcqS = buffSize;

        // list of buffers that contain a list of data records -> slow?
        listAcqBuffer = new List<DataRecord>[intBuffs];
        for (int i = 0; i < intBuffs; i++)
        {
            listAcqBuffer[i] = new List<DataRecord>();
        }

        thread.Priority = ThreadPriority.Highest;
        thread.Start(); //ToDo start,stop, and so on ...
    }

    // Public methods

    public int intBuffS
    {
        get
        {
            return intBuffs;
        }
    }
    public int StartWriter(string strFilePath)
    {
        int res = 0;
        writer = new StreamWriter(strFilePath, true);
        writer.AutoFlush = true;
        csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        writeCSV = true;

        res = 1;

        return res;
    }

    public int CloseWriter()
    {
        csv.Flush();
        try
        {
            csv.Dispose();
            writer.Close();
            writer.Dispose();
            
            return 1;
        }
        catch (Exception)
        {

            return 0; 
            throw;
            
        }
    }

    //public async Task<int> AppendToBuffer(double[,] dblAcqCh, UInt64[] TimeBuff)
    //public int AppendToBuffer(double[,] dblAcqCh, UInt64[] TimeBuff)
    public int AppendToBuffer(List<double[]> dblAcqCh, UInt64[] TimeBuff)
    {
        int b_old; //old buffer index  

        int intBuffS = dblAcqCh[0].Length; // all channels have same length, we take the 1st one
        int NChan = dblAcqCh.Count;
        double FlowWatchTemp;

        for (int i = 0; i < intBuffS; i++)
        {
            if (NChan > 6)
            {
                FlowWatchTemp = dblAcqCh[6][i];
            }
            else
            {
                FlowWatchTemp = -1;
            }

            DateTime localDate = DateTime.Now;

            listAcqBuffer[b].Add(new DataRecord
            {
                PosX = dblAcqCh[0][i],
                PosY = dblAcqCh[1][i],
                PosZ = dblAcqCh[2][i],
                PosB = 0,
                PosC = 0,
                VelCmd = dblAcqCh[3][i],
                LaserPcmd = dblAcqCh[4][i],
                LaserPfdbck = dblAcqCh[5][i],
                DataTime = TimeBuff[i].ToString(),
                FlowWatch = FlowWatchTemp,                
                PrintDate = localDate.ToString(@"yyyy-MM-dd", new CultureInfo("EN-US")),
                PrintTime = localDate.ToString(@"HH\:mm\:ss\.FFFFFF", new CultureInfo("EN-US"))
            });
        }

        intAcqBuffPos = intAcqBuffPos + intBuffS;
        intLastInd = listAcqBuffer[b].Count;

        if (listAcqBuffer[b].Count >= (intAcqS))       // write to CSV if CSV buffer is full
        {
            b_old = b;
            b = b + 1;      //select new buffer for acquisition
            if (b > intBuffs - 1)
                b = 0;
            list_b_old.Add(b_old);    
        }
            
        return 1;
    }


    // Private methods
    private static int WriteBuffer(List<DataRecord> records)
    {
        int intDone;
        if (writer != null)
        {
            if (csv != null)
            {
                csv.WriteRecords(records);
                csv.Flush();
            }

            intDone = 1;
        }
        else
        {
            intDone = 0; // no writer configured
        }
        return intDone;
    }

    private static void ThreadWriteRecords()
    {
        int b_old; //old buffer index  

        while (true)
        {
            if (list_b_old.Count > 0)
            {
                b_old = list_b_old.ElementAt(0);

                listAcqReturn = new List<DataRecord>(listAcqBuffer[b_old]);//ToDo, change and protect, right now ignoring this line

                if (writeCSV) //if write to CSV make copy of buffer
                {
                    bWriting = true;

                    records = listAcqBuffer[b_old];
                 
                    int success = 0;
                    success = WriteBuffer(records);
                    if (success > 0)
                    {
                        bWriting = false;
                    }

                }
                //listAcqBuffer[b_old].RemoveRange(0, intAcqS);
                listAcqBuffer[b_old].Clear();

                list_b_old.RemoveAt(0);
            }
        }
    }


    public static List<DataRecord> GetLastDataRecords()
    {
        //returns the last values that was added to the buffer cycling through the cyclic buffers.

        if (listAcqReturn != null)
        {
            if (listAcqReturn[1].DataTime != lasttime)
            {
                // only output list if buffer updated
                lasttime = listAcqReturn[1].DataTime;
                return listAcqReturn;
            }
            else
            {
                return null;
            }
        }
        else
        { return null; }


    }

}

