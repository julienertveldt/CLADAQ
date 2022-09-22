using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CLADAQ
{
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
}
