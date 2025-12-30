using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SimpleMES.Services.DAL;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SimpleMES.Core;
using System.Windows.Threading;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using System;

namespace SimpleMES.ViewModels
{
    public partial class ReportViewModel : ViewModelBase
    {
        private readonly IDataRepository _dbService;
        private readonly ProductionRecordModel _record;
        private readonly DispatcherTimer _chartTimer;
        private bool _isRefreshing;
        private readonly ObservableCollection<int> _runningValues = new ObservableCollection<int> { 0 };
        private readonly ObservableCollection<int> _stoppedValues = new ObservableCollection<int> { 0 };
        private PieSeries<int> _runningSeries;
        private PieSeries<int> _stoppedSeries;
        public ReportViewModel(IDbService dbService, DeviceCommunicationService service)
        {
            _dbService = new DataRepository(dbService);
            service.OnDeviceStatusChanged += Service_OnDeviceStatusChanged;
            // 初始化图表绑定集合
            ChartTValues = new ObservableCollection<double>();
            ChartPValues = new ObservableCollection<double>();
            ChartSValues = new ObservableCollection<double>();
            TimeValues = new ObservableCollection<string>();
            ChinesTextPaint = new SolidColorPaint()
            {
                Color = SKColors.Black,
                SKTypeface = SKTypeface.FromFamilyName("宋体")
            };
            DeviceValues = new ObservableCollection<DeviceDto>();
            LineSeries = new ISeries[]
            {
                new LineSeries<double>()
                {
                    Values = ChartTValues,
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Name = "实时温度",
                    
                },
                new LineSeries<double>()
                {
                    Values = ChartPValues,
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Name = "实时压力"
                },
                new LineSeries<double>()
                {
                    Values = ChartSValues,
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Name = "实时转速"
                }
            };
            XAxes = new Axis[]
            {
                new Axis()
                {
                    Name = "收集时间",
                    Labels = TimeValues,
                    LabelsRotation = 45, // 标签斜着放，防止拥挤
                    TextSize = 10,
                    LabelsPaint = ChinesTextPaint,
                    NamePaint = ChinesTextPaint
                }
            };
            YAxes = new Axis[]
            {
                new Axis()
                {
                    Name = "℃/Bar/Rpm",
                    TextSize = 10,
                    LabelsPaint = ChinesTextPaint,
                    NamePaint = ChinesTextPaint
                }
            };
            _runningSeries = new PieSeries<int>()
            {
                Values = _runningValues,
                Name = "运行中",
                Fill = new SolidColorPaint(SKColors.Green)
            };
            _stoppedSeries = new PieSeries<int>()
            {
                Values = _stoppedValues,
                Name = "停机/故障",
                Fill = new SolidColorPaint(SKColors.Red)
            };
            PieSeries = new ISeries[]
            {
                _runningSeries,
                _stoppedSeries
            };
            _chartTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _chartTimer.Tick += async (s, e) => await RefreshCharts();
            _chartTimer.Start();
            _ = RefreshCharts();
        }

        [ObservableProperty] private ISeries[] _pieSeries;
        [ObservableProperty] private ISeries[] _lineSeries;
        [ObservableProperty] private Axis[] _xAxes;
        [ObservableProperty] private Axis[] _yAxes;
        //实时数据
        [ObservableProperty] private ObservableCollection<DeviceDto> _deviceValues;
        [ObservableProperty] private ObservableCollection<double> _chartTValues;
        [ObservableProperty] private ObservableCollection<double> _chartPValues;
        [ObservableProperty] private ObservableCollection<double> _chartSValues;
        [ObservableProperty] private ObservableCollection<string> _timeValues;

        private int _isRunningValues;
        private int _isStoppedValues;
        private int _isFaultValues;
        public SolidColorPaint? ChinesTextPaint { get; set; }


        private void Service_OnDeviceStatusChanged(List<DeviceDto> listDeviceDto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DeviceValues.Count == 0)
                {
                    foreach (var dto in listDeviceDto)
                    {
                        DeviceValues.Add(dto);
                    }
                    _isRunningValues = DeviceValues.Count(dto => dto.Status == "Running");
                    _isStoppedValues = DeviceValues.Count(dto => dto.Status == "Stopped");
                    _isFaultValues = DeviceValues.Count(dto => dto.Status == "Fault");
                    _runningValues[0] = _isRunningValues;
                    _stoppedValues[0] = _isStoppedValues + _isFaultValues;
                }
                else
                {
                    foreach (var newDeviceDto in listDeviceDto)
                    {
                        var oldDeviceDto = DeviceValues.FirstOrDefault(d => d.DeviceId == newDeviceDto.DeviceId);
                        if (oldDeviceDto != null)
                        {
                            oldDeviceDto.Temperature = newDeviceDto.Temperature;
                            oldDeviceDto.Pressure = newDeviceDto.Pressure;
                            oldDeviceDto.Speed = newDeviceDto.Speed;
                            oldDeviceDto.Status = newDeviceDto.Status;
                        }
                    }
                    _isRunningValues = DeviceValues.Count(dto => dto.Status == "Running");
                    _isStoppedValues = DeviceValues.Count(dto => dto.Status == "Stopped");
                    _isFaultValues = DeviceValues.Count(dto => dto.Status == "Fault");
                    PieSeries = new ISeries[]
                    {
                        new PieSeries<int>()
                        {
                            Values = new int[] { _isRunningValues },
                            Name = "运行中",
                            Fill = new SolidColorPaint(SKColors.Green)
                        },
                        new PieSeries<int>()
                        {
                            Values = new int[] { _isStoppedValues + _isFaultValues },
                            Name = "停机/故障",
                            Fill = new SolidColorPaint(SKColors.Red)
                        },
                    };
                }
            });
        }

        [RelayCommand]
        private async Task RefreshCharts()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            try
            {
                var records = await _dbService.GetRecentRecordsAsync(1);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (records != null)
                    {
                        ChartTValues.Add((double?)records.Temperature ?? 0);
                        ChartPValues.Add((double?)records.Pressure ?? 0);
                        ChartSValues.Add((double?)records.Speed ?? 0);
                        TimeValues.Add(records.RecordTime.ToString("HH:mm:ss"));
                        if (ChartTValues.Count > 20)
                        {
                            ChartTValues.RemoveAt(0);
                            ChartPValues.RemoveAt(0);
                            ChartSValues.RemoveAt(0);
                            TimeValues.RemoveAt(0);
                        }
                    }
                });
            }
            finally
            {
                _isRefreshing = false;
            }
        }
    }
}
