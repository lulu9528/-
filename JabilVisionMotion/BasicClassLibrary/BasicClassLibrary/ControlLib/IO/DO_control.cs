using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.IO
{
    public partial class DO_control : UserControl
    {
        public delegate void lbClick_e(object DO_control, EventArgs e);
        public event lbClick_e lbClick;

        string _text = "DO-";
        int _index = 0;
        bool _status = false;
        [Description("描述"), Category("控件定义")]
        public string Description
        {
            get
            {
                return this._text;
            }
            set
            {
                if (this._text != value)
                {
                    this._text = value;
                    label1.Text = _text;
                }
            }
        }
        [Description("点位"), Category("控件定义")]
        public int index
        {
            get
            {
                return this._index;
            }
            set
            {
                if (this._index != value)
                {
                    this._index = value;
                }
            }
        }
        [Description("状态"), Category("控件定义")]
        public bool status
        {
            get
            {
                return this._status;
            }
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                    ReDrawControl();
                }
            }
        }
        public DO_control()
        {
            InitializeComponent();
        }
        private void ReDrawControl()
        {
            if (!_status)
            {
                pictureBox1.Image = BasicClassLibrary.Properties.Resources.red_light;
            }
            else
            {
                pictureBox1.Image = BasicClassLibrary.Properties.Resources.green_light;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            lbClick(this, null);
        }
    }
}
