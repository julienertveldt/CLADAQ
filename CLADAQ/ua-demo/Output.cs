using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CLADAQ
{
    public interface IOutput 
    {
        void LinkTB(Form form, System.Windows.Forms.TextBox tb);
        void WriteLine(object obj);
        void WriteLine(string msg);
        void WriteLine(string msg, params object[] parameters);

    }

    public class ConsoleOutput : IOutput 
    {
        //public void WriteLine(object obj) => Console.WriteLine(obj);
        //public void WriteLine(string msg) => Console.WriteLine(msg);
        //public void WriteLine(string msg, params object[] parameters) => Console.WriteLine(msg, parameters);

        public void LinkTB(Form form, System.Windows.Forms.TextBox tb) {
            
        }

        public void WriteLine(object obj) => CLADAQ.FormTools.AppendText(CLADAQ.cladaq1, CLADAQ.cladaq1.tbLog, obj + Environment.NewLine);
        public void WriteLine(string msg) => CLADAQ.FormTools.AppendText(CLADAQ.cladaq1, CLADAQ.cladaq1.tbLog, msg + Environment.NewLine);
        public void WriteLine(string msg, params object[] parameters) => CLADAQ.FormTools.AppendText(CLADAQ.cladaq1, CLADAQ.cladaq1.tbLog, String.Format(msg,parameters) + Environment.NewLine);

    }

}
