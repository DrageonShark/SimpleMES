using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NModbus.IO;

namespace SimpleMES.Core
{
    /// <summary>
    /// 将 <see cref="SerialPort"/> 适配为 NModbus 所需的 <see cref="IStreamResource"/>。
    /// 作用：为 Modbus RTU Master 提供统一的字节流读写、超时与缓冲区控制接口。
    /// </summary>
    public class SerialPortAdapter:IStreamResource
    {
        private readonly SerialPort _serialPort;

        public SerialPortAdapter(SerialPort serialPort)
        {
            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        }

        /// <summary>
        /// 释放串口资源。
        /// 说明：调用后串口句柄被释放；通常在连接生命周期结束或对象被回收时调用。
        /// 注意：适配器并不负责 Close 打开状态，<see cref="SerialPort.Dispose"/> 会处理必要地清理。
        /// </summary>
        public void Dispose()
        {
            _serialPort?.Dispose();
        }

        /// <summary>
        /// 清空输入缓冲区。
        /// 作用：丢弃尚未读取的接收数据，避免旧数据影响下一帧的 Modbus 解析。
        /// 常见时机：发送请求前或发生解析错误后，确保缓冲区干净。
        /// </summary>
        public void DiscardInBuffer()
        {
            if(_serialPort.IsOpen)
                _serialPort.DiscardInBuffer();
        }

        /// <summary>
        /// 从串口读取指定数量的字节到缓冲区。
        /// 作用：为 Modbus Master 提供底层字节读取，配合 <see cref="ReadTimeout"/> 控制等待时间。
        /// 返回值：实际读取的字节数；若超时可能抛出异常。
        /// </summary>
        /// <param name="buffer">目标缓冲区。</param>
        /// <param name="offset">在缓冲区中开始写入数据的位置。</param>
        /// <param name="count">计划读取的字节数。</param>
        /// <returns>实际读取到的字节数。</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            return _serialPort.IsOpen
                    ? throw new InvalidOperationException("串口未连接")
                    : _serialPort.Read(buffer, offset, count)
                ;
        }

        /// <summary>
        /// 将缓冲区中的字节写入串口。
        /// 作用：发送 Modbus RTU 帧（含地址、功能码、数据、CRC）。
        /// 配合 <see cref="WriteTimeout"/> 控制写入阻塞时间。
        /// </summary>
        /// <param name="buffer">要写入的数据源缓冲区。</param>
        /// <param name="offset">在缓冲区中开始读取数据的位置。</param>
        /// <param name="count">要写入的字节数。</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (_serialPort.IsOpen)
                throw new InvalidOperationException("串口未连接");
            _serialPort.Write(buffer, offset, count);
        }

        /// <summary>
        /// 表示“无限超时”的常量值。
        /// 用途：供上层（如 Modbus Master）设置读写操作为无超时。
        /// 来源：转发自 <see cref="SerialPort.InfiniteTimeout"/>。
        /// </summary>
        public int InfiniteTimeout => SerialPort.InfiniteTimeout;
        /// <summary>
        /// 读取操作的超时时间（毫秒）。
        /// 作用：控制 <see cref="Read(byte[], int, int)"/> 等读取在超过该时间未收到数据时抛出超时异常
        /// 使用场景：RTU 请求后等待从站响应；若没有响应则触发超时用于重试或错误处理。
        /// </summary>
        public int ReadTimeout { get=> _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        /// <summary>
        /// 写入操作的超时时间（毫秒）。
        /// 作用：控制 <see cref="Write(byte[], int, int)"/> 在写入阻塞超过该时间时抛出超时异常。
        /// 使用场景：在串口拥堵或硬件异常时避免无期限阻塞。
        /// </summary>
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
    }
}
