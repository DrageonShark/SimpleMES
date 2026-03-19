using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models.Dto;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceManagementViewModel : DialogViewModelBase, IDisposable
    {
        private readonly IDeviceWorkspaceActions _actions;

        [ObservableProperty]
        private int _pageSize = 12;

        [ObservableProperty]
        private int _currentPage = 1;

        public DeviceManagementViewModel(DeviceWorkspaceState state, IDeviceWorkspaceActions actions)
        {
            State = state;
            _actions = actions;
            PageTitle = "设备管理";

            State.FilteredDeviceDto.CollectionChanged += OnFilteredDevicesChanged;
            PageSizeOptions = new ObservableCollection<int> { 8, 12, 16, 24 };
            RefreshVisibleDevices();
        }

        public DeviceWorkspaceState State { get; }
        public IDeviceWorkspaceActions Actions => _actions;
        public ObservableCollection<DeviceDto> VisibleDevices { get; } = new();
        public ObservableCollection<int> PageSizeOptions { get; }

        public int TotalPages =>
            State.FilteredDeviceDto.Count == 0
                ? 1
                : (int)Math.Ceiling((double)State.FilteredDeviceDto.Count / PageSize);

        public bool HasMultiplePages => State.FilteredDeviceDto.Count > PageSize;
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        public int VisibleStartIndex => State.FilteredDeviceDto.Count == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
        public int VisibleEndIndex => State.FilteredDeviceDto.Count == 0 ? 0 : VisibleStartIndex + VisibleDevices.Count - 1;

        public string PagingSummary =>
            State.FilteredDeviceDto.Count == 0
                ? "暂无设备可展示"
                : $"第 {CurrentPage}/{TotalPages} 页，当前显示 {VisibleDevices.Count} / 共 {State.FilteredDeviceDto.Count} 台设备";

        public string VisibleRangeSummary =>
            State.FilteredDeviceDto.Count == 0
                ? "当前没有可浏览的设备"
                : $"正在查看第 {VisibleStartIndex}-{VisibleEndIndex} 台，共 {State.FilteredDeviceDto.Count} 台";

        partial void OnPageSizeChanged(int value)
        {
            CurrentPage = 1;
            RefreshVisibleDevices();
        }

        partial void OnCurrentPageChanged(int value)
        {
            RefreshVisibleDevices();
        }

        [RelayCommand]
        private Task AddDevice()
        {
            return _actions.AddDeviceAsync();
        }

        [RelayCommand]
        private Task EditDevice(DeviceDto? device)
        {
            return _actions.EditDeviceConfigAsync(device);
        }

        [RelayCommand]
        private Task ToggleDevice(DeviceDto? device)
        {
            return _actions.ToggleDeviceEnabledAsync(device);
        }

        [RelayCommand]
        private void ResetFilters()
        {
            State.ResetFilters();
            CurrentPage = 1;
            RefreshVisibleDevices();
        }

        [RelayCommand]
        private void RefreshView()
        {
            State.RefreshDeviceFilter();
            RefreshVisibleDevices();
        }

        [RelayCommand]
        private void FilterAll()
        {
            State.StateFilter = "全部";
        }

        [RelayCommand]
        private void FilterRunning()
        {
            State.StateFilter = "运行";
        }

        [RelayCommand]
        private void FilterDisconnected()
        {
            State.StateFilter = "断连";
        }

        [RelayCommand]
        private void FilterFault()
        {
            State.StateFilter = "故障";
        }

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private void PreviousPage()
        {
            if (!CanGoPrevious) return;
            CurrentPage--;
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextPage()
        {
            if (!CanGoNext) return;
            CurrentPage++;
        }

        public void Dispose()
        {
            State.FilteredDeviceDto.CollectionChanged -= OnFilteredDevicesChanged;
        }

        private void OnFilteredDevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibleDevices();
        }

        private void RefreshVisibleDevices()
        {
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            var pageItems = State.FilteredDeviceDto
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            VisibleDevices.Clear();
            foreach (var device in pageItems)
            {
                VisibleDevices.Add(device);
            }

            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(HasMultiplePages));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(PagingSummary));
            OnPropertyChanged(nameof(VisibleStartIndex));
            OnPropertyChanged(nameof(VisibleEndIndex));
            OnPropertyChanged(nameof(VisibleRangeSummary));
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
    }
}
