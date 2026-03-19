using SimpleMES.Services.Observer;
using SimpleMES.Services.UI;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public sealed class DeviceWorkspaceRealtimeBridge : IDisposable
    {
        private readonly IDeviceStatusNotifier _statusNotifier;
        private readonly IUiDispatcher _uiDispatcher;
        private readonly DeviceWorkspaceState _workspaceState;
        private bool _disposed;

        public DeviceWorkspaceRealtimeBridge(
            IDeviceStatusNotifier statusNotifier,
            DeviceWorkspaceState workspaceState,
            IUiDispatcher uiDispatcher)
        {
            _statusNotifier = statusNotifier;
            _workspaceState = workspaceState;
            _uiDispatcher = uiDispatcher;

            _statusNotifier.DeviceStatusChanged += OnDeviceStatusChanged;
        }

        private void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            _uiDispatcher.Invoke(() =>
            {
                _workspaceState.ApplyLatestDeviceSnapshot(e.LatestDevices);
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            _statusNotifier.DeviceStatusChanged -= OnDeviceStatusChanged;
            _disposed = true;
        }
    }
}
