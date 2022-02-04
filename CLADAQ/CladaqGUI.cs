using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLADAQ
{
    public partial class CLADAQgui 
    {
        public class cpBar : CircularProgressBar.CircularProgressBar
        {
            public cpBar()
                : base()
            {
                AnimationFunction = WinFormAnimation.KnownAnimationFunctions.Liner;
                AnimationSpeed = 500;
                BackColor = System.Drawing.Color.Transparent;
                Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                InnerColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
                InnerMargin = 2;
                InnerWidth = -1;
                Location = new System.Drawing.Point(262, 311);
                MarqueeAnimationSpeed = 2000;
                Name = "cpBar9";
                OuterColor = System.Drawing.Color.Gray;
                OuterMargin = -25;
                OuterWidth = 26;
                ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
                ProgressWidth = 15;
                SecondaryFont = new System.Drawing.Font("Microsoft Sans Serif", 36F);
                Size = new System.Drawing.Size(100, 100);
                StartAngle = 0;
                SubscriptColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(166)))), ((int)(((byte)(166)))));
                SubscriptMargin = new System.Windows.Forms.Padding(10, -35, 0, 0);
                SubscriptText = ".23";
                SuperscriptColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(166)))), ((int)(((byte)(166)))));
                SuperscriptMargin = new System.Windows.Forms.Padding(10, 35, 0, 0);
                SuperscriptText = "°C";
                TabIndex = 9;
                Text = "circularProgressBar3";
                TextMargin = new System.Windows.Forms.Padding(8, 8, 0, 0);
                Value = 68;
            }
        }
    }
}
