using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public class DeviceModel
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public int? Port { get; set; }
        public string SerialPort { get; set; }
        public byte SlaveId { get; set; }
        public string Status { get; set; }

        public DateTime LastUpdateTime { get; set; }
    }
}
