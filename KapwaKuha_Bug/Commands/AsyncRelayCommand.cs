using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KapwaKuha.Commands
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> executeAsync)
            => _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

        public bool CanExecute(object? parameter) => !_isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try { await _executeAsync(parameter); }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}