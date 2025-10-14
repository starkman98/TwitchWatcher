using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace TwitchWatcher.WPF.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _can;
        public RelayCommand(Action execute, Func<bool>? can = null)
        { 
            _execute = execute; _can = can;
            CommandManager.RequerySuggested += (_, __) => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
