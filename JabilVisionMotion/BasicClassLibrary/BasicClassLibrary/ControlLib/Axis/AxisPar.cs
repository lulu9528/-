using System;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.Axis
{
    public partial class AxisPar : UserControl
    {
        public AxisPar()
        {
            InitializeComponent();
        }

        private void checkBoxIsUse_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_IS_回零偏移距离.Checked)
            {
                chk_IS_回零偏移距离.Text = "启用";
            }
            else
            {
                chk_IS_回零偏移距离.Text = "禁用";
            }
        }

      
    }
}
