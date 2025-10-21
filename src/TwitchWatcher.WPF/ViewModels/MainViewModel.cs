using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm;
using TwitchWatcher.Configuration;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using TwitchWatcher.WPF.Infrastructure.Configuration;
using System.DirectoryServices.ActiveDirectory;
using TwitchWatcher.Models;
using TwitchWatcher.Core.Contracts;
using TwitchWatcher.Services;

namespace TwitchWatcher.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase /*, IChannelStateUpdater, IChannelTitleUpdater, IChannelImageUrlUpdater*/
    {
        private readonly IOptionsMonitor<AppOptions> _monitor;
        private readonly IWritableOptions<AppOptions> _writable;
        private readonly IChannelDataStore _store;

        public ObservableCollection<ChannelConfig> Channels { get; } = new();

        private ChannelConfig? _selected;
        public ChannelConfig? Selected { get => _selected; set { _selected = value; CommandManager.InvalidateRequerySuggested(); } }

        private string _newLogin = "";
        public string NewLogin { get => _newLogin; set { _newLogin = value; CommandManager.InvalidateRequerySuggested(); } }

        private bool _isSyncing;

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }

        public MainViewModel (IOptionsMonitor<AppOptions> monitor, IWritableOptions<AppOptions> writable, IChannelDataStore store)
        {
            _monitor = monitor;
            _writable = writable;
            _store = store;
            
            SyncFromOptions(_monitor.CurrentValue);
            _monitor.OnChange(options =>
            App.Current.Dispatcher.Invoke(() => SyncFromOptions(options)));
           
            _store.DataChanged += Store_DataChanged;

            RefreshFromStore();

            AddCommand = new RelayCommand(Add, () => !string.IsNullOrWhiteSpace(NewLogin));
            RemoveCommand = new RelayCommand(Remove, () => Selected != null);
            SaveCommand = new RelayCommand(Save, () => true);
        }

        public void Store_DataChanged(object? sender, EventArgs e)
        {
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(RefreshFromStore);
                return;
            }
            RefreshFromStore();
        }

        public void RefreshFromStore()
        {
            var users = _store.GetUsersSnapshot();
            var streams = _store.GetStreamsSnapshot();

            foreach (var channel in Channels)
            {
                var login = (channel.Login ?? string.Empty).Trim().ToLowerInvariant();

                if (users.TryGetValue(login, out var user))
                {
                    channel.DisplayName = user.DisplayName;
                    channel.ImageUrl = user.ProfileImageUrl.Replace("300", "70");
                }

                if (user != null && streams.TryGetValue(user.Id, out var stream))
                {
                    channel.Title = stream.Title ?? channel.Title;
                    channel.ViewerCount = stream.ViewerCount;
                    channel.State = string.Equals(stream.Type,"live", StringComparison.OrdinalIgnoreCase)
                        ? StreamState.Live
                        : StreamState.Offline;
                }
            }
        }

        private void SyncFromOptions(AppOptions options)
        {
            Channels.Clear();
            foreach (var c in options.Channels
                .Where(c => !string.IsNullOrWhiteSpace(c.Login))
                .GroupBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.Login))
            {
                Channels.Add(new ChannelConfig { Login = c.Login });
            }  
        }

        private void Add()
        {
            var login = NewLogin.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(login)) return;
            if (Channels.Any(c => c.Login.Equals(login, StringComparison.OrdinalIgnoreCase))) return;

            Channels.Add(new ChannelConfig { Login = login });
            NewLogin = "";
            CommandManager.InvalidateRequerySuggested();
        }

        private void Remove()
        {
            if (Selected is null) return;
            Channels.Remove(Selected);
            Selected = null;
            CommandManager.InvalidateRequerySuggested();
        }

        private void Save()
        {
            _writable.Update(options =>
            {
                options.Channels = Channels
                .Where(c => !string.IsNullOrWhiteSpace(c.Login))
                .GroupBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.Login)
                .ToList();
            });
            CommandManager.InvalidateRequerySuggested();
            
            
        }

        //public void UpdateChannelState(string login, StreamState state)
        //{
        //    if (!Application.Current.Dispatcher.CheckAccess())
        //    {
        //        Application.Current.Dispatcher.Invoke(() => UpdateChannelState(login, state));
        //        return;
        //    }

        //    var channel = Channels.FirstOrDefault(c => c.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        //    if (channel != null)
        //    {
        //        channel.State = state;
        //    }
        //}

        //public void UpdateChannelTitle(string login, string title)
        //{
        //    if (!Application.Current.Dispatcher.CheckAccess())
        //    {
        //        Application.Current.Dispatcher.Invoke(() => UpdateChannelTitle(login, title));
        //        return;
        //    }

        //    var channel = Channels.FirstOrDefault(c => c.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        //    if (channel != null)
        //    {
        //        channel.Title = title;
        //    }
        //}

        //public void UpdateChannelImageUrl(string login, string imageUrl)
        //{
        //    if (!Application.Current.Dispatcher.CheckAccess())
        //    {
        //        Application.Current.Dispatcher.Invoke(() => UpdateChannelTitle(login, imageUrl));
        //        return;
        //    }

        //    var channel = Channels.FirstOrDefault(c => c.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        //    if (channel != null)
        //    {
        //        channel.ImageUrl = imageUrl;
        //    }
        //}
    }
}
