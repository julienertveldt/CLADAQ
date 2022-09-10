using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CLADAQ
{
    static class Program
    {

        

        ///<summary>
        /// Necessary for UI interfaces using COM components --> Single thread for UI
        [STAThread]
        ///</summary>
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);      
            Application.Run(new CLADAQ());
        }
    }   
}
