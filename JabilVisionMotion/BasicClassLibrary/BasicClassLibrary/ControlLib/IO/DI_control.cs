using System.ComponentModel;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.DI
{
    public partial class DI_control : UserControl
    {
        public delegate void SetStatus(int index);
        public event SetStatus setDI;

        string _text = "DI-";
        int _index = 0;
        int _status = 0;

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
        public int status
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
        public DI_control()
        {
            InitializeComponent();
        }

        private void ReDrawControl()
        {

            switch (_status)
            {
                case 0:
                    pictureBox1.Image = BasicClassLibrary.Properties.Resources.red_light;
                    break;
                case 1:
                    pictureBox1.Image = BasicClassLibrary.Properties.Resources.green_light;
                    break;
                case 2:
                    //pictureBox1.Image = BasicClassLibrary.Properties.Resources.green_light;
                    break;

            }
        }
    }
}
