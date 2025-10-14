using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TwitchWatcher.WPF.ViewModels
{
    public partial class ChannelItemVm : ObservableObject
    {
        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _status = "Unknown";

    }
}
