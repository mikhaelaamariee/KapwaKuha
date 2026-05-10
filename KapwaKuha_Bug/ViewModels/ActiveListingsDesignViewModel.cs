// FILE: ViewModels/ActiveListingsDesignViewModel.cs
using System.Collections.ObjectModel;
using KapwaKuha.Models;

namespace KapwaKuha.ViewModels   // ← MUST have namespace
{
    public class ActiveListingsDesignViewModel
    {
        public ObservableCollection<ItemModel> Items { get; } = new()
        {
            new ItemModel
            {
                Item_ID = "ITEM001", Item_Name = "School Bag",
                Item_Status = "Available", Category_Name = "School Supplies",
                Item_Condition = "Good", PostType = "GeneralPost",
                Item_Description = "Lightly used school bag"
            },
            new ItemModel
            {
                Item_ID = "ITEM002", Item_Name = "Winter Blanket",
                Item_Status = "Claimed", Category_Name = "Clothing",
                Item_Condition = "New", PostType = "DirectTarget",
                Item_Description = "Brand new warm blanket"
            },
            new ItemModel
            {
                Item_ID = "ITEM003", Item_Name = "Canned Goods Box",
                Item_Status = "Reserved", Category_Name = "Food",
                Item_Condition = "Good", PostType = "GeneralPost",
                Item_Description = "Assorted canned goods"
            },
        };
        public string StatusMessage { get; } = "3 item(s) posted.";
        public bool IsItemSelected { get; } = true;
        public bool CanEditSelected { get; } = true;
        public bool IsBusy { get; } = false;
        public bool IsEditPanelOpen { get; } = false;
        public string EditName { get; } = "Sample Item";
        public string EditDescription { get; } = "Sample description";
        public string EditCondition { get; } = "Good";
        public string EditImagePath { get; } = string.Empty;
        public bool HasEditImage { get; } = false;
        public string[] Conditions { get; } = { "New", "Good", "Fair", "Poor" };
    }
}