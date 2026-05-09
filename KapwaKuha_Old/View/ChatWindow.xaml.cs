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
            DataContext = new ChatViewModel(myId, otherId, otherName, role);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void MsgBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ChatViewModel vm)
                vm.SendCommand.Execute(null);
        }
    }
}