using KapwaKuha.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KapwaKuha.ViewModels
{
    public class HighPriorityNeedsDesignViewModel
    {
        public ObservableCollection<NeedsPostModel> NeedsPosts { get; } = new()
        {
            new NeedsPostModel { NeedsPost_ID="NP001", Title="Blankets for Street Children", Org_Name="Barangay San Jose",    Urgency="High",   Description="We urgently need 50 warm blankets for homeless children this month." },
            new NeedsPostModel { NeedsPost_ID="NP002", Title="School Supplies Kit",          Org_Name="SISC Student Welfare", Urgency="Medium", Description="Back-to-school kits needed for 30 underprivileged students." },
            new NeedsPostModel { NeedsPost_ID="NP003", Title="Canned Goods Drive",           Org_Name="Barangay San Jose",    Urgency="Low",    Description="Accepting canned goods donations for the community pantry." },
        };
        public bool IsBusy { get; } = false;
    }
}
