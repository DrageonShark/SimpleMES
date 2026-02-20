namespace SimpleMES.Models
{
    public class OrderModel
    {
        public string OrderNo { get; set; }
        public string ProductCode { get; set; }
        public int PlanQty { get; set; }
        public int CompletedQty { get; set; }
        public string OrderStatus { get; set; }
        public DateTime? StartTime { get; set; } // 允许为 null
        public DateTime? EndTime { get; set; }
        public DateTime? LastOperationTime { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
