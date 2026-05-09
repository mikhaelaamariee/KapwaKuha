// FILE: ViewModels/BrowseItemsDesignViewModel.cs
using KapwaKuha.Commands;
using KapwaKuha.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KapwaKuha.ViewModels
{
    public class BrowseItemsDesignViewModel
    {
        public string SearchText { get; } = string.Empty;
        public string FilterCategory { get; } = "All";
        public string FilterCondition { get; } = "Any";
        public bool IsBusy { get; } = false;

        public bool IsCatAll { get; } = true;
        public bool IsCatClothing { get; } = false;
        public bool IsCatFood { get; } = false;
        public bool IsCatElectronics { get; } = false;
        public bool IsCatMedicine { get; } = false;
        public bool IsCatSchool { get; } = false;

        public ObservableCollection<ItemModel> Items { get; } = new()
        {
            new ItemModel
            {
                Item_ID          = "ITEM001",
                Item_Name        = "Assorted School Supplies",
                Item_Description = "Notebooks, pencils, crayons for students.",
                Item_Condition   = "New",
                Item_Status      = "Available",
                Category_Name    = "School Supplies",
                Donor_Name       = "Juan Dela Cruz",
                PostType         = "GeneralPost",
                Date_Found       = System.DateTime.Now
            },
            new ItemModel
            {
                Item_ID          = "ITEM002",
                Item_Name        = "Children's Clothing Set",
                Item_Description = "Gently used clothing for ages 4-8.",
                Item_Condition   = "Good",
                Item_Status      = "Available",
                Category_Name    = "Clothing",
                Donor_Name       = "Maria Santos",
                PostType         = "GeneralPost",
                Date_Found       = System.DateTime.Now.AddDays(-1)
            }
        };

        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
        public ICommand SelectItemCommand { get; } = new RelayCommand(_ => { });
    }
}