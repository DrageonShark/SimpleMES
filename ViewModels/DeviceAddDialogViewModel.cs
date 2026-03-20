using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using System.Globalization;

namespace SimpleMES.ViewModels
{
    partial class DeviceAddDialogViewModel : DialogViewModelBase
    {
        private readonly Func<DeviceModel, Task<bool>> _saveAsync;
        private readonly Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? _testAsync;
        private readonly DeviceModel _model;

        public DeviceAddDialogViewModel(
            DeviceModel model,
            Func<DeviceModel, Task<bool>> saveAsync,
            Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? testAsync = null)
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

        public string DeviceCode
        {
            get => _model.DeviceCode ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.DeviceCode == next) return;
                _model.DeviceCode = next;
                OnPropertyChanged();
            }
        }

        public string DeviceType
        {
            get => _model.DeviceType ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.DeviceType == next) return;
                _model.DeviceType = next;
                OnPropertyChanged();
            }
        }

        public string WorkshopName
        {
            get => _model.WorkshopName ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.WorkshopName == next) return;
                _model.WorkshopName = next;
                OnPropertyChanged();
            }
        }

        public string LineName
        {
            get => _model.LineName ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.LineName == next) return;
                _model.LineName = next;
                OnPropertyChanged();
            }
        }

        public string StationName
        {
            get => _model.StationName ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.StationName == next) return;
                _model.StationName = next;
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

        public int CriticalityIndex
        {
            get => Math.Clamp((_model.Criticality <= 0 ? 2 : _model.Criticality) - 1, 0, 2);
            set
            {
                var next = (byte)Math.Clamp(value + 1, 1, 3);
                if (_model.Criticality == next) return;
                _model.Criticality = next;
                OnPropertyChanged();
            }
        }

        public string SortOrderText
        {
            get => _model.SortOrder.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var next))
                {
                    next = 0;
                }

                if (_model.SortOrder == next) return;
                _model.SortOrder = next;
                OnPropertyChanged();
            }
        }

        public string Remark
        {
            get => _model.Remark ?? string.Empty;
            set
            {
                var next = value ?? string.Empty;
                if (_model.Remark == next) return;
                _model.Remark = next;
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            var ok = await _saveAsync(_model);
            if (ok)
            {
                Close(true);
            }
        }

        private bool CanTestConnection() => _testAsync is not null;

        [RelayCommand(CanExecute = nameof(CanTestConnection))]
        private async Task TestConnectionAsync()
        {
            if (_testAsync is null)
            {
                return;
            }

            var result = await _testAsync(_model);
            ShowMessage(result.IsSuccess ? "连接测试成功" : "连接测试失败", result.Message, result.IsSuccess);
        }
    }
}
