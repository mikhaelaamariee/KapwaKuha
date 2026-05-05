// FILE: ViewModels/ChatViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;
using System.Linq;

namespace KapwaKuha.ViewModels
{
    public class ChatViewModel : ObservableObject
    {
        private readonly string _myId;
        private readonly string _otherId;
        private readonly string _role;

        public string OtherName { get; }

        // Exposed so XAML can AND with IsSystemDirectTarget to hide buttons from donor
        public bool IsBeneficiary => _role == "Beneficiary";

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

       
        public ICommand BackCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

        public event Action? ScrollToBottom;

        public ChatViewModel(string myId, string otherId, string otherName, string role)
        {
            _myId = myId;
            _otherId = otherId;
            _role = role;
            OtherName = otherName;

            BackCommand = new RelayCommand(_ =>
            {
                if (_role == "Donor")
                    NavigationService.Navigate(new View.ChatListWindow(_myId, "Donor"));
                else
                    NavigationService.Navigate(new View.ChatListWindow(_myId, "Beneficiary"));
            });

            SendCommand = new AsyncRelayCommand(async _ =>
            {
                if (string.IsNullOrWhiteSpace(InputText)) return;
                string msg = InputText.Trim();
                InputText = string.Empty;
                await KapwaDataService.SaveChatMessage(_myId, _otherId, msg);
                await LoadMessages();
            });

            SendImageCommand = new AsyncRelayCommand(async _ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Image to Send"
                };
                if (dlg.ShowDialog() != true) return;
                string msg = $"[IMG]{dlg.FileName}";
                await KapwaDataService.SaveChatMessage(_myId, _otherId, msg);
                await LoadMessages();
            });

            // FIXED: null-safe, checks IsBeneficiary, validates LinkedItemId
            AcceptCommand = new AsyncRelayCommand(async param =>
            {
                if (!IsBeneficiary) return;
                if (param is not ChatMessage msg) return;
                if (string.IsNullOrEmpty(msg.LinkedItemId))
                {
                    MessageBox.Show("No item linked to this message.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Build the item from what we know — ClaimItemWindow will do the full claim
                var itemForClaim = new ItemModel
                {
                    Item_ID = msg.LinkedItemId,
                    Item_Name = msg.Text.Contains("Item: \"")
                        ? msg.Text[(msg.Text.IndexOf("Item: \"") + 7)..msg.Text.IndexOf("\"", msg.Text.IndexOf("Item: \"") + 7)]
                        : msg.LinkedItemId,
                    Donor_ID = _otherId,
                    Item_ImagePath = msg.LinkedItemPath ?? string.Empty
                };

                // Navigate to ClaimItemWindow WITHOUT pre-claiming
                // Buttons stay visible until the form is actually submitted
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // ClaimItemWindow will call SaveClaim itself on ConfirmClaimCommand
                    // We only hide buttons AFTER successful navigation + confirmed submit
                    // Pass a callback action that hides the buttons on success
                    NavigationService.Navigate(
                        new View.ClaimItemWindow(_myId, itemForClaim, onClaimSuccess: () =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                msg.IsActionable = false;
                                _ = LoadMessages();
                            });
                        }));
                });
            });

            DeclineCommand = new AsyncRelayCommand(async param =>
            {
                if (!IsBeneficiary) return;
                if (param is not ChatMessage msg) return;
                if (string.IsNullOrEmpty(msg.LinkedItemId)) return;

                var confirm = MessageBox.Show(
                    "Decline this donation? The item will be returned to the general marketplace.",
                    "Decline Donation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                // ── HIDE BUTTONS IMMEDIATELY ──
                Application.Current.Dispatcher.Invoke(() => msg.IsActionable = false);

                try
                {
                    IsBusy = true;
                    await KapwaDataService.RevertItemToGeneralPost(msg.LinkedItemId);
                    await KapwaDataService.SaveChatMessage(_myId, _otherId,
                        "❌ I have declined the donation. The item has been returned to the marketplace.");
                    MessageBox.Show("Item returned to marketplace.", "Declined",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadMessages();
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() => msg.IsActionable = true);
                }
                finally { IsBusy = false; }
            });

            _ = LoadMessages();
        }

        private async System.Threading.Tasks.Task LoadMessages()
        {
            try
            {
                var msgs = await KapwaDataService.GetChatMessages(_myId, _otherId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                    foreach (var m in msgs) Messages.Add(m);
                    ScrollToBottom?.Invoke();
                });
            }
            catch { }
        }
    }
}