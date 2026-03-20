using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Services.State;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace SimpleMES.ViewModels
{
    public partial class ReportViewModel : DialogViewModelBase, IDisposable
    {
        private readonly IDataRepository _dbService;
        private readonly ProductionRecordModel _record;
        private readonly DispatcherTimer _chartTimer;
        private readonly Dispatcher _dispatcher;
        private readonly IDeviceStatusNotifier _notifier;
        private bool _disposed;
        private bool _isRefreshing;
        private readonly ObservableCollection<int> _runningValues = new ObservableCollection<int> { 0 };
        private readonly ObservableCollection<int> _stoppedValues = new ObservableCollection<int> { 0 };
        private PieSeries<int> _runningSeries;
        private PieSeries<int> _stoppedSeries;
        public ReportViewModel(IDbService dbService, Dispatcher dispatcher, IDeviceStatusNotifier notifier)
        {
            var font = SKTypeface.FromFamilyName("Microsoft YaHei");
            _dispatcher = dispatcher;
            _notifier = notifier;
            _dbService = new DataRepository(dbService);
            _notifier.DeviceStatusChanged += OnDeviceStatusChanged;

            // 1. 坐标轴画笔 (黑色)
            AxisPaint = new SolidColorPaint
            {
                Color = SKColors.Black,
                SKTypeface = font,
                IsAntialias = true // 开启抗锯齿，文字更平滑
            };
            // 2. 图例画笔 (深灰色，看起来更有层次)
            LegendPaint = new SolidColorPaint
            {
                Color = SKColors.DarkSlateGray,
                SKTypeface = font,
                IsAntialias = true
            };

            // 3. 提示框文字画笔 (比如设为浅色，搭配深色背景；或者深色搭配浅色背景)
            // 这里为了稳妥，我们用深色文字
            TooltipTextPaint = new SolidColorPaint
            {
                Color = SKColors.Black, // 提示文字颜色
                SKTypeface = font,
                IsAntialias = true
            };

            // 4. 提示框背景画笔 (白色背景，带一点透明度)
            TooltipBgPaint = new SolidColorPaint
            {
                Color = SKColors.White.WithAlpha(240) // 略微透明的白色背景
            };
            // 初始化图表绑定集合
            ChartTValues = new ObservableCollection<double>();
            ChartPValues = new ObservableCollection<double>();
            ChartSValues = new ObservableCollection<double>();
            TimeValues = new ObservableCollection<string>();
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
                    LabelsPaint = AxisPaint,
                    NamePaint = AxisPaint
                }
            };
            YAxes = new Axis[]
            {
                new Axis()
                {
                    Name = "℃/Bar/Rpm",
                    TextSize = 10,
                    LabelsPaint = AxisPaint,
                    NamePaint = AxisPaint
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

        public SolidColorPaint AxisPaint { get; set; }      // 坐标轴字体
        public SolidColorPaint LegendPaint { get; set; }    // 图例字体
        public SolidColorPaint TooltipTextPaint { get; set; } // 提示框字体
        public SolidColorPaint TooltipBgPaint { get; set; }   // 提示框背景

        private void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var listDeviceDto = e.LatestDevices;
                if (DeviceValues.Count == 0)
                {
                    foreach (var dto in listDeviceDto)
                    {
                        DeviceValues.Add(dto);
                    }
                    _isRunningValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Running);
                    _isStoppedValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Disconnected);
                    _isFaultValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Fault);
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
                            oldDeviceDto.DeviceState = newDeviceDto.DeviceState;
                        }
                    }
                    _isRunningValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Running);
                    _isStoppedValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Disconnected);
                    _isFaultValues = DeviceValues.Count(dto => dto.DeviceState == DeviceState.Fault);
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

        public void Dispose()
        {
            if (_disposed) return;
            _notifier.DeviceStatusChanged -= OnDeviceStatusChanged;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
