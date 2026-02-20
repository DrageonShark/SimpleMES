namespace SimpleMES.Services.State
{
    /// <summary>
    /// 设备轮询结果记录类
    /// 用于封装设备轮询（数据采集）操作的结果信息，采用record类型保证不可变性
    /// </summary>
    /// <param name="IsSuccess">轮询操作是否成功</param>
    /// <param name="RawData">从设备获取的原始数据（16位无符号整数数组），操作失败时可为null</param>
    /// <param name="Exception">操作失败时的异常信息，无异常时为null（可选参数，默认值null）</param>
    /// <param name="Timestamp">轮询操作发生的时间戳，未指定时使用当前时间（可选参数，默认值null）</param>
    public record DevicePollResult(
        bool IsSuccess,
        ushort[]? RawData,
        Exception? Exception = null,
        DateTime? Timestamp = null)
    {
        /// <summary>
        /// 轮询操作实际发生的时间
        /// 如果传入了Timestamp则使用该值，否则使用对象创建时的当前时间
        /// </summary>
        public DateTime OccurredAt { get; } = Timestamp ?? DateTime.Now;
    }
}
