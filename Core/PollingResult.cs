using SimpleMES.Models.Dto;
using SimpleMES.Services.State;

namespace SimpleMES.Core
{
    public record PollingResult(
        DevicePollResult PollResult,
        DeviceDto? Snapshot = null,
        PersistCallback? PersistAsync = null);
}
