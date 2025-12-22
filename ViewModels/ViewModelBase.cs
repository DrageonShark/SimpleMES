using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleMES.ViewModels
{
    public partial class ViewModelBase:ObservableObject
    {
        [ObservableProperty] private string _pageTitle = "未知页面";
    }
}
