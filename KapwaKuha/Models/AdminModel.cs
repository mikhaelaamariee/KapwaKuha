// FILE: Models/AdminModel.cs  (NEW — for finals)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class AdminModel : ObservableObject
    {
        public string Admin_ID { get; set; } = string.Empty;
        public string Admin_FullName { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
    }
}