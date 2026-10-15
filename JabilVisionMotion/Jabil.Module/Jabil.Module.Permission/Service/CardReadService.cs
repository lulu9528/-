using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// 卡片读取服务类
    /// 负责处理卡片ID的读取和设置操作，是读卡器功能的核心服务
    /// 提供卡片ID的获取和更新接口，用于权限验证等场景
    /// </summary>
    public class CardReadService
    {
        /// <summary>
        /// 卡片ID私有字段
        /// 存储当前读取到的卡片唯一标识
        /// </summary>
        string _id;

        /// <summary>
        /// 读取卡片ID
        /// 获取当前存储的卡片唯一标识
        /// </summary>
        /// <returns>卡片ID字符串，如果未设置则返回null</returns>
        public string ReadID()
        {
            return _id;
        }

        /// <summary>
        /// 设置卡片ID
        /// 更新存储的卡片唯一标识，通常由读卡器硬件交互逻辑调用
        /// </summary>
        /// <param name="val">新的卡片ID值</param>
        public void SetId(string val)
        {
            _id = val;
        }
    }
}