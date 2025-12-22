using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleMES.Models.Dto
{
    public partial class DeviceDto:ObservableObject
    {
        [ObservableProperty] private int _deviceId;
        [ObservableProperty] private string _deviceName;
        [ObservableProperty] private string _ipAddress;
        [ObservableProperty] private string _serialPort;
        [ObservableProperty] private decimal? _temperature;
        [ObservableProperty] private decimal? _pressure;
        [ObservableProperty] private int _speed;
        [ObservableProperty] private string _status;
        [ObservableProperty] private DateTime _lastUpdateTime;

        
    }
}
