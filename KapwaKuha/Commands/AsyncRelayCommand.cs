// Commands/AsyncRelayCommand.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KapwaKuha
{
    /// <summary>Async ICommand — accepts both parameterless and object-param lambdas.</summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private bool _isExecuting;

        // Parameterless lambda:  new AsyncRelayCommand(async () => { ... })
        public AsyncRelayCommand(Func<Task> execute)
            => _execute = _ => execute();

        // Object-param lambda:   new AsyncRelayCommand(async param => { ... })
        public AsyncRelayCommand(Func<object?, Task> execute)
            => _execute = execute;

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try { await _execute(parameter); }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Async ICommand with a typed parameter.</summary>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                T? typedParam = parameter is T t ? t : default;
                await _execute(typedParam);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}