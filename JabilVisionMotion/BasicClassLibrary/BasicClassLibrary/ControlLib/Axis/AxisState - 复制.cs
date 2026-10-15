using System;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.Axis
{
    public partial class AxisState : UserControl
    {
        public AxisState()
        {
            InitializeComponent();
            #region 刷新
            tableLayoutPanel3.Delay = 5000;
            this.tableLayoutPanel3.TimerElapsed += MouseHover;
            this.tableLayoutPanel3.MouseEnter += new System.EventHandler(MouseEnter);
            this.tableLayoutPanel3.MouseLeave += new System.EventHandler(MouseLeave);
            #endregion
        }
        public bool Is_Use;
        private new void MouseHover(object o, EventArgs e)
        {
            Is_Use = false;
        }
        private new void MouseEnter(object sender, EventArgs e)
        {
            Is_Use = true;
        }
        private new void MouseLeave(object sender, EventArgs e)
        {
            Is_Use = false;
        }

    }
}
