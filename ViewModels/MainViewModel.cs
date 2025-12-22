using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel:ViewModelBase
    {
        [ObservableProperty] private object _currentView;
    }
}
