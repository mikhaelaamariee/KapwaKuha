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
        private readonly string _otherName;

        public string OtherName { get; }
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
            _otherName = otherName;
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

            // ── RelayCommand (not Async) — navigation is synchronous, no await needed ──
            AcceptCommand = new RelayCommand(param =>
            {
                if (!IsBeneficiary) return;
                if (param is not ChatMessage msg) return;
                if (string.IsNullOrEmpty(msg.LinkedItemId))
                {
                    MessageBox.Show("No item linked to this message.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string itemName = msg.LinkedItemId;
                int colonIdx = msg.Text.IndexOf("Item:");
                if (colonIdx >= 0)
                {
                    string afterColon = msg.Text[(colonIdx + 5)..].TrimStart();
                    int nameStart = 0;
                    while (nameStart < afterColon.Length && afterColon[nameStart] == '"')
                        nameStart++;
                    int nameEnd = afterColon.IndexOf('"', nameStart);
                    if (nameEnd > nameStart)
                        itemName = afterColon[nameStart..nameEnd].Trim().Trim('"');
                }

                var itemForClaim = new ItemModel
                {
                    Item_ID = msg.LinkedItemId,
                    Item_Name = itemName,
                    Donor_ID = _otherId,
                    Donor_Name = _otherName,
                    Item_ImagePath = msg.LinkedItemPath ?? string.Empty
                };

                var capturedMsg = msg;
                NavigationService.Navigate(
                    new View.ClaimItemWindow(
                        _myId,
                        itemForClaim,
                        onClaimSuccess: () =>
                        {
                            capturedMsg.IsActionable = false;
                            _ = LoadMessages();
                        },
                        returnToDonorId: _otherId,
                        returnToDonorName: _otherName));
            });

            DeclineCommand = new AsyncRelayCommand(async param =>
            {
                if (!IsBeneficiary) return;
                if (param is not ChatMessage msg) return;
                if (string.IsNullOrEmpty(msg.LinkedItemId)) return;

                var confirm = MessageBox.Show(
                    "Decline this donation?\n\nThe item will be deactivated. " +
                    "The donor can choose to re-post it manually from their listings.",
                    "Decline Donation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes)
                {
                    await LoadMessages();
                    return;
                }

                msg.IsDeclined = true;
                msg.IsActionable = false;

                try
                {
                    IsBusy = true;
                    await KapwaDataService.RevertItemToGeneralPost(msg.LinkedItemId);
                    msg.IsActionable = false;
                    await KapwaDataService.SaveChatMessage(_myId, _otherId,
                        "❌ I have declined the donation. The item has been deactivated. " +
                        "You can re-post it from your Active Listings if you wish.");
                    MessageBox.Show("Donation declined.", "Declined",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadMessages();
                }
                catch { }
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
                var otherPic = string.Empty;
                try
                {
                    if (_role == "Beneficiary")
                    {
                        var donor = await KapwaDataService.GetDonorById(_otherId);
                        otherPic = donor?.ProfilePicturePath ?? string.Empty;
                    }
                    else
                    {
                        var bene = await KapwaDataService.GetBeneficiaryById(_otherId);
                        otherPic = bene?.ProfilePicturePath ?? string.Empty;
                    }
                }
                catch { }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var m in Messages)
                        if (!m.IsFromUser) m.SenderProfilePicture = otherPic;
                });
            }
            catch { }
        }
    }
}