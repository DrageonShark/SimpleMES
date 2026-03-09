namespace SimpleMES.Services.Observer
{
    public enum ConfigChangeType
    {
        /// <summary>
        /// 设备添加
        /// </summary>
        Added,
        /// <summary>
        /// 设备信息修改
        /// </summary>
        Updated,
        /// <summary>
        /// 设备删除
        /// </summary>
        Deleted,
        /// <summary>
        /// 停用
        /// </summary>
        Disabled,
        /// <summary>
        /// 启用
        /// </summary>
        Enabled
    }
}
