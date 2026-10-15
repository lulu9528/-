using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.IO.Lib
{
    /// <summary>
    /// 用于跟踪IO状态变化并判断状态是否稳定保持指定时长的工具类
    /// </summary>
    public class IOStateKeep
    {
        /// <summary>
        /// 记录最后一次状态变化的时间（使用UTC时间避免本地时间偏移问题）
        /// </summary>
        private DateTime _lastStateChangeTime;

        /// <summary>
        /// 当前IO状态的私有字段
        /// </summary>
        private bool _state;

        /// <summary>
        /// 获取或设置当前IO状态
        /// 状态变化时会更新最后一次状态变化时间
        /// </summary>
        public bool State
        {
            set
            {
                if (value != _state)
                {
                    _lastStateChangeTime = DateTime.UtcNow;
                }
                _state = value;
            }
            get => _state;
        }

        /// <summary>
        /// 初始化IOStateKeep实例，默认初始状态为false
        /// </summary>
        public IOStateKeep()
        {
            _state = false;
            _lastStateChangeTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 初始化IOStateKeep实例并指定初始状态
        /// </summary>
        /// <param name="initialState">初始IO状态</param>
        public IOStateKeep(bool initialState)
        {
            _state = initialState;
            _lastStateChangeTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 检查当前状态是否为On且已稳定保持指定毫秒数
        /// </summary>
        /// <param name="durationMilliseconds">需要保持的毫秒数</param>
        /// <returns>如果状态为On且持续时间达标则返回true，否则返回false</returns>
        public bool StandOn(int durationMilliseconds)
        {
            return _state && GetCurrentStateDuration() > durationMilliseconds;
        }

        /// <summary>
        /// 检查当前状态是否为Off且已稳定保持指定毫秒数
        /// </summary>
        /// <param name="durationMilliseconds">需要保持的毫秒数</param>
        /// <returns>如果状态为Off且持续时间达标则返回true，否则返回false</returns>
        public bool StandOff(int durationMilliseconds)
        {
            return !_state && GetCurrentStateDuration() > durationMilliseconds;
        }

        /// <summary>
        /// 获取当前状态已持续的毫秒数
        /// </summary>
        /// <returns>当前状态持续的毫秒数</returns>
        private double GetCurrentStateDuration()
        {
            return (DateTime.UtcNow - _lastStateChangeTime).TotalMilliseconds;
        }

        /// <summary>
        /// 获取上升沿状态（待实现）
        /// </summary>
        /// <returns>上升沿状态标识</returns>
        public bool GetUpBridge()
        {
            throw new NotImplementedException("GetUpBridge方法尚未实现");
        }

        /// <summary>
        /// 获取下降沿状态（待实现）
        /// </summary>
        /// <returns>下降沿状态标识</returns>
        public bool GetDnBridge()
        {
            throw new NotImplementedException("GetDnBridge方法尚未实现");
        }
    }
}
