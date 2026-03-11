using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;

namespace SimpleMES.ViewModels
{
    partial class DeviceAddDialogViewModel : DialogViewModelBase
    {
        private readonly Func<DeviceModel, Task<bool>> _saveAsync;
        private readonly Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? _testAsync;
        private readonly DeviceModel _model;

        public DeviceAddDialogViewModel(
            DeviceModel model, Func<DeviceModel,
                Task<bool>> saveAsync, Func<DeviceModel,
                Task<(bool IsSuccess, string Message)>>? testAsync = null)
        {
            _model = model ?? new DeviceModel();
            _saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
            _testAsync = testAsync;
        }
        public string DeviceName
        {
            get => _model.DeviceName ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.DeviceName == next) return;
                _model.DeviceName = next;
                OnPropertyChanged();
            }
        }

        public string IpAddress
        {
            get => _model.IpAddress ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.IpAddress == next) return;
                _model.IpAddress = next;
                OnPropertyChanged();
            }
        }

        public int? Port
        {
            get => _model.Port;
            set
            {
                if (_model.Port == value) return;
                _model.Port = value;
                OnPropertyChanged();
            }
        }

        public string SerialPort
        {
            get => _model.SerialPort ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.SerialPort == next) return;
                _model.SerialPort = next;
                OnPropertyChanged();
            }
        }

        public byte? SlaveId
        {
            get => _model.SlaveId;
            set
            {
                if (_model.SlaveId == value) return;
                _model.SlaveId = value;
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            var ok = await _saveAsync(_model);
            if (ok) Close(true);
        }

        private bool CanTestConnection() => _testAsync is not null;

        [RelayCommand(CanExecute = nameof(CanTestConnection))]
        private async Task TestConnectionAsync()
        {
            if (_testAsync is null) return;
            var result = await _testAsync(_model);
            ShowMessage(result.IsSuccess ? "连接测试成功" : "连接测试失败", result.Message, result.IsSuccess);
        }
    }
}
