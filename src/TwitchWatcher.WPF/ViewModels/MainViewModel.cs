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
using TwitchWatcher.Contracts;

namespace TwitchWatcher.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase /*, IChannelStateUpdater, IChannelTitleUpdater, IChannelImageUrlUpdater*/
    {
        private readonly IOptionsMonitor<AppOptions> _monitor;
        private readonly IWritableOptions<AppOptions> _writable;
        private readonly IChannelDataStore _store;
        private readonly ITwitchApi _api;

        public ObservableCollection<ChannelConfig> Channels { get; } = new();

        private ChannelConfig? _selected;
        public ChannelConfig? Selected { get => _selected; 
            set { Set(ref _selected, value); RemoveCommand?.NotifyCanExecuteChanged(); } }

        private string _newLogin = "";
        public string NewLogin { get => _newLogin; 
            set { Set(ref _newLogin, value); AddCommand?.NotifyCanExecuteChanged(); } }

        private bool _isSyncing;

        public IAsyncRelayCommand AddCommand { get; private set; }
        public IAsyncRelayCommand RemoveCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }

        public MainViewModel (IOptionsMonitor<AppOptions> monitor, IWritableOptions<AppOptions> writable, IChannelDataStore store, ITwitchApi api)
        {
            _monitor = monitor;
            _writable = writable;
            _store = store;
            _api = api;
            
            SyncFromOptions(_monitor.CurrentValue);
            _monitor.OnChange(options =>
            App.Current.Dispatcher.Invoke(() => SyncFromOptions(options)));
           
            _store.DataChanged += Store_DataChanged;

            RefreshFromStore();

            AddCommand = new AsyncRelayCommand(AddAsync, () => !string.IsNullOrWhiteSpace(NewLogin));
            RemoveCommand = new AsyncRelayCommand(RemoveAsync, () => Selected != null);
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => true);
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
            //Channels.Clear();

            //foreach (var c in options.Channels
            //    .Where(c => !string.IsNullOrWhiteSpace(c.Login))
            //    .GroupBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
            //    .Select(g => g.First())
            //    .OrderBy(c => c.Login))
            //{
            //    Channels.Add(new ChannelConfig { Login = c.Login });
            //}

            var savedLogins = options.Channels
                .Where(c => !string.IsNullOrWhiteSpace(c.Login))
                .GroupBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First().Login.Trim().ToLowerInvariant())
                .OrderBy(l => l)
                .ToList();

            // remove deleted channels (iterate backwards)
            for (int i = Channels.Count - 1; i >= 0; i--)
            {
                var ch = Channels[i];
                if (!savedLogins.Contains(ch.Login, StringComparer.OrdinalIgnoreCase))
                    Channels.RemoveAt(i);
            }

            // insert new channels and reorder while preserving existing ChannelConfig instances
            int insertIndex = 0;
            foreach (var login in savedLogins)
            {
                var existing = Channels.FirstOrDefault(c => string.Equals(c.Login, login, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    var currentIndex = Channels.IndexOf(existing);
                    if (currentIndex != insertIndex)
                        Channels.Move(currentIndex, insertIndex);
                }
                else
                {
                    var newCh = new ChannelConfig { Login = login };
                    // seed UI values from the store immediately so UI doesn't show blanks
                    if (_store.TryGetUser(login, out var user))
                    {
                        newCh.DisplayName = user.DisplayName;
                        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
                            newCh.ImageUrl = user.ProfileImageUrl.Replace("300", "70");
                    }
                    Channels.Insert(insertIndex, newCh);
                }
                insertIndex++;
            }
        }

        private async Task AddAsync()
        {
            var login = NewLogin.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(login)) return;
            if (Channels.Any(c => c.Login.Equals(login, StringComparison.OrdinalIgnoreCase))) return;

            Channels.Add(new ChannelConfig { Login = login });
            NewLogin = "";
            CommandManager.InvalidateRequerySuggested();
            RefreshFromStore();

            await SaveAsync();
        }

        private async Task RemoveAsync()
        {
            if (Selected is null) return;
            Channels.Remove(Selected);
            Selected = null;
            CommandManager.InvalidateRequerySuggested();

            await SaveAsync();
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

            RefreshFromStore();
        }

        private async Task SaveAsync()
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

            var logins = Channels
                .Select(c => (c.Login ?? string.Empty).Trim().ToLowerInvariant())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            try
            {
                var usersMap = await _api.GetUsersDataByLoginsAsync(logins);
                if (usersMap != null && usersMap.Count > 0)
                {
                    _store.SetUsers(usersMap.Values);
                }

                var userIds = usersMap?.Values
                    .Select(u => u.Id)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList() 
                    ?? new();

                if (userIds.Count > 0)
                {
                    var streamsMap = await _api.GetStreamsByUserIdsAsync(userIds);
                    if (streamsMap != null)
                    {
                        _store.SetStreams(streamsMap.Values);
                    }
                }
            }
            catch (Exception ex) { /*optionally log errors in future*/}

            if (!App.Current.Dispatcher.CheckAccess())
                App.Current.Dispatcher.Invoke(RefreshFromStore);
            else
                RefreshFromStore();
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
