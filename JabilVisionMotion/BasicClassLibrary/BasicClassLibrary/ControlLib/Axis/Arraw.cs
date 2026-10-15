using BasicClassLibrary.Properties;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.Axis
{
    public partial class Arraw : UserControl
    {
        public Arraw()
        {
            InitializeComponent();
        }



        private void btn_Up_MouseDown(object sender, MouseEventArgs e)
        {
            btn_Up.BackgroundImage = Resources.上_;
            Application.DoEvents();
        }
        private void btn_Down_MouseDown(object sender, MouseEventArgs e)
        {
            btn_Down.BackgroundImage = Resources.下_;
            Application.DoEvents();
        }
        private void btn_Left_MouseDown(object sender, MouseEventArgs e)
        {
            btn_Left.BackgroundImage = Resources.左_;
            Application.DoEvents();
        }
        private void btn_Right_MouseDown(object sender, MouseEventArgs e)
        {
            btn_Right.BackgroundImage = Resources.右_;
            Application.DoEvents();
        }



        private void btn_Up_MouseUp(object sender, MouseEventArgs e)
        {
            btn_Up.BackgroundImage = Resources.上;
            Application.DoEvents();
        }
        private void btn_Right_MouseUp(object sender, MouseEventArgs e)
        {
            btn_Down.BackgroundImage = Resources.下;
            Application.DoEvents();
        }
        private void btn_Left_MouseUp(object sender, MouseEventArgs e)
        {
            btn_Left.BackgroundImage = Resources.左;
            Application.DoEvents();
        }
        private void btn_Down_MouseUp(object sender, MouseEventArgs e)
        {
            btn_Right.BackgroundImage = Resources.右;
            Application.DoEvents();
        }
    }
}
