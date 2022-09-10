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



public class DAQBuffer
    {
        
    //public fields

    public string CsvPath { get; set; }
    //public bool bWriteFile { get; set; }
    //public int intNumBuffs { get; set; }            //number of buffers to use cyclically
    public int intAcqBuffPos { get; set; }
    public int intLastInd { get; set; }
    public int intAcqS { get; set; }                //Maximum of samples in buffer before writing to file


    //private fields
    private List<DataRecord>[] listAcqBuffer;                 // data buffer to write
    //private List<string> csvString;                           // CSV string to write (replaced by DataRecord)

    private int intBuffs;
    private bool writeCSV;
    private int b_old;      //old buffer index


    private int b;          //buffer index


    private CsvWriter csv;
    private StreamWriter writer;
    private List<DataRecord> records;

    // Constructor
    public DAQBuffer()
    : this(2,1000)
    { }

    public DAQBuffer(int buffs, int buffSize)
    {
        intBuffs = buffs;
        intAcqS = buffSize;

        listAcqBuffer = new List<DataRecord>[intBuffs];
        for (int i = 0; i < intBuffs; i++)
        {
            listAcqBuffer[i] = new List<DataRecord>();
        }
    }

// Public methods
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
            writer.Dispose();
            return 1;
        }
        catch (Exception)
        {

            return 0; 
            throw;
            
        }
    }

    public async Task<int> AppendToBuffer(double[,] dblAcqCh, UInt64[] TimeBuff)
    {
        int intBuffS = dblAcqCh.GetLength(1);
        int NChan = dblAcqCh.GetLength(0);
        double FlowWatchTemp;
        DateTime now = DateTime.Today;

        for (int i = 0; i < intBuffS; i++)
        {
            if (NChan > 6)
            {
                FlowWatchTemp = dblAcqCh[6, i];
            }
            else
            {
                FlowWatchTemp = -1;
            }
            listAcqBuffer[b].Add(new DataRecord
            {
                PosX = dblAcqCh[0, i],
                PosY = dblAcqCh[1, i],
                PosZ = dblAcqCh[2, i],
                PosB = 0,
                PosC = 0,
                VelCmd = dblAcqCh[3, i],
                LaserPcmd = dblAcqCh[4, i],
                LaserPfdbck = dblAcqCh[5, i],
                DataTime = TimeBuff[i].ToString(),
                FlowWatch = FlowWatchTemp
            });
        }

        intAcqBuffPos = intAcqBuffPos + intBuffS;

        intLastInd = listAcqBuffer[b].Count;
        //DataRecord drLastDR = listAcqBuffer[b].ElementAt(intLastInd - 1);
        //double dblLastVal = drLastDR.LaserPfdbck;


        if (listAcqBuffer[b].Count >= (intAcqS))       // write to CSV if CSV buffer is full
        {

            lock (listAcqBuffer[b]) ;
            b_old = b;
            b = b + 1;      //select new buffer for acquisition
            if (b > intBuffs - 1)
                b = 0;

            if (writeCSV) //if write to CSV make copy of buffer
            {
                records = listAcqBuffer[b_old];

                int success = 0;
                
                success = WriteBuffer(records);

            }

            listAcqBuffer[b_old].RemoveRange(0, intAcqS);

        }

        return 1;
    }

// Private methods
    private int WriteBuffer(List<DataRecord> records)
    {
        if (writer != null)
        {
            int Idcount = 0;
            foreach (DataRecord dR in records.ToList())
            {
                Idcount = Idcount + 1;
                DateTime localDate = DateTime.Now;
                dR.PrintDate = localDate.ToString(@"yyyy-MM-dd", CLADAQ.CLADAQ.culture);
                dR.PrintTime = localDate.ToString(@"HH\:mm\:ss\.FFFFFF", CLADAQ.CLADAQ.culture);
                records.Add(dR);
            }

        lock (records) csv.WriteRecords(records);
            csv.Flush();

            return 1;
        }
        else
        {
            return 0; // no writer configured
        }

    }
 }

