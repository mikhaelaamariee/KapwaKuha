using System.Windows;
using System.Windows.Input;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ChatWindow : Window
    {
        public ChatWindow(string myId, string otherId, string otherName, string role)
        {
            InitializeComponent();
            var vm = new ChatViewModel(myId, otherId, otherName, role);
            DataContext = vm;

            // ── Fix: wire ScrollToBottom so new messages auto-scroll for BOTH donor and beneficiary ──
            vm.ScrollToBottom += () =>
            {
                // Dispatch to ensure the UI has rendered the new message before scrolling
                Dispatcher.InvokeAsync(() =>
                {
                    MessagesScroll.ScrollToBottom();
                }, System.Windows.Threading.DispatcherPriority.Background);
            };

            Loaded += (s, e) =>
            {
                NavigationService.SetCurrent(this);
                // Scroll to bottom on first load too
                MessagesScroll.ScrollToBottom();
            };
        }

        private void MsgBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ChatViewModel vm)
                vm.SendCommand.Execute(null);
        }
    }
}