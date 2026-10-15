using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Modbus.Entity
{
    /// <summary>
    /// 基础实体类，所有业务实体类的基类
    /// 封装实体通用属性与行为，实现代码复用与标准化
    /// 与BaseDto保持属性对齐，便于AutoMapper映射
    /// </summary>
    [Serializable]
    public class BaseEntity : INotifyPropertyChanged
    {
        private int _id;
        private string _equipmentName;
        private DateTime _createTime = DateTime.Now;
        private DateTime _updateTime = DateTime.Now;

        /// <summary>
        /// 主键ID，自增
        /// 数据库表唯一标识，用于CRUD操作的主键关联
        /// </summary>
        [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
        [JsonProperty("id")]
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 设备名称
        /// 业务唯一标识，用于设备的人性化识别与关联查询
        /// 长度限制：100字符，非空约束
        /// </summary>
        [SugarColumn(ColumnName = "设备名称", Length = 100, IsNullable = false)]
        [JsonProperty("equipmentName")]
        public string 设备名称
        {
            get => _equipmentName;
            set => SetProperty(ref _equipmentName, value);
        }

        /// <summary>
        /// 创建时间
        /// 记录实体创建的时间戳，默认为系统当前时间
        /// 用于数据审计与追踪
        /// </summary>
        [SugarColumn(
            ColumnName = "创建时间",
            IsNullable = false,
            DefaultValue = "GETDATE()",
            ColumnDescription = "记录创建时间戳"
        )]
        [JsonProperty("createTime")]
        public DateTime 创建时间
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 更新时间
        /// 记录实体最后修改的时间戳，自动更新
        /// 用于数据变更追踪
        /// </summary>
        [SugarColumn(
            ColumnName = "更新时间",
            IsNullable = false,
            DefaultValue = "GETDATE()",
            ColumnDescription = "记录最后更新时间戳",
            IsOnlyIgnoreInsert = true // 插入时不更新，仅更新时生效
        )]
        [JsonProperty("updateTime")]
        public DateTime 更新时间
        {
            get => _updateTime;
            set => SetProperty(ref _updateTime, value);
        }

        /// <summary>
        /// 是否启用
        /// 标识该点位是否启用监控和控制
        /// </summary>
        [SugarColumn(ColumnName = "是否启用", IsNullable = false, ColumnDescription = "是否启用该点位：1-启用，0-禁用")]
        public bool 是否启用 { get; set; } = true;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用成员名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // 更新时自动刷新UpdateTime
            if (propertyName != nameof(更新时间) &&
                propertyName != nameof(创建时间) &&
                !IsNew()) // 新增时不更新
            {
                更新时间 = DateTime.Now;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 安全设置属性值并触发变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">存储字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否发生了值变更</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            // 字符串空值统一处理
            if (typeof(T) == typeof(string) && value == null)
            {
                value = (T)(object)string.Empty;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 检查是否为新实体（ID未赋值）
        /// </summary>
        /// <returns>是否为新实体</returns>
        public virtual bool IsNew() => ID <= 0;

        /// <summary>
        /// 重写ToString方法，返回实体的JSON表示
        /// 便于日志记录与调试
        /// </summary>
        /// <returns>实体的JSON字符串</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// 复制基础属性到目标实体
        /// </summary>
        /// <param name="target">目标实体</param>
        protected virtual void CopyBaseProperties(BaseEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.ID = ID;
            target.设备名称 = 设备名称;
            target.创建时间 = 创建时间;
            target.更新时间 = 更新时间;
        }
    }
}