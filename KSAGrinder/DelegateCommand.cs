// Credit: https://afsdzvcx123.tistory.com/entry/C-WPF-WPF-Command-Binding-하는-방법

using System;
using System.Windows.Input;

namespace KSAGrinder
{
    public class DelegateCommand : ICommand
    {
        public delegate void CommandDelegate();

        private readonly Func<bool>? _canExecute;
        private readonly CommandDelegate _execute;

        public DelegateCommand(CommandDelegate execute) : this(execute, null)
        {

        }

        public DelegateCommand(CommandDelegate execute, Func<bool>? canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute is null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        public delegate void CommandDelegate(T parameter);

        private readonly Func<bool>? _canExecute;
        private readonly CommandDelegate _execute;

        public DelegateCommand(CommandDelegate execute) : this(execute, null)
        {

        }

        public DelegateCommand(CommandDelegate execute, Func<bool>? canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (parameter is not T)
                return false;
            if (_canExecute is null)
                return true;
            return _canExecute();
        }

        public void Execute(object? parameter)
        {
            if (parameter is T t)
                _execute(t);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
