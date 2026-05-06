// FILE: KapwaDataService.cs  (FULL REVISED)
// Database: KAPWAKUHA_DATABASE
// Pattern: identical to CarDataService — async, parameterized, dual-connection auto-detect.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using KapwaKuha.Models;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace KapwaKuha.Services
{
    public static class KapwaDataService
    {
        // ── Connection ────────────────────────────────────────────────────────
        private static string? _cachedConn;

        private static readonly string _laptopConn =
            @"Server=DESKTOP-8P1VJSE;Database=KapwaKuha_Database;Trusted_Connection=True;TrustServerCertificate=True;";
        private static readonly string _pcConn =
            @"Server=CCL2-12\MSSQLSERVER01;Database=KapwaKuha_Database;User Id=sa;Password=ccl2;TrustServerCertificate=True;";

        private static string _conn
        {
            get
            {
                if (_cachedConn != null) return _cachedConn;
                try
                {
                    using var t = new SqlConnection(_pcConn + "Connect Timeout=5;");
                    t.Open();
                    return _cachedConn = _pcConn;
                }
                catch { }
                return _cachedConn = _laptopConn;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // AUTH / LOGIN
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<(bool OK, string UserId, string FullName, string Username)>
     LoginDonor(string username, string password)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT d.Donor_ID, d.Donor_FullName, d.Donor_Username,
                   u.IsActive
            FROM Donors d
            INNER JOIN Users u ON u.UserID = d.Donor_ID
            WHERE d.Donor_Username = @uname AND u.Password = @pw", conn);
                cmd.Parameters.AddWithValue("@uname", username);
                cmd.Parameters.AddWithValue("@pw", password);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    string userId = r["Donor_ID"].ToString() ?? "";
                    string fullName = r["Donor_FullName"].ToString() ?? "";
                    string uname = r["Donor_Username"].ToString() ?? "";
                    bool isActive = Convert.ToBoolean(r["IsActive"]);
                    r.Close();

                    if (!isActive)
                    {
                        var reactivate = System.Windows.MessageBox.Show(
                            "Your account is currently deactivated.\n\nWould you like to reactivate it?",
                            "Account Deactivated",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (reactivate != System.Windows.MessageBoxResult.Yes)
                            return (false, "", "", "");

                        using var c2 = new SqlCommand(
                            "UPDATE Users SET IsActive = 1 WHERE UserID = @id", conn);
                        c2.Parameters.AddWithValue("@id", userId);
                        await c2.ExecuteNonQueryAsync();
                    }

                    return (true, userId, fullName, uname);
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("LoginDonor failed: " + ex.Message); }
            return (false, "", "", "");
        }

        public static async Task<(bool OK, string UserId, string FullName, string Username)>
            LoginBeneficiary(string username, string password)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT b.Beneficiary_ID,
                   b.Beneficiary_FullName AS FullName,
                   b.Beneficiary_Username AS Username,
                   u.IsActive
            FROM Beneficiaries b
            INNER JOIN Users u ON u.UserID = b.Beneficiary_ID
            WHERE b.Beneficiary_Username = @uname AND u.Password = @pw", conn);
                cmd.Parameters.AddWithValue("@uname", username);
                cmd.Parameters.AddWithValue("@pw", password);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    string userId = r["Beneficiary_ID"].ToString() ?? "";
                    string fullName = r["FullName"].ToString() ?? "";
                    string uname = r["Username"].ToString() ?? "";
                    bool isActive = Convert.ToBoolean(r["IsActive"]);
                    r.Close();

                    if (!isActive)
                    {
                        var reactivate = System.Windows.MessageBox.Show(
                            "Your account is currently deactivated.\n\nWould you like to reactivate it?",
                            "Account Deactivated",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (reactivate != System.Windows.MessageBoxResult.Yes)
                            return (false, "", "", "");

                        using var c2 = new SqlCommand(
                            "UPDATE Users SET IsActive = 1 WHERE UserID = @id", conn);
                        c2.Parameters.AddWithValue("@id", userId);
                        await c2.ExecuteNonQueryAsync();
                    }

                    return (true, userId, fullName, uname);
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("LoginBeneficiary failed: " + ex.Message); }
            return (false, "", "", "");
        }

        // ══════════════════════════════════════════════════════════════════════
        // REGISTRATION (sp_RegisterDonor / sp_RegisterBeneficiary)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<string> GetNextDonorId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Donors", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"D{n + 1:D3}";
            }
            catch { return $"D{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task<string> GetNextBeneficiaryId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Beneficiaries", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"B{n + 1:D3}";
            }
            catch { return $"B{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task RegisterDonor(DonorModel donor, string password,
            string securityQuestion, string securityAnswer)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_RegisterDonor", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DonorId", donor.Donor_ID);
                cmd.Parameters.AddWithValue("@FullName", donor.Donor_FullName);
                cmd.Parameters.AddWithValue("@Username", donor.Donor_Username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@Contact", donor.Donor_ContactNumber);
                cmd.Parameters.AddWithValue("@SecurityQ", securityQuestion);
                cmd.Parameters.AddWithValue("@SecurityA", securityAnswer);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("RegisterDonor failed: " + ex.Message); throw; }
        }

        public static async Task RegisterBeneficiary(BeneficiaryModel bene, string password,
            string securityQuestion, string securityAnswer)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_RegisterBeneficiary", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", bene.Beneficiary_ID);
                cmd.Parameters.AddWithValue("@FName", bene.Beneficiary_FName);
                cmd.Parameters.AddWithValue("@LName", bene.Beneficiary_LName);
                cmd.Parameters.AddWithValue("@Sex", bene.Beneficiary_Sex);
                cmd.Parameters.AddWithValue("@Contact", bene.Beneficiary_Contact);
                cmd.Parameters.AddWithValue("@OrgId", bene.Organization_ID);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@SecurityQ", securityQuestion);
                cmd.Parameters.AddWithValue("@SecurityA", securityAnswer);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("RegisterBeneficiary failed: " + ex.Message); throw; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // FORGOT PASSWORD (sp_GetSecurityQuestion / sp_ResetPassword)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<(bool Found, string Question, string UserId)>
            GetSecurityQuestion(string username)
        {
            try
            {

                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                // Try Donors first, then Beneficiaries
                using var cmd = new SqlCommand(@"
                    SELECT Donor_ID AS UserId, SecurityQuestion FROM Donors WHERE Donor_Username = @u
                    UNION ALL
                    SELECT Beneficiary_ID AS UserId, SecurityQuestion FROM Beneficiaries WHERE Beneficiary_ID = @u", conn);
                cmd.Parameters.AddWithValue("@u", username);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return (true, r["SecurityQuestion"].ToString() ?? "", r["UserId"].ToString() ?? "");
            }
            catch (Exception ex) { MessageBox.Show("GetSecurityQuestion failed: " + ex.Message); }
            return (false, "", "");
        }

        public static async Task<(bool Success, string Message)>
            ResetPassword(string username, string securityAnswer, string newPassword)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_ResetPassword", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@SecurityAnswer", securityAnswer);
                cmd.Parameters.AddWithValue("@NewPassword", newPassword);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Password reset successfully.");
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                return (false, "Security answer is incorrect.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ITEMS  (Strong Entity — parallel to Cars)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<ItemModel>> GetAllItems()
        {
            var list = new List<ItemModel>();
            const string sql = @"
        SELECT i.Item_ID, i.Item_Name, i.Item_Condition, i.Item_Status,
               i.Date_Found, i.Donor_ID, i.Category_ID,
               ISNULL(p.Post_Type,'GeneralPost')    AS PostType,
               ISNULL(i.TargetBeneficiary_ID,'')    AS TargetBeneficiary_ID,
               ISNULL(i.Item_Description,'')         AS Item_Description,
               ISNULL(i.Item_ImagePath,'')           AS Item_ImagePath,
               d.Donor_FullName                      AS Donor_Name,
               c.Category_Name
        FROM Items i
        LEFT JOIN Donors   d ON d.Donor_ID    = i.Donor_ID
        LEFT JOIN Category c ON c.Category_ID = i.Category_ID
        LEFT JOIN Post     p ON p.Post_ID     = i.Post_ID";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(MapItem(r));
            }
            catch (Exception ex) { MessageBox.Show("GetAllItems failed: " + ex.Message); }
            return list;
        }
        public static async Task<List<ItemModel>> GetDirectTargetItems(string beneficiaryId)
        {
            var list = new List<ItemModel>();
            const string sql = @"
        SELECT i.Item_ID, i.Item_Name, i.Item_Description, i.Item_Condition,
               i.Item_Status, i.Date_Found, i.Donor_ID, d.Donor_FullName,
               i.Category_ID, c.Category_Name,
               ISNULL(p.Post_Type,'DirectTarget') AS PostType,
               i.TargetBeneficiary_ID, i.Item_ImagePath
        FROM Items i
        JOIN Donors   d ON d.Donor_ID    = i.Donor_ID
        JOIN Category c ON c.Category_ID = i.Category_ID
        JOIN Post     p ON p.Post_ID     = i.Post_ID
        WHERE p.Post_Type = 'DirectTarget'
          AND i.TargetBeneficiary_ID = @bid";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@bid", beneficiaryId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new ItemModel
                    {
                        Item_ID = r["Item_ID"].ToString() ?? "",
                        Item_Name = r["Item_Name"].ToString() ?? "",
                        Item_Description = r["Item_Description"].ToString() ?? "",
                        Item_Condition = r["Item_Condition"].ToString() ?? "",
                        Item_Status = r["Item_Status"].ToString() ?? "",
                        Date_Found = Convert.ToDateTime(r["Date_Found"]),
                        Donor_ID = r["Donor_ID"].ToString() ?? "",
                        Donor_Name = r["Donor_FullName"].ToString() ?? "",
                        Category_ID = r["Category_ID"].ToString() ?? "",
                        Category_Name = r["Category_Name"].ToString() ?? "",
                        PostType = r["PostType"].ToString() ?? "",
                        TargetBeneficiary_ID = r["TargetBeneficiary_ID"].ToString() ?? "",
                        Item_ImagePath = r["Item_ImagePath"].ToString() ?? ""
                    });
            }
            catch (Exception ex) { MessageBox.Show("GetDirectTargetItems failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<ItemModel>> GetAvailableItems()
        {
            var all = await GetAllItems();
            return all.FindAll(i => i.Item_Status == "Available" && i.PostType == "GeneralPost");
        }

        public static async Task<List<ItemModel>> GetItemsByDonor(string donorId)
        {
            var all = await GetAllItems();
            return all.FindAll(i => i.Donor_ID == donorId);
        }

        public static async Task UpdateItemStatus(string itemId, string status)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE Items SET Item_Status=@s WHERE Item_ID=@id", conn);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateItemStatus failed: " + ex.Message); }
        }

        /// <summary>
        /// Alias used by PostItemViewModel — maps PostType/TargetBeneficiary_ID
        /// from ItemModel's field names to the SQL INSERT.
        /// </summary>
        public static async Task AddItem(ItemModel item)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                // Resolve Post_ID from Post_Type
                // 'GeneralPost' → 'P0T1', 'DirectTarget' → 'P0T2'
                using var postCmd = new SqlCommand(
                    "SELECT Post_ID FROM Post WHERE Post_Type = @pt", conn);
                postCmd.Parameters.AddWithValue("@pt", item.PostType);
                string postId = (await postCmd.ExecuteScalarAsync())?.ToString() ?? "P0T1";

                using var cmd = new SqlCommand("sp_AddItem", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ItemId", item.Item_ID);
                cmd.Parameters.AddWithValue("@ItemName", item.Item_Name);
                cmd.Parameters.AddWithValue("@Description", item.Item_Description ?? "");
                cmd.Parameters.AddWithValue("@Condition", item.Item_Condition);
                cmd.Parameters.AddWithValue("@DonorId", item.Donor_ID);
                cmd.Parameters.AddWithValue("@CategoryId", item.Category_ID);
                cmd.Parameters.AddWithValue("@PostId", postId);     // ← Post_ID FK
                cmd.Parameters.AddWithValue("@TargetBeneId",
                    string.IsNullOrEmpty(item.TargetBeneficiary_ID) ? "" : item.TargetBeneficiary_ID);
                cmd.Parameters.AddWithValue("@ImagePath", item.Item_ImagePath ?? "");
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("AddItem failed: " + ex.Message); throw; }
        }


        public static async Task DeleteItem(string itemId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "DELETE FROM Items WHERE Item_ID=@id AND Item_Status='Available'", conn);
                cmd.Parameters.AddWithValue("@id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("DeleteItem failed: " + ex.Message); throw; }
        }

        public static async Task<string> GetNextItemId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Items", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"ITEM{n + 1:D3}";
            }
            catch { return $"ITEM{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task<List<string>> GetAllCategories()
        {
            var list = new List<string>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT Category_ID, Category_Name FROM Category ORDER BY Category_Name", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(r["Category_Name"].ToString() ?? "");
            }
            catch (Exception ex) { MessageBox.Show("GetAllCategories failed: " + ex.Message); }
            return list;
        }

        public static async Task<string> GetCategoryIdByName(string name)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT Category_ID FROM Category WHERE Category_Name=@n", conn);
                cmd.Parameters.AddWithValue("@n", name);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? "";
            }
            catch { return ""; }
        }

        /// <summary>Alias for GetCategoryIdByName — used by PostItemViewModel.</summary>
        public static Task<string> GetCategoryId(string name)
            => GetCategoryIdByName(name);

        /// <summary>
        /// Items with status "Available" — used by ClaimProcessViewModel.
        /// Parallel to GetActiveRentals() in CarRentals.
        /// </summary>
        public static async Task<List<ItemModel>> GetFoundItems()
        {
            var all = await GetAllItems();
            return all.FindAll(i => i.Item_Status == "Available");
        }
        private static ItemModel MapItem(SqlDataReader r) => new()
        {
            Item_ID = r["Item_ID"].ToString() ?? "",
            Item_Name = r["Item_Name"].ToString() ?? "",
            Item_Description = r["Item_Description"].ToString() ?? "",
            Item_Condition = r["Item_Condition"].ToString() ?? "",
            Item_Status = r["Item_Status"].ToString() ?? "",
            Date_Found = Convert.ToDateTime(r["Date_Found"]),
            Donor_ID = r["Donor_ID"].ToString() ?? "",
            Donor_Name = r["Donor_Name"].ToString() ?? "",
            Category_ID = r["Category_ID"].ToString() ?? "",
            Category_Name = r["Category_Name"].ToString() ?? "",
            PostType = r["PostType"].ToString() ?? "GeneralPost",
            TargetBeneficiary_ID = r["TargetBeneficiary_ID"].ToString() ?? "",
            Item_ImagePath = r["Item_ImagePath"].ToString() ?? ""
        };

        public static async Task<List<TransactionRow>> GetDonorTransactionHistory(string donorId)
        {
            var list = new List<TransactionRow>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetDonorTransactionHistory", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DonorId", donorId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new TransactionRow
                    {
                        Claim_ID = r["Claim_ID"].ToString() ?? "",
                        Item_ID = r["Item_ID"].ToString() ?? "",
                        Item_Name = r["Item_Name"].ToString() ?? "",
                        Item_ImagePath = r["Item_ImagePath"].ToString() ?? "",
                        Item_Description = r["Item_Description"].ToString() ?? "",
                        Category_Name = r["Category_Name"].ToString() ?? "",
                        Item_Condition = r["Item_Condition"].ToString() ?? "",
                        Beneficiary_Name = r["Beneficiary_Name"].ToString() ?? "",
                        Organization_Name = r["Organization_Name"].ToString() ?? "",
                        Claim_Date = Convert.ToDateTime(r["Claim_Date"]),
                        Claim_Status = r["Claim_Status"].ToString() ?? "",
                        Handoff_Type = r["Handoff_Type"].ToString() ?? "",
                        DaysToRelease = Convert.ToInt32(r["DaysToRelease"])
                    });
            }
            catch (Exception ex)
            { MessageBox.Show("GetDonorTransactionHistory failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<TransactionRow>> GetBeneficiaryTransactionHistory(string beneficiaryId)
        {
            var list = new List<TransactionRow>();
            try
            {
                using var conn = new SqlConnection(_conn); // Uses your class-level connection string
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetBeneficiaryTransactionHistory", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", beneficiaryId);

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new TransactionRow
                    {
                        Claim_ID = r["Claim_ID"].ToString() ?? "",
                        Item_ID = r["Item_ID"].ToString() ?? "",
                        Item_Name = r["Item_Name"].ToString() ?? "",
                        Item_ImagePath = r["Item_ImagePath"].ToString() ?? "",
                        Item_Description = r["Item_Description"].ToString() ?? "",
                        Category_Name = r["Category_Name"].ToString() ?? "",
                        Item_Condition = r["Item_Condition"].ToString() ?? "",
                        Donor_FullName = r["Donor_Name"].ToString() ?? "",  
                        Beneficiary_Name = r["Donor_Name"].ToString() ?? "",   
                        Organization_Name = r["Organization_Name"].ToString() ?? "",
                        Claim_Date = Convert.ToDateTime(r["Claim_Date"]),
                        Claim_Status = r["Claim_Status"].ToString() ?? "",
                        Handoff_Type = r["Handoff_Type"].ToString() ?? "",
                        DaysToRelease = Convert.ToInt32(r["DaysToRelease"])
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetBeneficiaryTransactionHistory failed: " + ex.Message);
            }
            return list;
        }

        public static async Task<ItemModel?> GetItemById(string itemId)
        {
            var all = await GetAllItems();
            return all.FirstOrDefault(i => i.Item_ID == itemId);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CLAIMS  (Weak Entity — parallel to Rentals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<ClaimModel>> GetAllClaims()
        {
            var list = new List<ClaimModel>();
            const string sql = @"
        SELECT cl.Claim_ID, cl.Item_ID, cl.Beneficiary_ID,
               cl.Claim_Date, cl.Claim_Status, cl.Verification_Notes,
               ISNULL(cl.Handoff_Type,'Pickup')        AS Handoff_Type,
               ISNULL(i.Item_Name,'')                  AS Item_Name,
               ISNULL(i.Item_ImagePath,'')             AS Item_ImagePath,
               ISNULL(c.Category_Name,'')              AS Category_Name,
               ISNULL(b.Beneficiary_FullName,'')       AS Beneficiary_Name
        FROM Claims cl
        LEFT JOIN Items         i ON i.Item_ID        = cl.Item_ID
        LEFT JOIN Category      c ON c.Category_ID    = i.Category_ID
        LEFT JOIN Beneficiaries b ON b.Beneficiary_ID = cl.Beneficiary_ID";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(MapClaim(r));
            }
            catch (Exception ex) { MessageBox.Show("GetAllClaims failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<ClaimModel>> GetClaimsByBeneficiary(string beneficiaryId)
        {
            var all = await GetAllClaims();
            return all.FindAll(c => c.Beneficiary_ID == beneficiaryId);
        }
        // Beneficiary transaction history — all claims by this beneficiary with full item details
        public static async Task<List<ClaimModel>> GetClaimHistoryByBeneficiary(string beneficiaryId)
        {
            var list = new List<ClaimModel>();
            const string sql = @"
        SELECT cl.Claim_ID, cl.Item_ID, cl.Beneficiary_ID,
               cl.Claim_Date, cl.Claim_Status, cl.Verification_Notes,
               ISNULL(cl.Handoff_Type,'Pickup')        AS Handoff_Type,
               ISNULL(i.Item_Name,'')                  AS Item_Name,
               ISNULL(i.Item_ImagePath,'')             AS Item_ImagePath,
               ISNULL(c.Category_Name,'')              AS Category_Name,
               ISNULL(b.Beneficiary_FullName,'')       AS Beneficiary_Name
        FROM Claims cl
        LEFT JOIN Items         i ON i.Item_ID        = cl.Item_ID
        LEFT JOIN Category      c ON c.Category_ID    = i.Category_ID
        LEFT JOIN Beneficiaries b ON b.Beneficiary_ID = cl.Beneficiary_ID
        WHERE cl.Beneficiary_ID = @bid
        ORDER BY cl.Claim_Date DESC";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@bid", beneficiaryId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(MapClaim(r));
            }
            catch (Exception ex) { MessageBox.Show("GetClaimHistoryByBeneficiary failed: " + ex.Message); }
            return list;
        }

        public static async Task<(bool Success, string Error)> SaveClaim(ClaimModel claim)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_ProcessClaim", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ClaimId", claim.Claim_ID);
                cmd.Parameters.AddWithValue("@ItemId", claim.Item_ID);
                cmd.Parameters.AddWithValue("@BeneficiaryId", claim.Beneficiary_ID);
                cmd.Parameters.AddWithValue("@ClaimStatus", claim.Claim_Status);
                cmd.Parameters.AddWithValue("@HandoffType", claim.Handoff_Type);
                cmd.Parameters.AddWithValue("@Notes", claim.Verification_Notes);
                await cmd.ExecuteNonQueryAsync();
                return (true, string.Empty);
            }
            catch (SqlException ex) when (ex.Number == 50010)
            {
                return (false, "❌ This item is no longer available for claiming.");
            }
            catch (SqlException ex) when (ex.Number == 50011)
            {
                return (false, "❌ You already have an existing claim on this item.");
            }
            catch (SqlException ex) when (ex.Number == 50012)
            {
                return (false, "❌ This item is reserved for another beneficiary.");
            }
            catch (Exception ex)
            {
                return (false, "SaveClaim failed: " + ex.Message);
            }
        }

         public static async Task UpdateClaimStatus(string claimId, string newStatus)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_UpdateClaimStatus", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateClaimStatus failed: " + ex.Message); }
        }

        public static async Task UpdateClaimStatusAndHandoff(
    string claimId, string newStatus, string handoffType,
    string location, string eventName, DateTime? eventDate)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                // Update claim status and handoff type
                using var cmd = new SqlCommand(@"
            UPDATE Claims
            SET Claim_Status = @s, Handoff_Type = @ht
            WHERE Claim_ID = @id", conn);
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@ht", handoffType);
                cmd.Parameters.AddWithValue("@id", claimId);
                await cmd.ExecuteNonQueryAsync();

                // If Released, also mark item as Claimed in Items table
                if (newStatus == "Released")
                {
                    using var cmd2 = new SqlCommand(@"
                UPDATE Items SET Item_Status = 'Claimed'
                WHERE Item_ID = (SELECT Item_ID FROM Claims WHERE Claim_ID = @id2)", conn);
                    cmd2.Parameters.AddWithValue("@id2", claimId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                // Upsert HandoffDetails
                using var cmd3 = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM HandoffDetails WHERE Claim_ID = @cid)
                UPDATE HandoffDetails
                SET HandoffType = @ht2, Location = @loc,
                    EventName = @en, EventDate = @ed
                WHERE Claim_ID = @cid
            ELSE
                INSERT INTO HandoffDetails (Claim_ID, HandoffType, Location, EventName, EventDate)
                VALUES (@cid, @ht2, @loc, @en, @ed)", conn);
                cmd3.Parameters.AddWithValue("@cid", claimId);
                cmd3.Parameters.AddWithValue("@ht2", handoffType);
                cmd3.Parameters.AddWithValue("@loc", (object?)location ?? DBNull.Value);
                cmd3.Parameters.AddWithValue("@en", (object?)eventName ?? DBNull.Value);
                cmd3.Parameters.AddWithValue("@ed", (object?)eventDate ?? DBNull.Value);
                await cmd3.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateClaimStatusAndHandoff failed: " + ex.Message); }
        }

        /// <summary>Donor: gets all claims across all items for tracking/advancing status.</summary>
        public static async Task<List<ClaimModel>> GetAllClaimsForDonor(string donorId)
        {
            var list = new List<ClaimModel>();
            const string sql = @"
        SELECT cl.Claim_ID, cl.Item_ID, cl.Beneficiary_ID,
               cl.Claim_Date, cl.Claim_Status, cl.Verification_Notes,
               ISNULL(cl.Handoff_Type,'Pickup')        AS Handoff_Type,
               ISNULL(i.Item_Name,'')                  AS Item_Name,
               ISNULL(i.Item_ImagePath,'')             AS Item_ImagePath,
               ISNULL(c.Category_Name,'')              AS Category_Name,
               ISNULL(b.Beneficiary_FullName,'')       AS Beneficiary_Name
        FROM Claims cl
        LEFT JOIN Items         i ON i.Item_ID        = cl.Item_ID
        LEFT JOIN Category      c ON c.Category_ID    = i.Category_ID
        LEFT JOIN Beneficiaries b ON b.Beneficiary_ID = cl.Beneficiary_ID
        WHERE i.Donor_ID = @did
        ORDER BY cl.Claim_Date DESC";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@did", donorId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(MapClaim(r));
            }
            catch (Exception ex) { MessageBox.Show("GetAllClaimsForDonor failed: " + ex.Message); }
            return list;
        }

        public static async Task<string> GetNextClaimId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Claims", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"CL{n + 1:D3}";
            }
            catch { return $"CL{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        private static ClaimModel MapClaim(SqlDataReader r) => new()
        {
            Claim_ID = r["Claim_ID"].ToString() ?? "",
            Item_ID = r["Item_ID"].ToString() ?? "",
            Item_Name = r["Item_Name"].ToString() ?? "",
            Item_ImagePath = r["Item_ImagePath"].ToString() ?? "",
            Category_Name = r["Category_Name"].ToString() ?? "",  // ADD
            Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
            Beneficiary_Name = r["Beneficiary_Name"].ToString() ?? "",
            Claim_Date = Convert.ToDateTime(r["Claim_Date"]),
            Claim_Status = r["Claim_Status"].ToString() ?? "Pending",
            Verification_Notes = r["Verification_Notes"].ToString() ?? "",
            Handoff_Type = r["Handoff_Type"].ToString() ?? "Pickup"
        };

        public static async Task UpdateNeedsPost(NeedsPostModel post)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            UPDATE NeedsPosts
            SET Title       = @title,
                Description = @desc,
                Urgency     = @urg,
                ImagePath   = @img
            WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@title", post.Title);
                cmd.Parameters.AddWithValue("@desc", post.Description);
                cmd.Parameters.AddWithValue("@urg", post.Urgency);
                cmd.Parameters.AddWithValue("@img", post.ImagePath ?? "");
                cmd.Parameters.AddWithValue("@id", post.NeedsPost_ID);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            { MessageBox.Show("UpdateNeedsPost failed: " + ex.Message); throw; }
        }

        // Updates only the urgency level of an existing NeedsPost
        public static async Task UpdateNeedsPostUrgency(string postId, string newUrgency)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE NeedsPosts SET Urgency = @urg WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@urg", newUrgency);
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateNeedsPostUrgency failed: " + ex.Message); throw; }
        }



        public static async Task DeleteNeedsPost(string postId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "DELETE FROM NeedsPosts WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            { MessageBox.Show("DeleteNeedsPost failed: " + ex.Message); throw; }
        }

        public static async Task<List<NeedsPostModel>> GetNeedsPostsByOrg(string orgId)
        {
            var list = new List<NeedsPostModel>();
            if (string.IsNullOrEmpty(orgId)) return list;
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT n.NeedsPost_ID, n.Org_ID, n.Title, n.Description,
                   n.Urgency, n.Status, n.Post_Date,
                   ISNULL(n.ImagePath,'') AS ImagePath,
                   ISNULL(o.Organization_Name,'') AS Org_Name
            FROM NeedsPosts n
            LEFT JOIN Organization o ON o.Organization_ID = n.Org_ID
            WHERE n.Org_ID = @oid
            ORDER BY n.Post_Date DESC", conn);
                cmd.Parameters.AddWithValue("@oid", orgId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new NeedsPostModel
                    {
                        NeedsPost_ID = r["NeedsPost_ID"].ToString() ?? "",
                        Org_ID = r["Org_ID"].ToString() ?? "",
                        Org_Name = r["Org_Name"].ToString() ?? "",
                        Title = r["Title"].ToString() ?? "",
                        Description = r["Description"].ToString() ?? "",
                        Urgency = r["Urgency"].ToString() ?? "Medium",
                        Status = r["Status"].ToString() ?? "Open",
                        Post_Date = Convert.ToDateTime(r["Post_Date"]),
                        ImagePath = r["ImagePath"].ToString() ?? ""
                    });
            }
            catch (Exception ex)
            { MessageBox.Show("GetNeedsPostsByOrg failed: " + ex.Message); }
            return list;
        }



        public static async Task UpdateItem(ItemModel item)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_UpdateItem", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ItemId", item.Item_ID);
                cmd.Parameters.AddWithValue("@ItemName", item.Item_Name);
                cmd.Parameters.AddWithValue("@Description", item.Item_Description);
                cmd.Parameters.AddWithValue("@Condition", item.Item_Condition);
                cmd.Parameters.AddWithValue("@ImagePath", item.Item_ImagePath);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateItem failed: " + ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // BENEFICIARIES
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<BeneficiaryModel>> GetActiveBeneficiariesFull()
{
    var list = new List<BeneficiaryModel>();
    const string sql = @"
                SELECT b.Beneficiary_ID, b.Beneficiary_FullName,
                       b.Beneficiary_Username,
                       b.Beneficiary_Sex, b.Beneficiary_Contact, b.Beneficiaries_Status,
                       b.Organization_ID, o.Organization_Name
                FROM Beneficiaries b
                LEFT JOIN Organization o ON o.Organization_ID = b.Organization_ID
                WHERE b.Beneficiaries_Status='Active'";
    try
    {
        using var conn = new SqlConnection(_conn);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new BeneficiaryModel
            {
                Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
                Beneficiary_FullName = r["Beneficiary_FullName"].ToString() ?? "",
                Beneficiary_Username = r["Beneficiary_Username"].ToString() ?? "",
                Beneficiary_Sex = r["Beneficiary_Sex"].ToString() ?? "",
                Beneficiary_Contact = r["Beneficiary_Contact"].ToString() ?? "",
                Beneficiaries_Status = r["Beneficiaries_Status"].ToString() ?? "Active",
                Organization_ID = r["Organization_ID"].ToString() ?? "",
                Organization_Name = r["Organization_Name"].ToString() ?? ""
            });
    }
    catch (Exception ex) { MessageBox.Show("GetActiveBeneficiaries failed: " + ex.Message); }
    return list;
}

        /// <summary>Returns ALL active beneficiaries for donor chat search.</summary>
        public static async Task<List<BeneficiaryModel>> GetAllBeneficiariesForChat()
        {
            var list = new List<BeneficiaryModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT b.Beneficiary_ID, b.Beneficiary_FullName,
                   b.Beneficiary_Username,
                   b.Beneficiary_Sex, b.Beneficiary_Contact, b.Beneficiaries_Status,
                   b.Organization_ID, b.ProfilePicturePath,
                   ISNULL(o.Organization_Name,'') AS Organization_Name
            FROM Beneficiaries b
            LEFT JOIN Organization o ON o.Organization_ID = b.Organization_ID
            WHERE b.Beneficiaries_Status = 'Active'
            ORDER BY b.Beneficiary_FullName", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new BeneficiaryModel
                    {
                        Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
                        Beneficiary_FullName = r["Beneficiary_FullName"].ToString() ?? "",
                        Beneficiary_Username = r["Beneficiary_Username"].ToString() ?? "",
                        Beneficiary_Sex = r["Beneficiary_Sex"].ToString() ?? "",
                        Beneficiary_Contact = r["Beneficiary_Contact"].ToString() ?? "",
                        Beneficiaries_Status = r["Beneficiaries_Status"].ToString() ?? "Active",
                        Organization_ID = r["Organization_ID"].ToString() ?? "",
                        Organization_Name = r["Organization_Name"].ToString() ?? "",
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? "" // KEY FIX
                    });
            }
            catch (Exception ex) { MessageBox.Show("GetAllBeneficiariesForChat failed: " + ex.Message); }
            return list;
        }

        /// <summary>
        /// For beneficiary side: returns donors who have exchanged messages with this beneficiary.
        /// </summary>
        // In KapwaDataService.cs — update signature:
        public static async Task<List<(string UserId, string FullName, string LastMessage,
                                        int UnreadCount, string ProfilePicturePath)>>
            GetChatDonorsForBeneficiary(string beneficiaryId)
        {
            var list = new List<(string, string, string, int, string)>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT DISTINCT d.Donor_ID, d.Donor_FullName,
                ISNULL(d.ProfilePicturePath,'') AS ProfilePicturePath,
                (SELECT TOP 1 Message FROM ChatMessages
                 WHERE (SenderId = d.Donor_ID AND ReceiverId = @bid)
                    OR (SenderId = @bid AND ReceiverId = d.Donor_ID)
                 ORDER BY SentAt DESC) AS LastMessage,
                (SELECT COUNT(*) FROM ChatMessages
                 WHERE SenderId = d.Donor_ID AND ReceiverId = @bid AND IsRead = 0) AS UnreadCount
            FROM Donors d
            WHERE d.Donor_ID IN (
                SELECT DISTINCT SenderId   FROM ChatMessages WHERE ReceiverId = @bid
                UNION
                SELECT DISTINCT ReceiverId FROM ChatMessages WHERE SenderId = @bid
            )", conn);
                cmd.Parameters.AddWithValue("@bid", beneficiaryId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add((
                        r["Donor_ID"].ToString() ?? "",
                        r["Donor_FullName"].ToString() ?? "",
                        r["LastMessage"].ToString() ?? "",
                        Convert.ToInt32(r["UnreadCount"]),
                        r["ProfilePicturePath"].ToString() ?? ""
                    ));
            }
            catch (Exception ex) { MessageBox.Show("GetChatDonorsForBeneficiary failed: " + ex.Message); }
            return list;
        }

        // Legacy tuple version used by ClaimProcessViewModel
        public static async Task<List<(string Id, string DisplayName)>> GetActiveBeneficiaries()
{
    var full = await GetActiveBeneficiariesFull();
    var result = new List<(string, string)>();
    foreach (var b in full)
        result.Add((b.Beneficiary_ID, b.DisplayName));
    return result;
}

public static async Task<List<(string Id, string Name)>> GetAllOrganizations()
{
    var list = new List<(string, string)>();
    try
    {
        using var conn = new SqlConnection(_conn);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Organization_ID, Organization_Name FROM Organization ORDER BY Organization_Name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((r["Organization_ID"].ToString() ?? "", r["Organization_Name"].ToString() ?? ""));
    }
    catch (Exception ex) { MessageBox.Show("GetAllOrganizations failed: " + ex.Message); }
    return list;
}

        // ══════════════════════════════════════════════════════════════════════
        // IMPACT METRICS  (parallel to Revenue analytics)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<(int Total, int Claimed, int Active, int FulfilledNeeds, int ActiveBeneficiaries)>
            GetImpactMetrics(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                using var c1 = new SqlCommand(
                    "SELECT COUNT(*) FROM Items WHERE Donor_ID=@did", conn);
                c1.Parameters.AddWithValue("@did", donorId);
                int total = Convert.ToInt32(await c1.ExecuteScalarAsync());

                using var c2 = new SqlCommand(
                    "SELECT COUNT(*) FROM Items WHERE Donor_ID=@did AND Item_Status='Claimed'", conn);
                c2.Parameters.AddWithValue("@did", donorId);
                int claimed = Convert.ToInt32(await c2.ExecuteScalarAsync());

                using var c3 = new SqlCommand(
                    "SELECT COUNT(*) FROM Items WHERE Donor_ID=@did AND Item_Status='Available'", conn);
                c3.Parameters.AddWithValue("@did", donorId);
                int active = Convert.ToInt32(await c3.ExecuteScalarAsync());

                using var c4 = new SqlCommand(
                    "SELECT COUNT(*) FROM NeedsPosts WHERE Status='Fulfilled'", conn);
                int fulfilled = Convert.ToInt32(await c4.ExecuteScalarAsync());

                using var c5 = new SqlCommand(
                    "SELECT COUNT(*) FROM Beneficiaries WHERE Beneficiaries_Status='Active'", conn);
                int activeBenes = Convert.ToInt32(await c5.ExecuteScalarAsync());

                return (total, claimed, active, fulfilled, activeBenes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetImpactMetrics failed: " + ex.Message);
                return (0, 0, 0, 0, 0);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // NEEDS POSTS  (organization wishlists)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<NeedsPostModel>> GetOpenNeedsPosts()
        {
            var list = new List<NeedsPostModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                // Use the stored procedure so RequesterBeneficiaryId is populated
                using var cmd = new SqlCommand("sp_GetOpenNeedsPosts", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(MapNeedsPost(r));
            }
            catch (Exception ex) { MessageBox.Show("GetOpenNeedsPosts failed: " + ex.Message); }
            return list;
        }


        public static async Task PostNeedsRequest(NeedsPostModel post)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_PostNeedsRequest", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PostId", post.NeedsPost_ID);
                cmd.Parameters.AddWithValue("@OrgId", post.Org_ID);
                cmd.Parameters.AddWithValue("@Title", post.Title);
                cmd.Parameters.AddWithValue("@Description", post.Description);
                cmd.Parameters.AddWithValue("@Urgency", post.Urgency);
                cmd.Parameters.AddWithValue("@ImagePath", post.ImagePath ?? ""); // FIX: was missing
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            { MessageBox.Show("PostNeedsRequest failed: " + ex.Message); throw; }
        }


        public static async Task<string> GetNextNeedsPostId()
{
    try
    {
        using var conn = new SqlConnection(_conn);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM NeedsPosts", conn);
        int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return $"NP{n + 1:D3}";
    }
    catch { return $"NP{DateTime.Now.Ticks % 900 + 100:D3}"; }
}

        private static NeedsPostModel MapNeedsPost(SqlDataReader r) => new()
        {
            NeedsPost_ID = r["NeedsPost_ID"].ToString() ?? "",
            Org_ID = r["Org_ID"].ToString() ?? "",
            Org_Name = r["Org_Name"].ToString() ?? "",
            Title = r["Title"].ToString() ?? "",
            Description = r["Description"].ToString() ?? "",
            Urgency = r["Urgency"].ToString() ?? "Medium",
            Status = r["Status"].ToString() ?? "Open",
            Post_Date = Convert.ToDateTime(r["Post_Date"]),
            ImagePath = r["ImagePath"].ToString() ?? "",
            RequesterBeneficiaryId = r["RequesterBeneficiaryId"].ToString() ?? ""  // ← ADD
        };

        // ══════════════════════════════════════════════════════════════════════
        // CHAT  (parallel to CarRentals ChatMessages)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task SaveChatMessage(string senderId, string receiverId, string message)
{
    try
    {
        using var conn = new SqlConnection(_conn);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_SaveChatMessage", conn);
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@SenderId", senderId);
        cmd.Parameters.AddWithValue("@ReceiverId", receiverId);
        cmd.Parameters.AddWithValue("@Message", message);
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex) { MessageBox.Show("SaveChatMessage failed: " + ex.Message); }
}

        public static async Task<List<ChatMessage>> GetChatMessages(string userId1, string userId2)
        {
            var list = new List<ChatMessage>();
            const string sql = @"
SELECT cm.Id, cm.SenderId, cm.ReceiverId, cm.Message, cm.SentAt
FROM ChatMessages cm
WHERE (cm.SenderId = @u1 AND cm.ReceiverId = @u2)
   OR (cm.SenderId = @u2 AND cm.ReceiverId = @u1)
ORDER BY cm.SentAt ASC";
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u1", userId1);
                cmd.Parameters.AddWithValue("@u2", userId2);
                using var r = await cmd.ExecuteReaderAsync();

                var rawMessages = new System.Collections.Generic.List<(int Id, string SenderId,
                    string ReceiverId, string Message, System.DateTime SentAt)>();
                while (await r.ReadAsync())
                    rawMessages.Add((
                        Convert.ToInt32(r["Id"]),
                        r["SenderId"].ToString() ?? "",
                        r["ReceiverId"].ToString() ?? "",
                        r["Message"].ToString() ?? "",
                        Convert.ToDateTime(r["SentAt"])
                    ));
                r.Close(); // Close reader so the connection is freed for subsequent queries

                // For each DirectTarget system message, extract the item name from the message text
                // then look up the specific item (only items between THIS sender and receiver pair)
                foreach (var raw in rawMessages)
                {
                    string linkedItemId = string.Empty;
                    string linkedItemPath = string.Empty;

                    if (raw.Message.Contains("reserved for you") &&
                        raw.Message.Contains("Item: \""))
                    {
                        // Extract item name from message: Item: "NAME" (
                        int start = raw.Message.IndexOf("Item: \"") + 7;
                        int end = raw.Message.IndexOf("\"", start);
                        if (start > 6 && end > start)
                        {
                            string itemName = raw.Message[start..end];
                            // Find the specific item for this donor->beneficiary pair with that name
                            // Find the specific item for this donor→beneficiary pair with that name
                            // Uses Post JOIN since PostType is now a lookup table
                            using var c2 = new SqlCommand(@"
    SELECT TOP 1 i.Item_ID, ISNULL(i.Item_ImagePath,'') AS Item_ImagePath
    FROM Items i
    JOIN Post p ON p.Post_ID = i.Post_ID
    WHERE i.Donor_ID              = @sid
      AND i.TargetBeneficiary_ID  = @rid
      AND i.Item_Name             = @iname
      AND p.Post_Type             = 'DirectTarget'
    ORDER BY i.Date_Found DESC", conn);
                            c2.Parameters.AddWithValue("@sid", raw.SenderId);
                            c2.Parameters.AddWithValue("@rid", raw.ReceiverId);
                            c2.Parameters.AddWithValue("@iname", itemName);
                            using var r2 = await c2.ExecuteReaderAsync();
                            if (await r2.ReadAsync())
                            {
                                linkedItemId = r2["Item_ID"].ToString() ?? "";
                                linkedItemPath = r2["Item_ImagePath"].ToString() ?? "";
                            }
                        }
                    }

                    bool alreadyActioned = false;
                    if (!string.IsNullOrEmpty(linkedItemId) && raw.ReceiverId == userId1)
                    {
                        // Check if beneficiary (userId1) already has a claim on this item
                        using var c3 = new SqlCommand(@"
        SELECT COUNT(*) FROM Claims
        WHERE Item_ID        = @iid
          AND Beneficiary_ID = @bid", conn);
    c3.Parameters.AddWithValue("@iid", linkedItemId);
    c3.Parameters.AddWithValue("@bid", userId1);
    var countObj = await c3.ExecuteScalarAsync();
    var count = (countObj is DBNull || countObj == null) ? 0 : Convert.ToInt32(countObj);
    alreadyActioned = count > 0;
}

                    list.Add(new ChatMessage
                    {
                        Id = raw.Id,
                        SenderId = raw.SenderId,
                        ReceiverId = raw.ReceiverId,
                        Text = raw.Message,
                        Time = raw.SentAt.ToString("HH:mm"),
                        LinkedItemId = linkedItemId,
                        LinkedItemPath = linkedItemPath,
                        IsFromUser = raw.SenderId == userId1,
                        // KEY FIX: if claim already exists → IsActionable=false → buttons stay hidden
                        IsActionable = !alreadyActioned
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show("GetChatMessages failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<(string UserId, string FullName, string LastMessage, int UnreadCount)>>
    GetChatDonors()
{
    var list = new List<(string, string, string, int)>();
    try
    {
        using var conn = new SqlConnection(_conn);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetChatDonors", conn);
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((
                r["Donor_ID"].ToString() ?? "",
                r["Donor_FullName"].ToString() ?? "",
                r["LastMessage"].ToString() ?? "",
                Convert.ToInt32(r["UnreadCount"])
            ));
    }
    catch (Exception ex) { MessageBox.Show("GetChatDonors failed: " + ex.Message); }
    return list;
}

        // ADD: RevertItemToGeneralPost (for Decline in chat)
        public static async Task RevertItemToGeneralPost(string itemId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE Items SET PostType='GeneralPost', TargetBeneficiary_ID='', Item_Status='Available' WHERE Item_ID=@id",
                    conn);
                cmd.Parameters.AddWithValue("@id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("RevertItemToGeneralPost failed: " + ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PROFILE UPDATE
        // ══════════════════════════════════════════════════════════════════════

        public static async Task UpdateDonorProfile(string donorId, string newUsername,
                                              string profilePic, string address = "")
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_UpdateDonorProfile", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DonorId", donorId);
                cmd.Parameters.AddWithValue("@NewUsername", newUsername);
                cmd.Parameters.AddWithValue("@ProfilePic", profilePic);
                cmd.Parameters.AddWithValue("@Address", address);   // NEW
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateDonorProfile failed: " + ex.Message); }
        }

        public static async Task<DonorModel?> GetDonorById(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT Donor_ID, Donor_FullName, Donor_Username, Donor_Address, " +
                    "Donor_ContactNumber, ProfilePicturePath FROM Donors WHERE Donor_ID=@id", conn);
                cmd.Parameters.AddWithValue("@id", donorId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return new DonorModel
                    {
                        Donor_ID = r["Donor_ID"].ToString() ?? "",
                        Donor_FullName = r["Donor_FullName"].ToString() ?? "",
                        Donor_Username = r["Donor_Username"].ToString() ?? "",
                        Donor_Address = r["Donor_Address"].ToString() ?? "",
                        Donor_ContactNumber = r["Donor_ContactNumber"].ToString() ?? "",
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? ""
                    };
            }
            catch (Exception ex) { MessageBox.Show("GetDonorById failed: " + ex.Message); }
            return null;
        }
        public static async Task<List<(string Id, string DisplayName)>> GetBeneficiariesByOrg(string orgId)
        {
            var list = new List<(string, string)>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT Beneficiary_ID, Beneficiary_FullName
            FROM Beneficiaries
            WHERE Organization_ID = @oid AND Beneficiaries_Status = 'Active'", conn);
                cmd.Parameters.AddWithValue("@oid", orgId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add((r["Beneficiary_ID"].ToString() ?? "",
                              r["Beneficiary_FullName"].ToString() ?? ""));
            }
            catch (Exception ex) { MessageBox.Show("GetBeneficiariesByOrg failed: " + ex.Message); }
            return list;
        }

        public static async Task UpdateBeneficiaryProfile(
    string beneficiaryId, string newUsername, string profilePic)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            UPDATE Beneficiaries
            SET Beneficiary_Username = @uname,
                ProfilePicturePath   = @pic
            WHERE Beneficiary_ID = @id", conn);
                cmd.Parameters.AddWithValue("@uname", newUsername);
                cmd.Parameters.AddWithValue("@pic", profilePic ?? "");
                cmd.Parameters.AddWithValue("@id", beneficiaryId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            { MessageBox.Show("UpdateBeneficiaryProfile failed: " + ex.Message); throw; }
        }

        public static async Task<BeneficiaryModel?> GetBeneficiaryById(string beneficiaryId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT b.Beneficiary_ID, b.Beneficiary_FullName,
                   b.Beneficiary_Username, b.Beneficiary_Contact,
                   b.Beneficiaries_Status, b.Organization_ID,
                   ISNULL(b.ProfilePicturePath,'') AS ProfilePicturePath,
                   ISNULL(o.Organization_Name,'')  AS Organization_Name
            FROM Beneficiaries b
            LEFT JOIN Organization o ON o.Organization_ID = b.Organization_ID
            WHERE b.Beneficiary_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", beneficiaryId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return new BeneficiaryModel
                    {
                        Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
                        Beneficiary_FullName = r["Beneficiary_FullName"].ToString() ?? "",
                        Beneficiary_Username = r["Beneficiary_Username"].ToString() ?? "",
                        Beneficiary_Contact = r["Beneficiary_Contact"].ToString() ?? "",
                        Beneficiaries_Status = r["Beneficiaries_Status"].ToString() ?? "Active",
                        Organization_ID = r["Organization_ID"].ToString() ?? "",
                        Organization_Name = r["Organization_Name"].ToString() ?? "",
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? ""
                    };
            }
            catch (Exception ex)
            { MessageBox.Show("GetBeneficiaryById failed: " + ex.Message); }
            return null;
        }

        public static async Task DeactivateAccount(string userId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_DeactivateAccount", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            { MessageBox.Show("DeactivateAccount failed: " + ex.Message); throw; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // FILE REPORTS
        // ══════════════════════════════════════════════════════════════════════

        public static void GenerateClaimReport(ClaimModel claim)
{
    try
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                   "KapwaKuhaData", "ClaimReports");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"Claim_{claim.Claim_ID}.txt");
        using var w = new StreamWriter(path, append: false);
        w.WriteLine("================================================");
        w.WriteLine("       KAPWAKUHA — ITEM CLAIM REPORT            ");
        w.WriteLine("================================================");
        w.WriteLine($"Claim ID        : {claim.Claim_ID}");
        w.WriteLine($"Claim Date      : {claim.Claim_Date:yyyy-MM-dd HH:mm:ss}");
        w.WriteLine("------------------------------------------------");
        w.WriteLine($"Item ID         : {claim.Item_ID}");
        w.WriteLine($"Item Name       : {claim.Item_Name}");
        w.WriteLine("------------------------------------------------");
        w.WriteLine($"Beneficiary ID  : {claim.Beneficiary_ID}");
        w.WriteLine($"Beneficiary     : {claim.Beneficiary_Name}");
        w.WriteLine("------------------------------------------------");
        w.WriteLine($"Handoff Type    : {claim.Handoff_Type}");
        w.WriteLine($"Status          : {claim.Claim_Status}");
        w.WriteLine($"Notes           : {claim.Verification_Notes}");
        w.WriteLine("================================================");
        w.WriteLine("  Kapwa — Together We Give What Others Need     ");
        w.WriteLine("================================================");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Claim report error: {ex.Message}",
            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

public static void GenerateDonationReceipt(ClaimModel claim, string donorName)
{
    try
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                   "KapwaKuhaData", "DonorReceipts");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"Receipt_{claim.Claim_ID}.txt");
        using var w = new StreamWriter(path, append: false);
        w.WriteLine("================================================");
        w.WriteLine("       KAPWAKUHA — DONATION RECEIPT             ");
        w.WriteLine("================================================");
        w.WriteLine($"Receipt for     : {donorName}");
        w.WriteLine($"Claim ID        : {claim.Claim_ID}");
        w.WriteLine($"Date            : {claim.Claim_Date:yyyy-MM-dd HH:mm:ss}");
        w.WriteLine("------------------------------------------------");
        w.WriteLine($"Item Donated    : {claim.Item_Name}");
        w.WriteLine($"Received By     : {claim.Beneficiary_Name}");
        w.WriteLine($"Handoff Method  : {claim.Handoff_Type}");
        w.WriteLine("================================================");
        w.WriteLine("  Thank you for your generosity, Kapwa!         ");
        w.WriteLine("================================================");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Receipt error: {ex.Message}",
            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
    }
}

