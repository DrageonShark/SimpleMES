using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public class OrderModel
    {
        public string OrderNo { get; set; }
        public string ProductCode { get; set; }
        public int PlanQty { get; set; }
        public int CompletedQty { get; set; }

        // 对应数据库的 int 类型，代码中使用 int，逻辑处理时强转枚举
        public int OrderStatus { get; set; }

        // 辅助属性：方便界面显示文字状态 (非数据库字段)
        public string StatusText => ((OrderStatus)this.OrderStatus).ToString();

        public DateTime? StartTime { get; set; } // 允许为 null
        public DateTime? EndTime { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
