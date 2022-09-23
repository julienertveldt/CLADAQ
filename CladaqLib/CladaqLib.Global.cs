using System;

namespace CladaqLib
{
    public static class Global
    {
        public static int intBuffS { get; private set; }                    // position value buffer sent by PLC: same as in GlobVarContant XM22                    
        public static int intAcqS { get; private set; }                     // ms delay for screen refresh
        public static uint intAcqDelay { get; private set; }                 // ms delay for acquisition timer
        public static int intIdxEnd { get; private set; }                   // number of values in pos data buffer
        public static int intNumBuffs { get; private set; }                 // number of (cyclic) buffers to use.
        public static int intSimFS { get; private set; }                    // Simulation frequency
        public static uint intNCh { get; private set; }
        public static int intDispDelay { get; private set; }
        public static int intPlotS { get; private set; }
        public static int intPlotSkip { get; private set; }
        public static uint intMTXDelay { get; private set; }                // ms refresh rate for MTX status

 
        public static string strMTXOPCIP { get; private set; }
        public static string strMLPIIP { get; private set; }        


        public static void InitializeApp()
        {
            //Buffer & Acquisition settings
            intBuffS = 100;                          // position value buffer sent by PLC: same as in GlobVarContant XM22
            intAcqS = intBuffS*10;                   // acquisition buffer to be written to file
            intAcqDelay = 20;                       // ms delay for acquisition timer
            intIdxEnd = intBuffS;                    // number of values in pos data buffer
            intNumBuffs = 20;                        // number of (cyclic) buffers to use.
            intSimFS = 500;                          // Simulation frequency
            intNCh = 6;
            intMTXDelay = 100;                      

            // GUI Settings;
            intDispDelay = 100;                         // ms delay for screen refresh
            intPlotS = 200;                           // number of points to plot
            intPlotSkip = 10;                          // plot 1 out of X samples

            // connection info;
            strMTXOPCIP = "opc.tcp://192.168.142.250:4840/";
            strMLPIIP = "192.168.142.3";
    }

    }


}
