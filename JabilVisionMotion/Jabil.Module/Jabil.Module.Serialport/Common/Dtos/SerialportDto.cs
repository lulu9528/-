using Jabil.Lib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Serialport.Common.Dtos
{
    public class SerialportDto : BaseDto
    {

        private int _波特率;
        public int 波特率
        {
            get => _波特率;
            set
            {
                _波特率 = value;
                OnPropertyChanged();
            }
        }


        private int _校验位;
        public int 校验位
        {
            get => _校验位;
            set
            {
                _校验位 = value;
                OnPropertyChanged();
            }
        }


        private int _数据位;
        public int 数据位
        {
            get => _数据位;
            set
            {
                _数据位 = value;
                OnPropertyChanged();
            }
        }


        private int _停止位;
        public int 停止位
        {
            get => _停止位;
            set
            {
                _停止位 = value;
                OnPropertyChanged();
            }
        }


        private int _超时;
        public int 超时
        {
            get => _超时;
            set
            {
                _超时 = value;
                OnPropertyChanged();
            }
        }
    }
}
