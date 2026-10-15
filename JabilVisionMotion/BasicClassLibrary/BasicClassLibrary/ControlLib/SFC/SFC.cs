using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BasicClassLibrary.Controls.SFC
{
    public enum SourceStyle
    {
        // Token: 0x040002FD RID: 765
        First = 1,
        // Token: 0x040002FE RID: 766
        Cross,
        // Token: 0x040002FF RID: 767
        Parallel
    }
    public enum Style
    {
        // Token: 0x04000301 RID: 769
        Left = 1,
        // Token: 0x04000302 RID: 770
        Mid,
        // Token: 0x04000303 RID: 771
        Right
    }
    public enum TargetStyle
    {
        // Token: 0x04000305 RID: 773
        Switch = 1,
        // Token: 0x04000306 RID: 774
        Parallel,
        // Token: 0x04000307 RID: 775
        Jump,
        // Token: 0x04000308 RID: 776
        End
    }
    public struct NoticePoint
    {
        // Token: 0x0600024F RID: 591 RVA: 0x00034E21 File Offset: 0x00033021
        public NoticePoint(Control _Parent, Point _PointInfo)
        {
            this.Parent = _Parent;
            this.PointInfo = _PointInfo;
        }

        // Token: 0x04000317 RID: 791
        public Control Parent;

        // Token: 0x04000318 RID: 792
        public Point PointInfo;
    }

    // Token: 0x02000035 RID: 53
    [ToolboxBitmap(typeof(SFC), "SFC.bmp")]
    [DefaultEvent("SFCRunEvent")]
    public class SFC : UserControl
    {
        // Token: 0x17000080 RID: 128
        // (get) Token: 0x060002EE RID: 750 RVA: 0x0003A938 File Offset: 0x00038B38
        public static List<ushort> CategoryItems
        {
            get
            {
                return SFC._CategoryItems;
            }
        }

        // Token: 0x17000081 RID: 129
        // (get) Token: 0x060002EF RID: 751 RVA: 0x0003A950 File Offset: 0x00038B50
        // (set) Token: 0x060002F0 RID: 752 RVA: 0x0003A968 File Offset: 0x00038B68
        [Category("(自定義_來源設置)")]
        [Description("來源樣式")]
        public SourceStyle Source
        {
            get
            {
                return this._Source;
            }
            set
            {
                this.PropetryIsSet[0] = true;
                if (this._Source != value)
                {
                    this._Source = value;
                    if (this._Source == SourceStyle.First)
                    {
                        this._SourceShow = Style.Mid;
                    }
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000082 RID: 130
        // (get) Token: 0x060002F1 RID: 753 RVA: 0x0003A9B8 File Offset: 0x00038BB8
        // (set) Token: 0x060002F2 RID: 754 RVA: 0x0003A9D0 File Offset: 0x00038BD0
        [Description("來源顯示樣式")]
        [Category("(自定義_來源設置)")]
        public Style SourceShow
        {
            get
            {
                return this._SourceShow;
            }
            set
            {
                this.PropetryIsSet[1] = true;
                if (this._SourceShow != value)
                {
                    this._SourceShow = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000083 RID: 131
        // (get) Token: 0x060002F3 RID: 755 RVA: 0x0003AA08 File Offset: 0x00038C08
        // (set) Token: 0x060002F4 RID: 756 RVA: 0x0003AA20 File Offset: 0x00038C20
        [Category("(自定義_來源設置)")]
        [Description("來源支路之间的间隔，单位（像素）")]
        public ushort SourceDistance
        {
            get
            {
                return this._SourceDistance;
            }
            set
            {
                this.PropetryIsSet[2] = true;
                if (this._SourceDistance != value)
                {
                    this._SourceDistance = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000084 RID: 132
        // (get) Token: 0x060002F5 RID: 757 RVA: 0x0003AA58 File Offset: 0x00038C58
        // (set) Token: 0x060002F6 RID: 758 RVA: 0x0003AA70 File Offset: 0x00038C70
        [Category("(自定義_來源設置)")]
        [Description("目標支路數目")]
        public ushort SourceCrossNum
        {
            get
            {
                return this._SourceCrossNum;
            }
            set
            {
                this.PropetryIsSet[3] = true;
                if (value > 16)
                {
                    MessageBox.Show("必須在範圍0～16內！", "提示", MessageBoxButtons.OK);
                }
                else if (this._SourceCrossNum != value)
                {
                    this._SourceCrossNum = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000085 RID: 133
        // (get) Token: 0x060002F7 RID: 759 RVA: 0x0003AAC8 File Offset: 0x00038CC8
        // (set) Token: 0x060002F8 RID: 760 RVA: 0x0003AAE0 File Offset: 0x00038CE0
        [Description("控件自身的JumpTag值")]
        [Category("(自定義_來源設置)")]
        public string SourceJumpTag
        {
            get
            {
                return this._SourceJumpTag;
            }
            set
            {
                this._SourceJumpTag = value;
                this.lbl_JumpTag.Text = this._SourceJumpTag;
            }
        }

        // Token: 0x17000086 RID: 134
        // (get) Token: 0x060002F9 RID: 761 RVA: 0x0003AAFC File Offset: 0x00038CFC
        // (set) Token: 0x060002FA RID: 762 RVA: 0x0003AB14 File Offset: 0x00038D14
        [Category("(自定義_目標設置)")]
        [Description("目標樣式")]
        public TargetStyle Target
        {
            get
            {
                return this._Target;
            }
            set
            {
                this.PropetryIsSet[4] = true;
                if (this._Target != value)
                {
                    if (value == TargetStyle.Jump && this._TargetJumpTag.Replace(" ", "") == "")
                    {
                        this._TargetJumpTag = "NoJump";
                    }
                    this._Target = value;
                    if (this._Target == TargetStyle.Jump || this._Target == TargetStyle.End)
                    {
                        this._TargetShow = Style.Mid;
                        base.Height = 59;
                    }
                    else if (this._TargetCrossNum == 0)
                    {
                        base.Height = 59;
                    }
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000087 RID: 135
        // (get) Token: 0x060002FB RID: 763 RVA: 0x0003ABD8 File Offset: 0x00038DD8
        // (set) Token: 0x060002FC RID: 764 RVA: 0x0003ABF0 File Offset: 0x00038DF0
        [Category("(自定義_目標設置)")]
        [Description("目標顯示樣式")]
        public Style TargetShow
        {
            get
            {
                return this._TargetShow;
            }
            set
            {
                this.PropetryIsSet[5] = true;
                if (this._TargetShow != value)
                {
                    this._TargetShow = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x17000088 RID: 136
        // (get) Token: 0x060002FD RID: 765 RVA: 0x0003AC28 File Offset: 0x00038E28
        // (set) Token: 0x060002FE RID: 766 RVA: 0x0003AC40 File Offset: 0x00038E40
        [Category("(自定義_目標設置)")]
        [Description("目標支路之间的间隔，单位（像素）")]
        public ushort TargetDistance
        {
            get
            {
                return this._TargetDistance;
            }
            set
            {
                this.PropetryIsSet[6] = true;
                this._TargetDistance = value;
                this.reDraw(-1);
            }
        }

        // Token: 0x17000089 RID: 137
        // (get) Token: 0x060002FF RID: 767 RVA: 0x0003AC5C File Offset: 0x00038E5C
        // (set) Token: 0x06000300 RID: 768 RVA: 0x0003AC74 File Offset: 0x00038E74
        [Description("目標支路數目")]
        [Category("(自定義_目標設置)")]
        public ushort TargetCrossNum
        {
            get
            {
                return this._TargetCrossNum;
            }
            set
            {
                this.PropetryIsSet[7] = true;
                if (value > 16)
                {
                    MessageBox.Show("必須在範圍0～16內！", "提示", MessageBoxButtons.OK);
                }
                else if (this._TargetCrossNum != value)
                {
                    this._TargetCrossNum = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x1700008A RID: 138
        // (get) Token: 0x06000301 RID: 769 RVA: 0x0003ACCC File Offset: 0x00038ECC
        // (set) Token: 0x06000302 RID: 770 RVA: 0x0003ACE4 File Offset: 0x00038EE4
        [Description("跳轉目標的JumpTag值")]
        [Category("(自定義_目標設置)")]
        public string TargetJumpTag
        {
            get
            {
                return this._TargetJumpTag;
            }
            set
            {
                if (this._TargetJumpTag != value)
                {
                    this._TargetJumpTag = value;
                    this.reDraw(-1);
                }
            }
        }

        // Token: 0x1700008B RID: 139
        // (get) Token: 0x06000303 RID: 771 RVA: 0x0003AD18 File Offset: 0x00038F18
        [Description("指示當前是否在運行")]
        [Category("(自定義_状态与分类)")]
        public bool IsRunning
        {
            get
            {
                bool result;
                if (base.InvokeRequired)
                {
                    Func<bool> func = new Func<bool>(this.GetEnable);
                    result = func();
                }
                else
                {
                    result = this.GetEnable();
                }
                return result;
            }
        }

        // Token: 0x1700008C RID: 140
        // (get) Token: 0x06000304 RID: 772 RVA: 0x0003AD54 File Offset: 0x00038F54
        // (set) Token: 0x06000305 RID: 773 RVA: 0x0003AD6B File Offset: 0x00038F6B
        [Description("指示设计模式下移动控件時是否自動定位")]
        [Category("(自定義_状态与分类)")]
        public bool AutoLocate
        {
            get
            {
                return SFC._AutoLocate;
            }
            set
            {
                SFC._AutoLocate = value;
            }
        }

        // Token: 0x1700008D RID: 141
        // (get) Token: 0x06000306 RID: 774 RVA: 0x0003AD74 File Offset: 0x00038F74
        // (set) Token: 0x06000307 RID: 775 RVA: 0x0003AD8C File Offset: 0x00038F8C
        [Category("(自定義_状态与分类)")]
        [Description("模块标记")]
        public ushort Category
        {
            get
            {
                return this._Category;
            }
            set
            {
                this._Category = value;
                if (!SFC._CategoryItems.Contains(value))
                {
                    SFC._CategoryItems.Add(value);
                }
            }
        }

        // Token: 0x1700008E RID: 142
        // (get) Token: 0x06000308 RID: 776 RVA: 0x0003ADC0 File Offset: 0x00038FC0
        protected internal bool AllPropetryIsSet
        {
            get
            {
                return this.PropetryIsSet[0] && this.PropetryIsSet[1] && this.PropetryIsSet[2] && this.PropetryIsSet[3] && this.PropetryIsSet[4] && this.PropetryIsSet[5] && this.PropetryIsSet[6] && this.PropetryIsSet[7];
            }
        }

        // Token: 0x14000015 RID: 21
        // (add) Token: 0x06000309 RID: 777 RVA: 0x0003AE24 File Offset: 0x00039024
        // (remove) Token: 0x0600030A RID: 778 RVA: 0x0003AE60 File Offset: 0x00039060
        private static event SFC.InitdHandler InitEvent;

        // Token: 0x14000016 RID: 22
        // (add) Token: 0x0600030B RID: 779 RVA: 0x0003AE9C File Offset: 0x0003909C
        // (remove) Token: 0x0600030C RID: 780 RVA: 0x0003AED8 File Offset: 0x000390D8
        private static event SFC.MarkInitdHandler MarkInitEvent;

        // Token: 0x14000017 RID: 23
        // (add) Token: 0x0600030D RID: 781 RVA: 0x0003AF14 File Offset: 0x00039114
        // (remove) Token: 0x0600030E RID: 782 RVA: 0x0003AF50 File Offset: 0x00039150
        private static event SFC.MarkPauseHandler MarkPauseEvent;

        // Token: 0x14000018 RID: 24
        // (add) Token: 0x0600030F RID: 783 RVA: 0x0003AF8C File Offset: 0x0003918C
        // (remove) Token: 0x06000310 RID: 784 RVA: 0x0003AFC8 File Offset: 0x000391C8
        public event SFC.SFCRunHandler SFCRunEvent;

        // Token: 0x06000311 RID: 785 RVA: 0x0003B004 File Offset: 0x00039204
        public SFC()
        {
            this.InitializeComponent();
            lock (SFC.aTimerLocker)
            {
                this.aTimer.Enabled = false;
            }
            SFC.InitEvent += this.init;
            SFC.MarkInitEvent += this.MarkInitMethod;
            SFC.MarkPauseEvent += this.ChangeMyPause;
            SFC.GenSimSFCProgressEvent = (SFC.GenSFCProgressEventHandle)Delegate.Combine(SFC.GenSimSFCProgressEvent, new SFC.GenSFCProgressEventHandle(this.GenLastAndNextSimSFC));
            SFC.MethodClearEvent = (SFC.MethodClearHandler)Delegate.Combine(SFC.MethodClearEvent, new SFC.MethodClearHandler(this.ClearMethod));
            base.Move += this.SimSFC_Move;
        }

        // Token: 0x06000312 RID: 786 RVA: 0x0003B210 File Offset: 0x00039410
        private void ChangeMyUnit(object sender, EventArgs e)
        {
            this.Font = new Font("楷体", 9f, ((Control)sender).Font.Unit);
        }

        // Token: 0x06000313 RID: 787 RVA: 0x0003B23C File Offset: 0x0003943C
        protected internal bool IsContainPoint(NoticePoint NPoint, string Jump)
        {
            return (NPoint.PointInfo.X - base.Location.X <= base.Width && NPoint.PointInfo.X >= base.Location.X && NPoint.PointInfo.Y - base.Location.Y <= base.Height && NPoint.PointInfo.Y >= base.Location.Y && NPoint.Parent == base.Parent) || this._SourceJumpTag == Jump;
        }

        // Token: 0x06000314 RID: 788 RVA: 0x0003B2F8 File Offset: 0x000394F8
        private void GenLastAndNextSimSFC()
        {
            if (this.SFCRunEvent != null)
            {
                if (this.SFCRunEvent.GetInvocationList().Length > 1)
                {
                    throw new IndexOutOfRangeException("SimSFCRunEvent所订阅的方法最多只能1个，订阅多个将产生此错误！");
                }
            }
            switch (this._Source)
            {
                case SourceStyle.Cross:
                case SourceStyle.Parallel:
                    switch (this._SourceShow)
                    {
                        case Style.Left:
                            for (int i = 0; i <= (int)this._SourceCrossNum; i++)
                            {
                                foreach (object obj in base.Parent.Controls)
                                {
                                    Control control = (Control)obj;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance) + i * (int)this._SourceDistance, base.Location.Y - 5)), null))
                                        {
                                            this.LastSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                        case Style.Mid:
                            for (int i = 0; i <= (int)this._SourceCrossNum; i++)
                            {
                                foreach (object obj2 in base.Parent.Controls)
                                {
                                    Control control = (Control)obj2;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance / 2) + i * (int)this._SourceDistance, base.Location.Y - 5)), null))
                                        {
                                            this.LastSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                        case Style.Right:
                            for (int i = 0; i <= (int)this._SourceCrossNum; i++)
                            {
                                foreach (object obj3 in base.Parent.Controls)
                                {
                                    Control control = (Control)obj3;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X + i * (int)this._SourceDistance, base.Location.Y - 5)), null))
                                        {
                                            this.LastSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }
            switch (this._Target)
            {
                case TargetStyle.Switch:
                case TargetStyle.Parallel:
                    switch (this._TargetShow)
                    {
                        case Style.Left:
                            for (int i = 0; i <= (int)this._TargetCrossNum; i++)
                            {
                                foreach (object obj4 in base.Parent.Controls)
                                {
                                    Control control = (Control)obj4;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._TargetCrossNum * this._TargetDistance) + i * (int)this._TargetDistance, base.Location.Y + base.Height + 5)), null))
                                        {
                                            this.NextSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                        case Style.Mid:
                            for (int i = 0; i <= (int)this._TargetCrossNum; i++)
                            {
                                foreach (object obj5 in base.Parent.Controls)
                                {
                                    Control control = (Control)obj5;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._TargetCrossNum * this._TargetDistance / 2) + i * (int)this._TargetDistance, base.Location.Y + base.Height + 5)), null))
                                        {
                                            this.NextSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                        case Style.Right:
                            for (int i = 0; i <= (int)this._TargetCrossNum; i++)
                            {
                                foreach (object obj6 in base.Parent.Controls)
                                {
                                    Control control = (Control)obj6;
                                    if (control is SFC)
                                    {
                                        if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X + i * (int)this._TargetDistance, base.Location.Y + base.Height + 5)), null))
                                        {
                                            this.NextSimSFC.Add(control as SFC);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    break;
                case TargetStyle.Jump:
                    foreach (object obj7 in base.Parent.Controls)
                    {
                        Control control = (Control)obj7;
                        if (control is SFC)
                        {
                            if (control as SFC != this && (control as SFC).IsContainPoint(new NoticePoint(null, new Point(-2147483648, -2147483648)), this._TargetJumpTag))
                            {
                                this.NextSimSFC.Add(control as SFC);
                            }
                        }
                    }
                    break;
            }
            this.lbl_info.Text = "L=" + this.LastSimSFC.Count.ToString() + ",N=" + this.NextSimSFC.Count.ToString();
        }

        // Token: 0x06000315 RID: 789 RVA: 0x0003BB64 File Offset: 0x00039D64
        private void SimSFC_Load(object sender, EventArgs e)
        {
        }

        // Token: 0x06000316 RID: 790 RVA: 0x0003BB68 File Offset: 0x00039D68
        private void reDraw(int select)
        {
            int height = base.Height;
            int num = 60;
            int num2 = 60;
            if (this._TargetCrossNum == 0 && this._Target == TargetStyle.Parallel)
            {
                this._Target = TargetStyle.Switch;
            }
            switch (this._Source)
            {
                case SourceStyle.Cross:
                case SourceStyle.Parallel:
                    num = ((num > (int)(this._SourceCrossNum * this._SourceDistance)) ? num : ((int)(this._SourceCrossNum * this._SourceDistance)));
                    break;
            }
            switch (this._Target)
            {
                case TargetStyle.Switch:
                case TargetStyle.Parallel:
                    num2 = ((num2 > (int)(this._TargetCrossNum * this.TargetDistance)) ? num2 : ((int)(this._TargetCrossNum * this.TargetDistance)));
                    break;
            }
            int num3 = 0;
            int num4 = 0;
            switch (this._SourceShow)
            {
                case Style.Left:
                    switch (this._TargetShow)
                    {
                        case Style.Left:
                            num3 = ((num > num2) ? (num + 30) : (num2 + 30));
                            if (this._SourceCrossNum == 0 && this._TargetCrossNum == 0)
                            {
                                num4 = 30;
                            }
                            else
                            {
                                num4 = ((num > num2) ? num : num2);
                            }
                            break;
                        case Style.Mid:
                            if (this._SourceCrossNum == 0)
                            {
                                num3 = num2;
                                num4 = num2 / 2;
                            }
                            else
                            {
                                num3 = ((num > num2 / 2) ? (num + num2 / 2) : num2);
                                num4 = ((num > num2 / 2) ? num : (num2 / 2));
                            }
                            break;
                        case Style.Right:
                            if (this._SourceCrossNum != 0)
                            {
                                if (this._TargetCrossNum != 0)
                                {
                                    num3 = num + num2;
                                }
                                else
                                {
                                    num3 = num + 40;
                                }
                                num4 = num;
                            }
                            else
                            {
                                num3 = 40 + num2;
                                num4 = 40;
                            }
                            break;
                    }
                    break;
                case Style.Mid:
                    switch (this._TargetShow)
                    {
                        case Style.Left:
                            num3 = ((num / 2 < num2) ? (num / 2 + num2) : num);
                            num4 = ((num / 2 > num2) ? (num / 2) : num2);
                            break;
                        case Style.Mid:
                            num3 = ((num > num2) ? num : num2);
                            num4 = ((num > num2) ? (num / 2) : (num2 / 2));
                            break;
                        case Style.Right:
                            num3 = ((num / 2 < num2) ? (num / 2 + num2) : num);
                            num4 = num / 2;
                            break;
                    }
                    if (this._TargetCrossNum == 0)
                    {
                        num3 = num;
                        num4 = num / 2;
                    }
                    break;
                case Style.Right:
                    switch (this._TargetShow)
                    {
                        case Style.Left:
                            if (this._TargetCrossNum != 0)
                            {
                                if (this._SourceCrossNum != 0)
                                {
                                    num3 = num + num2;
                                }
                                else
                                {
                                    num3 = 20 + num2;
                                }
                                num4 = num2;
                            }
                            else
                            {
                                num3 = num + 40;
                                num4 = 40;
                            }
                            break;
                        case Style.Mid:
                            if (this._SourceCrossNum != 0)
                            {
                                num3 = ((num > num2 / 2) ? (num + num2 / 2) : num2);
                            }
                            else
                            {
                                num3 = num2;
                            }
                            num4 = num2 / 2;
                            break;
                        case Style.Right:
                            num3 = ((num > num2) ? (num + 30) : (num2 + 30));
                            num4 = 30;
                            break;
                    }
                    break;
            }
            if (this._SourceCrossNum == 0 && this._TargetCrossNum == 0)
            {
                num3 = 50;
                num4 = 30;
            }
            num3 += 20;
            this.ptb_showLine.Width = (base.Width = num3);
            this.ptb_showLine.Height = (base.Height = height);
            Bitmap image = new Bitmap(this.ptb_showLine.Width, this.ptb_showLine.Height);
            Pen pen = new Pen(Color.OrangeRed, 2f);
            Graphics graphics = Graphics.FromImage(image);
            Brush brush = new SolidBrush(Color.Red);
            Font font = new Font("Arial", 8f, FontStyle.Regular);
            graphics.DrawRectangle(new Pen(this.BackColor, (float)this.TargetDistance), 0, 0, this.ptb_showLine.Width, this.ptb_showLine.Height);
            num4 += 5;
            this.ProgressMid_X = num4;
            this.panel_ProgressShow.Location = new Point(num4 - this.panel_ProgressShow.Width / 2, 10);
            if (this._Source == SourceStyle.First)
            {
                graphics.DrawLine(pen, num4, 10, num4, 39);
            }
            else
            {
                graphics.DrawLine(pen, num4, 6, num4, 39);
            }
            switch (this._Source)
            {
                case SourceStyle.Cross:
                case SourceStyle.Parallel:
                    {
                        int num5 = 0;
                        switch (this._SourceShow)
                        {
                            case Style.Left:
                                num5 = num4 - (int)(this._SourceDistance * this._SourceCrossNum);
                                break;
                            case Style.Mid:
                                num5 = num4 - (int)(this._SourceDistance * this._SourceCrossNum / 2);
                                break;
                            case Style.Right:
                                num5 = num4;
                                break;
                        }
                        for (int i = 0; i <= (int)this._SourceCrossNum; i++)
                        {
                            graphics.DrawLine(pen, num5 + (int)this._SourceDistance * i, 0, num5 + (int)this._SourceDistance * i, 6);
                        }
                        graphics.DrawLine(pen, num5, 6, num5 + (int)(this._SourceDistance * this._SourceCrossNum), 6);
                        if (this._Source == SourceStyle.Parallel)
                        {
                            graphics.DrawLine(pen, num5, 3, num5 + (int)(this._SourceDistance * this._SourceCrossNum) - 1, 3);
                        }
                        break;
                    }
            }
            switch (this._Target)
            {
                case TargetStyle.Switch:
                case TargetStyle.Parallel:
                    {
                        int num6 = 0;
                        switch (this._TargetShow)
                        {
                            case Style.Left:
                                num6 = num4 - (int)(this._TargetDistance * this._TargetCrossNum);
                                break;
                            case Style.Mid:
                                num6 = num4 - (int)(this._TargetDistance * this._TargetCrossNum / 2);
                                break;
                            case Style.Right:
                                num6 = num4;
                                break;
                        }
                        for (int i = 0; i <= (int)this._TargetCrossNum; i++)
                        {
                            graphics.DrawLine(pen, num6 + i * (int)this.TargetDistance, 39, num6 + i * (int)this.TargetDistance, height);
                            if (this._Target == TargetStyle.Switch)
                            {
                                graphics.DrawString(i.ToString(), font, brush, (float)(num6 + i * (int)this.TargetDistance), 43f);
                            }
                            else
                            {
                                graphics.DrawString("0", font, brush, (float)(num6 + i * (int)this.TargetDistance), 43f);
                            }
                        }
                        graphics.DrawLine(pen, num6, 39, num6 + (int)(this._TargetCrossNum * this.TargetDistance), 39);
                        if (this._Target == TargetStyle.Parallel)
                        {
                            graphics.DrawLine(pen, num6, 42, num6 + (int)(this._TargetCrossNum * this.TargetDistance), 42);
                        }
                        break;
                    }
                case TargetStyle.Jump:
                    graphics.DrawLine(pen, num4, 35, num4, 45);
                    graphics.DrawLine(pen, num4, 45, num4 + 5, 37);
                    graphics.DrawLine(pen, num4 - 1, 45, num4 - 6, 37);
                    graphics.DrawString(this._TargetJumpTag, font, brush, (float)(num4 - 30), 45f);
                    break;
                case TargetStyle.End:
                    graphics.DrawLine(pen, num4, 35, num4, 57);
                    graphics.DrawLine(pen, num4 - 30, 57, num4 + 30, 57);
                    graphics.DrawString("End", font, brush, (float)num4, 43f);
                    break;
            }
            this.ptb_showLine.Image = image;
            this.ptb_showLine.Refresh();
        }

        // Token: 0x06000317 RID: 791 RVA: 0x0003C30D File Offset: 0x0003A50D
        private void ptb_showLine_SizeChanged(object sender, EventArgs e)
        {
        }

        // Token: 0x06000318 RID: 792 RVA: 0x0003C314 File Offset: 0x0003A514
        private void aTimer_Tick(object sender, EventArgs e)
        {
            short num = -1;
            if (this.MyMarkPause)
            {
                this.panel_ProgressShow.BackColor = Color.Khaki;
                this.FirstCircleFlagAfterPause = true;
            }
            if (!this.TickEventIsRun && !this.MyMarkPause)
            {
                this.panel_ProgressShow.BackColor = this._RunColor;
                this.TickEventIsRun = true;
                if (this.FirstCircleFlag)
                {
                    this.FirstCircleFlagAfterPause = false;
                    this.FirstCircleTimAfterPause = DateTime.Now;
                    this.NextStarPointIsSend = false;
                    this.FirstCircleTime = DateTime.Now;
                    this.TV_Data = new decimal[10];
                    this.TV_M = new bool[10];
                    this.TV_Object = new object[10];
                    this.TV_Tim = new DateTime[]
                    {
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime,
                        this.FirstCircleTime
                    };
                    this.TV_Int = new int[10];
                }
                if (this.SFCRunEvent != null)
                {
                    if (this.FirstCircleFlagAfterPause)
                    {
                        this.FirstCircleTimAfterPause = DateTime.Now;
                    }
                    num = this.SFCRunEvent(this.FirstCircleFlag, this.FirstCircleTime, this);
                    this.FirstCircleFlagAfterPause = false;
                    if (num > (short)this._TargetCrossNum)
                    {
                        throw new ArgumentOutOfRangeException("SimSFC控件返回值錯誤", "控件名稱為：" + base.Name + "的控件TransEvent訂閱的方法返回值超出範圍；範圍：負數 - Cross屬性值！");
                    }
                }
                this.FirstCircleFlag = false;
                if (num >= 0 && this.aTimer.Enabled)
                {
                    if (this._Target == TargetStyle.End)
                    {
                        this.aTimer.Enabled = false;
                        this.panel_ProgressShow.BackColor = Color.FloralWhite;
                    }
                    if (this.NextSimSFC.Count > 0)
                    {
                        switch (this._Target)
                        {
                            case TargetStyle.Switch:
                                if (this.NextSimSFC[(int)num].Source == SourceStyle.Parallel && this.NextSimSFC[(int)num]._SourceCrossNum > 0)
                                {
                                    switch (this._TargetShow)
                                    {
                                        case Style.Left:
                                            this.NextSimSFC[(int)num].Run(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance) + (int)(num * (short)this._SourceDistance), base.Location.Y + base.Height)));
                                            break;
                                        case Style.Mid:
                                            this.NextSimSFC[(int)num].Run(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance / 2) + (int)(num * (short)this._SourceDistance), base.Location.Y + base.Height)));
                                            break;
                                        case Style.Right:
                                            this.NextSimSFC[(int)num].Run(new NoticePoint(base.Parent, new Point(base.Location.X + this.ProgressMid_X + (int)(num * (short)this._SourceDistance), base.Location.Y + base.Height)));
                                            break;
                                    }
                                }
                                else
                                {
                                    this.NextSimSFC[(int)num].Run(true);
                                }
                                break;
                            case TargetStyle.Parallel:
                                foreach (SFC sfc in this.NextSimSFC)
                                {
                                    sfc.Run(true);
                                }
                                break;
                            case TargetStyle.Jump:
                                this.Stop();
                                this.NextSimSFC[0].Run(true);
                                break;
                            case TargetStyle.End:
                                this.Stop();
                                break;
                        }
                    }
                }
                this.TickEventIsRun = false;
            }
        }

        // Token: 0x06000319 RID: 793 RVA: 0x0003C7E0 File Offset: 0x0003A9E0
        private void init()
        {
            lock (SFC.aTimerLocker)
            {
                this.aTimer.Enabled = false;
            }
        }

        // Token: 0x0600031A RID: 794 RVA: 0x0003C83C File Offset: 0x0003AA3C
        private void MarkInitMethod(ushort mark)
        {
            if (base.InvokeRequired)
            {
                Action<ushort> method = delegate (ushort x)
                {
                    this._MarkInitMethod(x);
                };
                base.BeginInvoke(method, new object[]
                {
                    mark
                });
            }
            else
            {
                this._MarkInitMethod(mark);
            }
        }

        // Token: 0x0600031B RID: 795 RVA: 0x0003C898 File Offset: 0x0003AA98
        private void _MarkInitMethod(ushort mark)
        {
            if (this._Category == mark)
            {
                lock (SFC.aTimerLocker)
                {
                    this.aTimer.Enabled = false;
                }
                this.panel_ProgressShow.BackColor = Color.FloralWhite;
            }
        }

        // Token: 0x0600031C RID: 796 RVA: 0x0003C908 File Offset: 0x0003AB08
        private void ptb_showLine_Click(object sender, EventArgs e)
        {
        }

        // Token: 0x0600031D RID: 797 RVA: 0x0003C90C File Offset: 0x0003AB0C
        protected internal void SimSFC_Move(object sender, EventArgs e)
        {
            if (Control.MouseButtons == MouseButtons.None)
            {
                if (!(SFC.LastMoveTim.AddMilliseconds(1000.0) > DateTime.Now))
                {
                    SFC.LastMoveTim = DateTime.Now;
                    this.panel_ProgressShow.BackColor = Color.FloralWhite;
                    if (SFC._AutoLocate)
                    {
                        if (base.Created)
                        {
                            if (base.DesignMode)
                            {
                                Control parent = (sender as SFC).Parent;
                                if (this._Source != SourceStyle.First)
                                {
                                    if (parent != null)
                                    {
                                        int num = 0;
                                        switch (this._SourceShow)
                                        {
                                            case Style.Left:
                                                num = this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance);
                                                break;
                                            case Style.Mid:
                                                num = this.ProgressMid_X - (int)(this._SourceCrossNum * this._SourceDistance / 2);
                                                break;
                                            case Style.Right:
                                                num = this.ProgressMid_X;
                                                break;
                                        }
                                        Point pt = new Point(base.Location.X + num, base.Location.Y - 10);
                                        Control childAtPoint = (sender as SFC).Parent.GetChildAtPoint(pt);
                                        if (!(childAtPoint is SFC) && this._Source != SourceStyle.First)
                                        {
                                            this.panel_ProgressShow.BackColor = Color.Red;
                                        }
                                        else if ((childAtPoint as SFC).Target == TargetStyle.End || (childAtPoint as SFC).Target == TargetStyle.Jump)
                                        {
                                            this.panel_ProgressShow.BackColor = Color.Red;
                                        }
                                        else
                                        {
                                            int num2 = 0;
                                            int num3 = int.MaxValue;
                                            int num4 = 0;
                                            switch ((childAtPoint as SFC).TargetShow)
                                            {
                                                case Style.Left:
                                                    num4 = (childAtPoint as SFC).Location.X + (childAtPoint as SFC).ProgressMid_X - (int)((childAtPoint as SFC).TargetCrossNum * (childAtPoint as SFC).TargetDistance);
                                                    break;
                                                case Style.Mid:
                                                    num4 = (childAtPoint as SFC).Location.X + (childAtPoint as SFC).ProgressMid_X - (int)((childAtPoint as SFC).TargetCrossNum * (childAtPoint as SFC).TargetDistance / 2);
                                                    break;
                                                case Style.Right:
                                                    num4 = (childAtPoint as SFC).Location.X + (childAtPoint as SFC).ProgressMid_X;
                                                    break;
                                            }
                                            for (int i = 0; i <= (int)(childAtPoint as SFC).TargetCrossNum; i++)
                                            {
                                                if (num3 > Math.Abs(pt.X - (num4 + (int)(childAtPoint as SFC).TargetDistance * i)))
                                                {
                                                    num2 = i;
                                                    num3 = Math.Abs(pt.X - (num4 + (int)(childAtPoint as SFC).TargetDistance * i));
                                                }
                                            }
                                            int num5 = num4 + num2 * (int)(childAtPoint as SFC).TargetDistance;
                                            Point location = new Point(num5 - num, childAtPoint.Location.Y + childAtPoint.Height);
                                            base.Location = location;
                                            this.panel_ProgressShow.BackColor = Color.FloralWhite;
                                            List<Control> list = new List<Control>();
                                            foreach (object obj in (sender as SFC).Parent.Controls)
                                            {
                                                Control control = (Control)obj;
                                                if (control.Location.X < location.X + 20 && control.Location.X + control.Width > location.X + 20 && control.Location.Y < location.Y + 20 && control.Location.Y + control.Height > location.Y + 20)
                                                {
                                                    list.Add(control);
                                                }
                                            }
                                            if (list.Count > 1)
                                            {
                                                base.Location = new Point(location.X + 20, location.Y + 20);
                                                this.panel_ProgressShow.BackColor = Color.Red;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Token: 0x0600031E RID: 798 RVA: 0x0003CDD0 File Offset: 0x0003AFD0
        private float Distance(Point P0, Point P1)
        {
            return (float)Math.Sqrt((double)((P0.X - P1.X) * (P0.X - P1.X) + (P0.Y - P1.Y) * (P0.Y - P1.Y)));
        }

        // Token: 0x0600031F RID: 799 RVA: 0x0003CE34 File Offset: 0x0003B034
        private void ChangeMyPause(ushort mark, bool pause)
        {
            if (base.InvokeRequired)
            {
                Action<ushort, bool> method = delegate (ushort x, bool y)
                {
                    this._ChangeMyPause(x, y);
                };
                base.BeginInvoke(method, new object[]
                {
                    mark,
                    pause
                });
            }
            else
            {
                this._ChangeMyPause(mark, pause);
            }
        }

        // Token: 0x06000320 RID: 800 RVA: 0x0003CE98 File Offset: 0x0003B098
        private void _ChangeMyPause(ushort mark, bool pause)
        {
            if (mark == this.Category)
            {
                this.MyMarkPause = pause;
            }
            if (pause && !SFC.MarkPauseRec.Contains((int)this.Category))
            {
                SFC.MarkPauseRec.Add((int)this.Category);
            }
            if (!pause)
            {
                while (SFC.MarkPauseRec.Contains((int)this.Category))
                {
                    SFC.MarkPauseRec.Remove((int)this.Category);
                }
            }
        }

        // Token: 0x06000321 RID: 801 RVA: 0x0003CF1C File Offset: 0x0003B11C
        private void _ClearMethod()
        {
            if (this.SFCRunEvent != null)
            {
                this.SFCRunEvent = null;
            }
        }

        // Token: 0x06000322 RID: 802 RVA: 0x0003CF48 File Offset: 0x0003B148
        private void ClearMethod()
        {
            if (base.InvokeRequired)
            {
                Action method = delegate ()
                {
                    this._ClearMethod();
                };
                base.BeginInvoke(method);
            }
            else
            {
                this._ClearMethod();
            }
        }

        // Token: 0x06000323 RID: 803 RVA: 0x0003CF90 File Offset: 0x0003B190
        private void SimSFC_SizeChanged(object sender, EventArgs e)
        {
            if (this.AllPropetryIsSet)
            {
                base.SizeChanged -= this.SimSFC_SizeChanged;
                if (this._Target == TargetStyle.Jump || this._Target == TargetStyle.End)
                {
                    base.Height = 59;
                    base.SizeChanged += this.SimSFC_SizeChanged;
                }
                else if (this._TargetCrossNum != 0)
                {
                    base.Height = 59;
                    base.SizeChanged += this.SimSFC_SizeChanged;
                }
                else if (base.Height % 59 > 30)
                {
                    int num = 0;
                    for (int i = 1; i < base.Height / 59 + 1; i++)
                    {
                        Control childAtPoint = base.Parent.GetChildAtPoint(new Point(base.Location.X + this.ProgressMid_X, base.Location.Y + 30 + i * 59));
                        if (childAtPoint is SFC)
                        {
                            num = i;
                            i = base.Height / 59 + 1;
                        }
                    }
                    if (num == 0)
                    {
                        base.Height = base.Height / 59 * 59 + 59;
                    }
                    else
                    {
                        base.Height = num * 59 + 59;
                    }
                    base.SizeChanged += this.SimSFC_SizeChanged;
                    this.reDraw(-1);
                }
                else
                {
                    base.Height = 59;
                    base.SizeChanged += this.SimSFC_SizeChanged;
                }
            }
        }

        // Token: 0x06000324 RID: 804 RVA: 0x0003D130 File Offset: 0x0003B330
        private bool GetEnable()
        {
            return this.aTimer.Enabled;
        }

        // Token: 0x06000325 RID: 805 RVA: 0x0003D1B0 File Offset: 0x0003B3B0
        protected internal void Stop()
        {
            if (base.InvokeRequired)
            {
                Action method = delegate ()
                {
                    lock (SFC.aTimerLocker)
                    {
                        this.aTimer.Enabled = false;
                    }
                    this.panel_ProgressShow.BackColor = Color.FloralWhite;
                };
                base.BeginInvoke(method);
            }
            else
            {
                this.aTimer.Enabled = false;
                this.panel_ProgressShow.BackColor = Color.FloralWhite;
            }
        }

        // Token: 0x06000326 RID: 806 RVA: 0x0003D210 File Offset: 0x0003B410
        private void _Run()
        {
            lock (SFC.aTimerLocker)
            {
                if (!this.aTimer.Enabled)
                {
                    this.aTimer.Enabled = true;
                    this.FirstCircleFlag = true;
                }
            }
        }

        // Token: 0x06000327 RID: 807 RVA: 0x0003D28C File Offset: 0x0003B48C
        public void Run()
        {
            if (base.InvokeRequired)
            {
                Action method = delegate ()
                {
                    this._Run();
                };
                base.BeginInvoke(method);
            }
            else
            {
                this._Run();
            }
        }

        // Token: 0x06000328 RID: 808 RVA: 0x0003D2E0 File Offset: 0x0003B4E0
        public void Run(string Mark)
        {
            if (base.InvokeRequired)
            {
                Action<string> method = delegate (string x)
                {
                    this._Run(x);
                };
                base.BeginInvoke(method, new object[]
                {
                    Mark
                });
            }
            else
            {
                this._Run(Mark);
            }
        }

        // Token: 0x06000329 RID: 809 RVA: 0x0003D334 File Offset: 0x0003B534
        private void _Run(string Mark)
        {
            if (!(Mark != this._SourceJumpTag) || !(this._SourceJumpTag != ""))
            {
                this.Run();
            }
        }

        // Token: 0x0600032A RID: 810 RVA: 0x0003D374 File Offset: 0x0003B574
        protected internal void Run(NoticePoint NP)
        {
            if (!this.RevSource.Contains(NP))
            {
                this.RevSource.Add(NP);
            }
            if (this.RevSource.Count >= (int)(this._SourceCrossNum + 1))
            {
                this.RevSource.Clear();
                foreach (SFC sfc in this.LastSimSFC)
                {
                    sfc.Stop();
                }
                this.Run();
            }
        }

        // Token: 0x0600032B RID: 811 RVA: 0x0003D41C File Offset: 0x0003B61C
        protected internal void Run(bool RSTAll)
        {
            foreach (SFC sfc in this.LastSimSFC)
            {
                sfc.Stop();
            }
            this.Run();
        }

        // Token: 0x0600032C RID: 812 RVA: 0x0003D480 File Offset: 0x0003B680
        public static void Init(ushort category)
        {
            if (SFC.MarkInitEvent != null)
            {
                SFC.MarkInitEvent(category);
            }
        }

        // Token: 0x0600032D RID: 813 RVA: 0x0003D4A8 File Offset: 0x0003B6A8
        public static void Pause(ushort category)
        {
            if (SFC.MarkPauseEvent != null)
            {
                SFC.MarkPauseEvent(category, true);
            }
        }

        // Token: 0x0600032E RID: 814 RVA: 0x0003D4D4 File Offset: 0x0003B6D4
        public static void Continue(ushort category)
        {
            if (SFC.MarkPauseEvent != null)
            {
                SFC.MarkPauseEvent(category, false);
            }
        }

        // Token: 0x0600032F RID: 815 RVA: 0x0003D500 File Offset: 0x0003B700
        public static void GenSFCProgress()
        {
            if (SFC.GenSimSFCProgressEvent != null)
            {
                SFC.GenSimSFCProgressEvent();
            }
        }

        // Token: 0x06000330 RID: 816 RVA: 0x0003D528 File Offset: 0x0003B728
        public static void AllSFCMethodClear()
        {
            if (SFC.MethodClearEvent != null)
            {
                SFC.MethodClearEvent();
            }
        }

        // Token: 0x06000331 RID: 817 RVA: 0x0003D550 File Offset: 0x0003B750
        public static bool GetCategoryPauseState(ushort category)
        {
            return SFC.MarkPauseRec.Contains((int)category);
        }

        // Token: 0x06000332 RID: 818 RVA: 0x0003D570 File Offset: 0x0003B770
        private void panel_ProgressShow_Or_lbl_DoubleClick(object sender, EventArgs e)
        {
            this.OnDoubleClick(e);
            if (SFC.GetProgressInfoEn)
            {
                this.ShowProgressInfo();
            }
        }

        // Token: 0x06000333 RID: 819 RVA: 0x0003D598 File Offset: 0x0003B798
        private void ShowProgressInfo()
        {
            Console.WriteLine("当前选中SimSFC控件信息...");
            Console.WriteLine("控件名称:{0}", base.Name);
            Console.WriteLine("控件坐标:{0},{1}", base.Location.X, base.Location.Y);
            Console.WriteLine("运行状态:{0}", this.IsRunning);
            Console.WriteLine("暂停状态:{0}", this.MyMarkPause);
            Console.WriteLine("最近一次运行时间:{0},{1}\r\n", this.FirstCircleTime, this.FirstCircleTime.Millisecond);
            Console.WriteLine("内部布尔暂存器状态...");
            for (int i = 0; i < this.TV_M.Length; i++)
            {
                Console.WriteLine("TV_M[" + i.ToString() + "]    " + this.TV_M[i].ToString());
            }
            Console.WriteLine("\r\n内部decimal暂存器状态...");
            for (int i = 0; i < this.TV_Data.Length; i++)
            {
                Console.WriteLine("TV_Data[" + i.ToString() + "]    " + this.TV_Data[i].ToString());
            }
            Console.WriteLine("\r\n内部时间节点暂存器状态...");
            for (int i = 0; i < this.TV_Tim.Length; i++)
            {
                Console.WriteLine(string.Concat(new string[]
                {
                    "TV_Tim[",
                    i.ToString(),
                    "]    ",
                    this.TV_Tim[i].ToLongTimeString(),
                    ",",
                    this.TV_Tim[i].Millisecond.ToString()
                }));
            }
            MethodInfo methodInfo = this.SFCRunEvent.GetInvocationList()[0].GetMethodInfo();
            Console.WriteLine("\r\n方法名称：" + methodInfo.Name);
        }

        // Token: 0x06000334 RID: 820 RVA: 0x0003D7A5 File Offset: 0x0003B9A5
        private void SFC_ParentChanged(object sender, EventArgs e)
        {
        }

        // Token: 0x06000335 RID: 821 RVA: 0x0003D7A8 File Offset: 0x0003B9A8
        private void panel_ProgressShow_MouseDown(object sender, MouseEventArgs e)
        {
            this.OnMouseDown(e);
        }

        // Token: 0x06000336 RID: 822 RVA: 0x0003D7B3 File Offset: 0x0003B9B3
        private void panel_ProgressShow_MouseUp(object sender, MouseEventArgs e)
        {
            this.OnMouseUp(e);
        }

        // Token: 0x06000337 RID: 823 RVA: 0x0003D7BE File Offset: 0x0003B9BE
        private void panel_ProgressShow_MouseMove(object sender, MouseEventArgs e)
        {
            this.OnMouseMove(e);
        }

        // Token: 0x06000338 RID: 824 RVA: 0x0003D7C9 File Offset: 0x0003B9C9
        private void SFC_BackColorChanged(object sender, EventArgs e)
        {
            this.reDraw(-1);
        }

        // Token: 0x06000339 RID: 825 RVA: 0x0003D7D4 File Offset: 0x0003B9D4
        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Token: 0x0600033A RID: 826 RVA: 0x0003D80C File Offset: 0x0003BA0C
        private void InitializeComponent()
        {
            this.components = new Container();
            this.ptb_showLine = new PictureBox();
            this.aTimer = new Timer(this.components);
            this.panel_ProgressShow = new Panel();
            this.lbl_JumpTag = new Label();
            this.lbl_info = new Label();
            ((ISupportInitialize)this.ptb_showLine).BeginInit();
            this.panel_ProgressShow.SuspendLayout();
            base.SuspendLayout();
            this.ptb_showLine.BackColor = Color.Transparent;
            this.ptb_showLine.Location = new Point(0, 0);
            this.ptb_showLine.Margin = new Padding(2);
            this.ptb_showLine.Name = "ptb_showLine";
            this.ptb_showLine.Size = new Size(62, 25);
            this.ptb_showLine.TabIndex = 2;
            this.ptb_showLine.TabStop = false;
            this.ptb_showLine.SizeChanged += this.ptb_showLine_SizeChanged;
            this.ptb_showLine.Click += this.ptb_showLine_Click;
            this.aTimer.Interval = 13;
            this.aTimer.Tick += this.aTimer_Tick;
            this.panel_ProgressShow.BackColor = Color.FloralWhite;
            this.panel_ProgressShow.BorderStyle = BorderStyle.FixedSingle;
            this.panel_ProgressShow.Controls.Add(this.lbl_JumpTag);
            this.panel_ProgressShow.Controls.Add(this.lbl_info);
            this.panel_ProgressShow.Location = new Point(52, 16);
            this.panel_ProgressShow.Margin = new Padding(2);
            this.panel_ProgressShow.Name = "panel_ProgressShow";
            this.panel_ProgressShow.Size = new Size(50, 20);
            this.panel_ProgressShow.TabIndex = 3;
            this.panel_ProgressShow.DoubleClick += this.panel_ProgressShow_Or_lbl_DoubleClick;
            this.panel_ProgressShow.MouseDown += this.panel_ProgressShow_MouseDown;
            this.panel_ProgressShow.MouseMove += this.panel_ProgressShow_MouseMove;
            this.panel_ProgressShow.MouseUp += this.panel_ProgressShow_MouseUp;
            this.lbl_JumpTag.AutoSize = true;
            this.lbl_JumpTag.Location = new Point(2, 9);
            this.lbl_JumpTag.Margin = new Padding(2, 0, 2, 0);
            this.lbl_JumpTag.Name = "lbl_JumpTag";
            this.lbl_JumpTag.Size = new Size(0, 9);
            this.lbl_JumpTag.TabIndex = 0;
            this.lbl_JumpTag.DoubleClick += this.panel_ProgressShow_Or_lbl_DoubleClick;
            this.lbl_info.AutoSize = true;
            this.lbl_info.Location = new Point(2, 0);
            this.lbl_info.Margin = new Padding(2, 0, 2, 0);
            this.lbl_info.Name = "lbl_info";
            this.lbl_info.Size = new Size(15, 9);
            this.lbl_info.TabIndex = 0;
            this.lbl_info.Text = "  ";
            this.lbl_info.DoubleClick += this.panel_ProgressShow_Or_lbl_DoubleClick;
            base.AutoScaleDimensions = new SizeF(5f, 9f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.panel_ProgressShow);
            base.Controls.Add(this.ptb_showLine);
            this.Font = new Font("宋体", 9f, FontStyle.Regular, GraphicsUnit.Pixel, 134);
            base.Margin = new Padding(2);
            base.Name = "SFC";
            base.Size = new Size(150, 59);
            base.Load += this.SimSFC_Load;
            base.BackColorChanged += this.SFC_BackColorChanged;
            base.SizeChanged += this.SimSFC_SizeChanged;
            base.ParentChanged += this.SFC_ParentChanged;
            ((ISupportInitialize)this.ptb_showLine).EndInit();
            this.panel_ProgressShow.ResumeLayout(false);
            this.panel_ProgressShow.PerformLayout();
            base.ResumeLayout(false);
        }

        // Token: 0x04000392 RID: 914
        private static List<ushort> _CategoryItems = new List<ushort>();

        // Token: 0x04000393 RID: 915
        protected internal List<NoticePoint> RevSource = new List<NoticePoint>();

        // Token: 0x04000394 RID: 916
        private static List<SFC> FoundSimSFC = new List<SFC>();

        // Token: 0x04000395 RID: 917
        private List<SFC> LastSimSFC = new List<SFC>();

        // Token: 0x04000396 RID: 918
        private List<SFC> NextSimSFC = new List<SFC>();

        // Token: 0x04000397 RID: 919
        private static SFC.GenSFCProgressEventHandle GenSimSFCProgressEvent;

        // Token: 0x04000398 RID: 920
        private SourceStyle _Source = SourceStyle.Cross;

        // Token: 0x04000399 RID: 921
        public static bool GetProgressInfoEn = false;

        // Token: 0x0400039A RID: 922
        public static bool OnlyDesignModeChangeFlowEn = true;

        // Token: 0x0400039B RID: 923
        private Style _SourceShow = Style.Left;

        // Token: 0x0400039C RID: 924
        private ushort _SourceDistance = 90;

        // Token: 0x0400039D RID: 925
        private ushort _SourceCrossNum = 1;

        // Token: 0x0400039E RID: 926
        private string _SourceJumpTag = "";

        // Token: 0x0400039F RID: 927
        private TargetStyle _Target = TargetStyle.Switch;

        // Token: 0x040003A0 RID: 928
        private Style _TargetShow = Style.Left;

        // Token: 0x040003A1 RID: 929
        private ushort _TargetDistance = 90;

        // Token: 0x040003A2 RID: 930
        private ushort _TargetCrossNum = 1;

        // Token: 0x040003A3 RID: 931
        private string _TargetJumpTag = "";

        // Token: 0x040003A4 RID: 932
        private static bool _AutoLocate = true;

        // Token: 0x040003A5 RID: 933
        private Color _RunColor = Color.LightGreen;

        // Token: 0x040003A6 RID: 934
        private ushort _Category;

        // Token: 0x040003A7 RID: 935
        private static DateTime LastMoveTim = DateTime.Now;

        // Token: 0x040003A8 RID: 936
        private object SelectCountLocker = new object();

        // Token: 0x040003A9 RID: 937
        protected internal int ProgressMid_X;

        // Token: 0x040003AA RID: 938
        private static List<int> MarkPauseRec = new List<int>();

        // Token: 0x040003AB RID: 939
        private bool[] PropetryIsSet = new bool[8];

        // Token: 0x040003AC RID: 940
        private static SFC.MethodClearHandler MethodClearEvent;

        // Token: 0x040003B1 RID: 945
        private bool NextStarPointIsSend = false;

        // Token: 0x040003B2 RID: 946
        private bool TickEventIsRun = false;

        // Token: 0x040003B3 RID: 947
        public object[] TV_Object = new object[10];

        // Token: 0x040003B4 RID: 948
        public bool[] TV_M = new bool[10];

        // Token: 0x040003B5 RID: 949
        public decimal[] TV_Data = new decimal[10];

        // Token: 0x040003B6 RID: 950
        public DateTime[] TV_Tim = new DateTime[10];

        // Token: 0x040003B7 RID: 951
        public int[] TV_Int = new int[10];

        // Token: 0x040003B8 RID: 952
        public DateTime FirstCircleTimAfterPause = DateTime.Now;

        // Token: 0x040003B9 RID: 953
        public bool FirstCircleFlagAfterPause = false;

        // Token: 0x040003BA RID: 954
        private bool MyMarkPause = false;

        // Token: 0x040003BB RID: 955
        private bool FirstCircleFlag = false;

        // Token: 0x040003BC RID: 956
        protected internal new bool Select = false;

        // Token: 0x040003BD RID: 957
        private DateTime FirstCircleTime = DateTime.Now;

        // Token: 0x040003BE RID: 958
        private static object aTimerLocker = new object();

        // Token: 0x040003BF RID: 959
        private Control MyOldParent = null;

        // Token: 0x040003C0 RID: 960
        private IContainer components = null;

        // Token: 0x040003C1 RID: 961
        private PictureBox ptb_showLine;

        // Token: 0x040003C2 RID: 962
        private Timer aTimer;

        // Token: 0x040003C3 RID: 963
        private Panel panel_ProgressShow;

        // Token: 0x040003C4 RID: 964
        private Label lbl_JumpTag;

        // Token: 0x040003C5 RID: 965
        private Label lbl_info;

        // Token: 0x02000036 RID: 54
        // (Invoke) Token: 0x06000343 RID: 835
        private delegate void GenSFCProgressEventHandle();

        // Token: 0x02000037 RID: 55
        // (Invoke) Token: 0x06000347 RID: 839
        private delegate void MethodClearHandler();

        // Token: 0x02000038 RID: 56
        // (Invoke) Token: 0x0600034B RID: 843
        private delegate void InitdHandler();

        // Token: 0x02000039 RID: 57
        // (Invoke) Token: 0x0600034F RID: 847
        private delegate void MarkInitdHandler(ushort mark);

        // Token: 0x0200003A RID: 58
        // (Invoke) Token: 0x06000353 RID: 851
        private delegate void MarkPauseHandler(ushort mark, bool pause);

        // Token: 0x0200003B RID: 59
        // (Invoke) Token: 0x06000357 RID: 855
        public delegate short SFCRunHandler(bool FirstCircleFlag, DateTime FirstCircleTime, SFC CurrentSFC);
    }



}
