using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public class ProductionRecordModel
    {
        public long RecordId { get; set; }
        public int DeviceId { get; set; }
        public decimal? Temperature { get; set; } // SQL decimal 对应 C# decimal
        public decimal? Pressure { get; set; }
        public int? Speed { get; set; }
        public DateTime RecordTime { get; set; }
    }
}
