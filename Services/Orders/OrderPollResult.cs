namespace SimpleMES.Services.Orders
{
    public record class OrderPollResult
    {
        public string OrderNo { get; }
        public OrderState Operation { get; }
        public bool IsSuccess { get; }
        public int? CompletedQtyDelta { get; }//完成数量的变化值
        public string? Comment { get; }//备注
        public Exception? Exception { get; }
        public DateTime? OccurredAt { get; }

        public OrderPollResult(string orderNo,
            OrderState operation, bool isSuccess,
            int? completedQtyDelta = null, string? comment = null,
            Exception? exception = null, DateTime? occurredAt = null)
        {
            OrderNo = orderNo;
            Operation = operation;
            IsSuccess = isSuccess;
            CompletedQtyDelta = completedQtyDelta;
            Comment = comment;
            Exception = exception;
            OccurredAt = occurredAt ?? DateTime.Now;
        }
        public static OrderPollResult Success(
            string orderNo,
            OrderState operation,
            int? completedQtyDelta = null,
            string? comment = null,
            DateTime? occurredAt = null)
        {
            return new(orderNo, operation, true, completedQtyDelta, comment, null, occurredAt);
        }
        public static OrderPollResult Fail(
            string orderNo,
            OrderState operation,
            int? completedQtyDelta = null,
            string? comment = null,
            DateTime? occurredAt = null)
        {
            return new(orderNo, operation, false, completedQtyDelta, comment, null, occurredAt);
        }
    }
}
