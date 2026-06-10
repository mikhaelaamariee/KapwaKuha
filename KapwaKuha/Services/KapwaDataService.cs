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
        // Connection strings for all team members
        private static readonly string _pcConn =
            @"Server=CCL2-12\MSSQLSERVER01;Database=KapwaKuha_Database;User Id=sa;Password=ccl2;TrustServerCertificate=True;Connection Timeout=5;";

        private static readonly string _laptopConn =
            @"Server=DESKTOP-8P1VJSE;Database=KapwaKuha_Database;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;";

        // Yuan's new laptop
        private static readonly string _laptopConn1 =
        @"Server=LAPTOP-IDI5D94J\SQLEXPRESS;Database=KapwaKuha_Database;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;";

        // Teammate 1
        private static readonly string _team1Conn =
            @"Server=LAPTOP-3QIJJ85P\MSSQLSERVER06;Database=KapwaKuha_Database;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;";

        // Teammate 2
        private static readonly string _team2Conn =
            @"Server=KAORI\MSSQLSERVER01;Database=KapwaKuha_Database;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;";

        private static string? _cachedConn;

        private static string _conn
        {
            get
            {
                if (_cachedConn != null) return _cachedConn;

                // List of all possible strings to try
                string[] connectionStrings = { _pcConn, _laptopConn, _laptopConn1, _team1Conn, _team2Conn };

                foreach (var connectionString in connectionStrings)
                {
                    try
                    {
                        using var t = new SqlConnection(connectionString);
                        // The connection will now safely give up after 3 seconds if the server is offline
                        t.Open();
                        return _cachedConn = connectionString;
                    }
                    catch
                    {
                        // If this one fails, it moves to the next one in the list automatically
                        continue;
                    }
                }

                // Default fallback if everything fails
                return _laptopConn;
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
           u.IsActive, u.IsBlacklisted, u.Admin_Approval_Status
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
                    bool isBlacklist = Convert.ToBoolean(r["IsBlacklisted"]);
                    r.Close();

                    // FIX: block blacklisted users entirely — no reactivation path
                    if (isBlacklist)
                    {
                        System.Windows.MessageBox.Show(
                            "Your account has been permanently suspended due to policy violations.\nContact support if you believe this is an error.",
                            "Account Suspended", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return (false, "", "", "");
                    }

                    if (!isActive)
                    {
                        var reactivate = System.Windows.MessageBox.Show(
                            "Your account is currently deactivated.\n\nWould you like to reactivate it?",
                            "Account Deactivated",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);
                        if (reactivate != System.Windows.MessageBoxResult.Yes)
                            return (false, "", "", "");
                        using var c1 = new SqlCommand(
                            "UPDATE Users SET IsActive = 1 WHERE UserID = @id", conn);
                        c1.Parameters.AddWithValue("@id", userId);
                        await c1.ExecuteNonQueryAsync();
                        using var c2 = new SqlCommand(
                            "UPDATE Donors SET Donor_AccountStatus = 'Active' WHERE Donor_ID = @id", conn);
                        c2.Parameters.AddWithValue("@id", userId);
                        await c2.ExecuteNonQueryAsync();
                        System.Windows.MessageBox.Show("Account successfully reactivated! Welcome back.", "Success");
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
           u.IsActive, u.IsBlacklisted, u.Admin_Approval_Status
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
                    bool isBlacklist = Convert.ToBoolean(r["IsBlacklisted"]);
                    string approval = r["Admin_Approval_Status"].ToString() ?? "";
                    r.Close();

                    if (isBlacklist)
                    {
                        System.Windows.MessageBox.Show(
                            "Your account has been permanently suspended.",
                            "Account Suspended", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return (false, "", "", "");
                    }

                    // FIX: gate on approval for institutional beneficiaries
                    if (approval == "Pending")
                    {
                        System.Windows.MessageBox.Show(
                            "Your account is still awaiting admin approval.\nYou will be notified once approved.",
                            "Pending Approval", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                        return (false, "", "", "");
                    }
                    if (approval == "Rejected")
                    {
                        System.Windows.MessageBox.Show(
                            "Your account application was rejected.\nContact support for more information.",
                            "Application Rejected", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return (false, "", "", "");
                    }

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

                // After registration, immediately persist address + profile pic
                if (!string.IsNullOrEmpty(donor.ProfilePicturePath) || !string.IsNullOrEmpty(donor.Donor_Address))
                {
                    await UpdateDonorProfile(donor.Donor_ID, donor.Donor_Username,
                        donor.ProfilePicturePath ?? "", donor.Donor_Address ?? "");
                }
            }
            catch (Exception ex) { MessageBox.Show("RegisterDonor failed: " + ex.Message); throw; }
        }

        public static async Task RegisterBeneficiary(BeneficiaryModel bene, string password, string securityQuestion, string securityAnswer)
        {
            string fullName = string.IsNullOrWhiteSpace(bene.Beneficiary_FullName)
                ? $"{bene.Beneficiary_FName} {bene.Beneficiary_LName}".Trim()
                : bene.Beneficiary_FullName;

            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_RegisterBeneficiary", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", bene.Beneficiary_ID);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Username", bene.Beneficiary_Username);
                cmd.Parameters.AddWithValue("@Sex", bene.Beneficiary_Sex);
                cmd.Parameters.AddWithValue("@Contact", bene.Beneficiary_Contact);
                cmd.Parameters.AddWithValue("@OrgName", bene.Organization_Name);
                cmd.Parameters.AddWithValue("@OrgAddress", string.IsNullOrWhiteSpace(bene.Organization_Address) ? (object)DBNull.Value : bene.Organization_Address);
                cmd.Parameters.AddWithValue("@OrgContact", string.IsNullOrWhiteSpace(bene.Organization_Contact) ? (object)DBNull.Value : bene.Organization_Contact);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@SecurityQ", securityQuestion);
                cmd.Parameters.AddWithValue("@SecurityA", securityAnswer);
                await cmd.ExecuteNonQueryAsync();

                if (!string.IsNullOrEmpty(bene.ProfilePicturePath))
                    await UpdateBeneficiaryProfile(bene.Beneficiary_ID, bene.Beneficiary_Username, bene.ProfilePicturePath);
            }
            catch (Exception ex) { MessageBox.Show("Registration failed: " + ex.Message); throw; }
        }
        public static string GetClaimReportPath(string claimId)
        {
            string dir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "KapwaKuhaData", "ClaimReports");
            return System.IO.Path.Combine(dir, $"Claim_{claimId}.txt");
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

                // Fix: Changed Beneficiary_ID to Beneficiary_Username in the WHERE clause
                using var cmd = new SqlCommand(@"
            SELECT Donor_ID AS UserId, SecurityQuestion FROM Donors WHERE Donor_Username = @u
            UNION ALL
            SELECT Beneficiary_ID AS UserId, SecurityQuestion FROM Beneficiaries WHERE Beneficiary_Username = @u", conn);

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
       ISNULL(p.Post_Type,'GeneralPost')         AS PostType,
       ISNULL(i.TargetBeneficiary_ID,'')         AS TargetBeneficiary_ID,
       ISNULL(i.Item_Description,'')             AS Item_Description,
       ISNULL(i.Item_ImagePath,'')               AS Item_ImagePath,
       ISNULL(i.Admin_Approval_Status,'Pending') AS Admin_Approval_Status,
       ISNULL(i.RejectionNote,'')               AS RejectionNote,
       d.Donor_FullName                          AS Donor_Name,
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
       i.TargetBeneficiary_ID, i.Item_ImagePath,
       ISNULL(i.Admin_Approval_Status,'Pending') AS Admin_Approval_Status
FROM Items i
JOIN Donors   d ON d.Donor_ID    = i.Donor_ID
JOIN Category c ON c.Category_ID = i.Category_ID
JOIN Post     p ON p.Post_ID     = i.Post_ID
WHERE p.Post_Type = 'DirectTarget'
  AND i.TargetBeneficiary_ID = @bid
  AND i.Admin_Approval_Status = 'Approved'";
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
                        Item_ImagePath = r["Item_ImagePath"].ToString() ?? "",
                        Admin_Approval_Status = r["Admin_Approval_Status"].ToString() ?? "Pending"
                    });
            }
            catch (Exception ex) { MessageBox.Show("GetDirectTargetItems failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<ItemModel>> GetAvailableItems()
        {
            var all = await GetAllItems();
            return all.FindAll(i =>
                i.Item_Status == "Available" &&
                i.PostType == "GeneralPost" &&
                i.Admin_Approval_Status == "Approved");  // FIX: block unapproved items from browse
        }


        public static async Task<List<ItemModel>> GetItemsByDonor(string donorId)
        {
            var all = await GetAllItems();
            return all.FindAll(i => i.Donor_ID == donorId);
            // Returns Pending + Approved + Rejected — donor sees everything they posted
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

        public static async Task UploadProofOfReceipt(string receiptId, string claimId, string filePath, string verificationId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                // Verification_Status column: 'Pending' = V001 logic, 'Released' = V002 logic
                string statusVal = verificationId == "V002" ? "Released" : "Pending";
                var cmd = new SqlCommand(@"
    INSERT INTO ProofOfReceipt
        (Receipt_ID, Claim_ID, FilePath, UploadDate, Verification_Status)
    VALUES
        (@ReceiptId, @ClaimId, @FilePath, GETDATE(), @VerifStatus)", conn);
                cmd.Parameters.AddWithValue("@ReceiptId", receiptId);
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.AddWithValue("@VerifStatus", statusVal);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("UploadProofOfReceipt failed: " + ex.Message);
            }
        }
        public static async Task SaveProofOfReceipt(string claimId, string filePath)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                // Get next receipt ID
                string receiptId = "R001";
                using (var cmd = new SqlCommand("sp_GetNextReceiptId", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync()) receiptId = r["NextId"].ToString() ?? "R001";
                }

                // Default verification = V001 (Pending)
                // Check claim status to assign correct verification
                string verifId = "V001"; // Pending default
                using (var statusCmd = new SqlCommand(
                    "SELECT Claim_Status FROM Claims WHERE Claim_ID = @cid", conn))
                {
                    statusCmd.Parameters.AddWithValue("@cid", claimId);
                    var statusResult = await statusCmd.ExecuteScalarAsync();
                    if (statusResult?.ToString() == "Released")
                        verifId = "V002";
                }

                using var cmd2 = new SqlCommand("sp_SaveProofOfReceipt", conn);
                cmd2.CommandType = System.Data.CommandType.StoredProcedure;
                cmd2.Parameters.AddWithValue("@ReceiptId", receiptId);
                cmd2.Parameters.AddWithValue("@ClaimId", claimId);
                cmd2.Parameters.AddWithValue("@FilePath", filePath);
                cmd2.Parameters.AddWithValue("@VerifId", verifId);
                await cmd2.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("SaveProofOfReceipt failed: " + ex.Message); }
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
                using var cmd = new SqlCommand(
       "SELECT Category_ID, Category_Name FROM Category " +
       "ORDER BY CASE WHEN Category_Name = 'Others' THEN 1 ELSE 0 END, Category_Name ASC", conn);
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
            Item_ImagePath = r["Item_ImagePath"].ToString() ?? "",
            Admin_Approval_Status = r["Admin_Approval_Status"].ToString() ?? "Pending",
            RejectionNote = r["RejectionNote"].ToString() ?? ""
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
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetAllClaims", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
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
               ISNULL(cl.Handoff_Type,'Pickup')                         AS Handoff_Type,
               ISNULL(i.Item_Name,'')                                   AS Item_Name,
               ISNULL(i.Item_ImagePath,'')                              AS Item_ImagePath,
               ISNULL(c.Category_Name,'')                               AS Category_Name,
               ISNULL(i.Donor_ID,'')                                    AS Donor_ID,
               ISNULL(d.Donor_FullName,'')                              AS Donor_Name,
               ISNULL(ib.Beneficiary_FullName, ISNULL(ind.FullName,'')) AS Beneficiary_Name
        FROM Claims cl
        LEFT JOIN Items                      i   ON i.Item_ID          = cl.Item_ID
        LEFT JOIN Category                   c   ON c.Category_ID      = i.Category_ID
        LEFT JOIN Donors                     d   ON d.Donor_ID         = i.Donor_ID
        LEFT JOIN InstitutionalBeneficiaries ib  ON ib.Beneficiary_ID  = cl.InstitutionalBene_ID
        LEFT JOIN IndependentBeneficiaries   ind ON ind.IndepBene_ID   = cl.IndependentBene_ID
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

                if (newStatus == "Released")
                {
                    // Update ProofOfReceipt verification to Released
                    // FIX: was Verification_ID = 'V002' — column is Verification_Status
                    using var verifCmd = new SqlCommand(
                        "UPDATE ProofOfReceipt SET Verification_Status = 'Released' WHERE Claim_ID = @cid", conn);
                    verifCmd.Parameters.AddWithValue("@cid", claimId);
                    await verifCmd.ExecuteNonQueryAsync();


                    // If the claimed item was a DirectTarget, mark the matching NeedsPost as Fulfilled
                    using var needsCmd = new SqlCommand(@"
UPDATE NeedsPosts SET Status = 'Fulfilled'
WHERE Status = 'Open'
  AND Org_ID IN (
      SELECT ISNULL(ib.Organization_ID, '')
      FROM Claims cl
      LEFT JOIN InstitutionalBeneficiaries ib ON ib.Beneficiary_ID = cl.InstitutionalBene_ID
      WHERE cl.Claim_ID = @cid2
        AND cl.InstitutionalBene_ID IS NOT NULL
  )
  AND NeedsPost_ID IN (
      SELECT TOP 1 n.NeedsPost_ID
      FROM Claims cl
      JOIN Items i ON i.Item_ID = cl.Item_ID
      JOIN Post p ON p.Post_ID = i.Post_ID
      LEFT JOIN InstitutionalBeneficiaries ib ON ib.Beneficiary_ID = cl.InstitutionalBene_ID
      JOIN NeedsPosts n ON n.Org_ID = ib.Organization_ID
      WHERE cl.Claim_ID = @cid2
        AND p.Post_Type = 'DirectTarget'
        AND n.Status = 'Open'
        AND cl.InstitutionalBene_ID IS NOT NULL
      ORDER BY n.Post_Date DESC
  )", conn);
                    needsCmd.Parameters.AddWithValue("@cid2", claimId);
                    await needsCmd.ExecuteNonQueryAsync();
                }
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
               ISNULL(cl.Handoff_Type,'Pickup')                         AS Handoff_Type,
               ISNULL(i.Item_Name,'')                                   AS Item_Name,
               ISNULL(i.Item_ImagePath,'')                              AS Item_ImagePath,
               ISNULL(c.Category_Name,'')                               AS Category_Name,
               ISNULL(i.Donor_ID,'')                                    AS Donor_ID,
               ISNULL(d.Donor_FullName,'')                              AS Donor_Name,
               ISNULL(ib.Beneficiary_FullName, ISNULL(ind.FullName,'')) AS Beneficiary_Name
        FROM Claims cl
        LEFT JOIN Items                      i   ON i.Item_ID          = cl.Item_ID
        LEFT JOIN Category                   c   ON c.Category_ID      = i.Category_ID
        LEFT JOIN Donors                     d   ON d.Donor_ID         = i.Donor_ID
        LEFT JOIN InstitutionalBeneficiaries ib  ON ib.Beneficiary_ID  = cl.InstitutionalBene_ID
        LEFT JOIN IndependentBeneficiaries   ind ON ind.IndepBene_ID   = cl.IndependentBene_ID
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
            Category_Name = r["Category_Name"].ToString() ?? "",
            Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
            Beneficiary_Name = r["Beneficiary_Name"].ToString() ?? "",
            Donor_ID = r["Donor_ID"].ToString() ?? "",
            Donor_Name = r["Donor_Name"].ToString() ?? "",
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
SET PreviousTitle         = Title,
    PreviousDescription   = Description,
    PreviousUrgency       = Urgency,
    Title                 = @title,
    Description           = @desc,
    Urgency               = @urg,
    ImagePath             = @img,
    Admin_Approval_Status = 'Pending',
    RejectionNote         = NULL
WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@title", post.Title);
                cmd.Parameters.AddWithValue("@desc", post.Description);
                cmd.Parameters.AddWithValue("@urg", post.Urgency);
                cmd.Parameters.AddWithValue("@img", post.ImagePath ?? "");
                cmd.Parameters.AddWithValue("@id", post.NeedsPost_ID);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateNeedsPost failed: " + ex.Message); throw; }
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

        public static async Task FulfillNeedsPost(string postId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE NeedsPosts SET Status = 'Fulfilled' WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("FulfillNeedsPost failed: " + ex.Message); }
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
       ISNULL(n.ImagePath,'')          AS ImagePath,
       ISNULL(o.Organization_Name,'') AS Org_Name,
       n.Admin_Approval_Status,
       ISNULL(n.RejectionNote,'')     AS RejectionNote
FROM NeedsPosts n
LEFT JOIN Organization o ON o.Organization_ID = n.Org_ID
WHERE n.Org_ID = @oid
  AND n.Status = 'Open'
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
                        ImagePath = r["ImagePath"].ToString() ?? "",
                        Admin_Approval_Status = r["Admin_Approval_Status"].ToString() ?? "Pending",
                        RejectionNote = r["RejectionNote"].ToString() ?? "",
                    });
            }
            catch (Exception ex) { MessageBox.Show("GetNeedsPostsByOrg failed: " + ex.Message); }
            return list;
        }



        public static async Task UpdateItem(ItemModel item)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
UPDATE Items
SET Item_Name             = @name,
    Item_Description      = @desc,
    Item_Condition        = @cond,
    Item_ImagePath        = @img,
    Admin_Approval_Status = 'Pending',
    RejectionNote         = NULL
WHERE Item_ID = @id", conn);
                cmd.Parameters.AddWithValue("@name", item.Item_Name);
                cmd.Parameters.AddWithValue("@desc", item.Item_Description);
                cmd.Parameters.AddWithValue("@cond", item.Item_Condition);
                cmd.Parameters.AddWithValue("@img", item.Item_ImagePath ?? "");
                cmd.Parameters.AddWithValue("@id", item.Item_ID);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("UpdateItem failed: " + ex.Message); throw; }
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
        // ══════════════════════════════════════════════════════════════════════
        // CHAT LIST FETCHING (For ChatListViewModel)
        // ══════════════════════════════════════════════════════════════════════
        /// <summary>Returns ALL active beneficiaries for donor chat search.</summary>
        public static async Task<List<BeneficiaryModel>> GetAllBeneficiariesForChat()
        {
            var list = new List<BeneficiaryModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                // Fetch all active beneficiaries (Removed b.Organization_Name)
                using var cmd = new SqlCommand(@"
            SELECT b.Beneficiary_ID, 
                   b.Beneficiary_FullName, 
                   b.Beneficiary_Username, 
                   ISNULL(b.ProfilePicturePath, '') AS ProfilePicturePath
            FROM Beneficiaries b
            INNER JOIN Users u ON u.UserID = b.Beneficiary_ID
            WHERE u.IsActive = 1", conn);

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BeneficiaryModel
                    {
                        Beneficiary_ID = r["Beneficiary_ID"].ToString() ?? "",
                        Beneficiary_FullName = r["Beneficiary_FullName"].ToString() ?? "",
                        Beneficiary_Username = r["Beneficiary_Username"].ToString() ?? "",
                        Organization_Name = "", // Assigned an empty string to avoid errors
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetAllBeneficiariesForChat failed: " + ex.Message);
            }
            return list;
        }

        public static async Task<List<DonorModel>> GetAllDonorsForChat()
        {
            var list = new List<DonorModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();

                // Fetch all active donors
                using var cmd = new SqlCommand(@"
                    SELECT d.Donor_ID, 
                           d.Donor_FullName, 
                           d.Donor_Username, 
                           ISNULL(d.ProfilePicturePath, '') AS ProfilePicturePath
                    FROM Donors d
                    INNER JOIN Users u ON u.UserID = d.Donor_ID
                    WHERE u.IsActive = 1", conn);

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new DonorModel
                    {
                        Donor_ID = r["Donor_ID"].ToString() ?? "",
                        Donor_FullName = r["Donor_FullName"].ToString() ?? "",
                        Donor_Username = r["Donor_Username"].ToString() ?? "",
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetAllDonorsForChat failed: " + ex.Message);
            }
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

                // 1. Total items donated by THIS donor
                using var c1 = new SqlCommand("SELECT COUNT(*) FROM Items WHERE Donor_ID=@did", conn);
                c1.Parameters.AddWithValue("@did", donorId);
                int total = Convert.ToInt32(await c1.ExecuteScalarAsync());

                // 2. Items by THIS donor that are claimed
                using var c2 = new SqlCommand("SELECT COUNT(*) FROM Items WHERE Donor_ID=@did AND Item_Status='Claimed'", conn);
                c2.Parameters.AddWithValue("@did", donorId);
                int claimed = Convert.ToInt32(await c2.ExecuteScalarAsync());

                // 3. Items by THIS donor still available
                using var c3 = new SqlCommand("SELECT COUNT(*) FROM Items WHERE Donor_ID=@did AND Item_Status='Available'", conn);
                c3.Parameters.AddWithValue("@did", donorId);
                int active = Convert.ToInt32(await c3.ExecuteScalarAsync());

                // 4. FIX: NeedsPosts fulfilled specifically by THIS donor's items
                // We join Items to Claims to NeedsPosts (or similar logic depending on your flow)
                using var c4 = new SqlCommand(@"
            SELECT COUNT(DISTINCT n.NeedsPost_ID) 
            FROM NeedsPosts n
            INNER JOIN Items i ON i.TargetBeneficiary_ID <> '' -- Assuming you track fulfillment via items
            WHERE i.Donor_ID = @did AND n.Status = 'Fulfilled'", conn);
                c4.Parameters.AddWithValue("@did", donorId);
                int fulfilled = Convert.ToInt32(await c4.ExecuteScalarAsync());

                // 5. FIX: Beneficiaries who have specifically received items from THIS donor
                using var c5 = new SqlCommand(@"
            SELECT COUNT(DISTINCT cl.Beneficiary_ID) 
            FROM Claims cl
            INNER JOIN Items i ON cl.Item_ID = i.Item_ID
            WHERE i.Donor_ID = @did AND cl.Claim_Status = 'Released'", conn);
                c5.Parameters.AddWithValue("@did", donorId);
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
                // For each DirectTarget system message, extract the item name from the message text
                // then look up the specific item (only items between THIS sender and receiver pair)
                // For each DirectTarget system message, extract the item name from the message text
                // then look up the specific item (only items between THIS sender and receiver pair)
                foreach (var raw in rawMessages)
                {
                    string linkedItemId = string.Empty;
                    string linkedItemPath = string.Empty;

                    if (raw.Message.Contains("reserved for you"))
                    {
                        // ── Extract item name robustly ────────────────────────────────
                        // Message format from trigger:
                        //   📦 A donation has been reserved for you! Item: "NAME" (condition, category)...
                        // But if Item_Name itself contained quotes the DB stores: Item: ""NAME""
                        // So we find "Item:" then strip ALL surrounding quote characters.

                        string itemName = string.Empty;
                        int colonIdx = raw.Message.IndexOf("Item:");
                        if (colonIdx >= 0)
                        {
                            // Grab everything after "Item:"
                            string afterColon = raw.Message[(colonIdx + 5)..].TrimStart();

                            // Strip any number of leading quotes (handles " and "")
                            int nameStart = 0;
                            while (nameStart < afterColon.Length && afterColon[nameStart] == '"')
                                nameStart++;

                            // From nameStart, find the next quote — that ends the name
                            int nameEnd = afterColon.IndexOf('"', nameStart);
                            if (nameEnd > nameStart)
                                itemName = afterColon[nameStart..nameEnd].Trim();

                            // Final safety: strip any stray quotes the name itself might have
                            itemName = itemName.Trim('"').Trim();
                        }

                        if (!string.IsNullOrEmpty(itemName))
                        {
                            // ── DB lookup: match by donor, beneficiary, cleaned item name ──
                            using var c2 = new SqlCommand(@"
SELECT TOP 1 i.Item_ID, ISNULL(i.Item_ImagePath,'') AS Item_ImagePath
FROM Items i
JOIN Post p ON p.Post_ID = i.Post_ID
WHERE i.Donor_ID             = @sid
  AND i.TargetBeneficiary_ID = @rid
  AND LTRIM(RTRIM(REPLACE(i.Item_Name, CHAR(34), ''))) = LTRIM(RTRIM(@iname))
  AND p.Post_Type            = 'DirectTarget'
ORDER BY i.Date_Found DESC", conn);
                            c2.Parameters.AddWithValue("@sid", raw.SenderId);
                            c2.Parameters.AddWithValue("@rid", raw.ReceiverId);
                            // Strip quotes from param too so both sides match cleanly
                            c2.Parameters.AddWithValue("@iname", itemName.Replace("\"", ""));
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

                // NEW BEHAVIOUR: item is deactivated (Reserved→Available) but stays DirectTarget
                // Donor must manually re-post it — do NOT move it to GeneralPost automatically
                // Just clear the reservation so it doesn't block the beneficiary's view
                using var cmd = new SqlCommand(@"
            UPDATE Items
            SET Item_Status          = 'Available',
                TargetBeneficiary_ID = '',
                Post_ID              = (SELECT Post_ID FROM Post WHERE Post_Type = 'GeneralPost')
            WHERE Item_ID = @id", conn);
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
     string beneficiaryId, string newUsername, string profilePic,
     string orgName = "", string orgAddress = "", string orgContact = "")
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_UpdateBeneficiaryProfile", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", beneficiaryId);
                cmd.Parameters.AddWithValue("@NewUsername", newUsername);
                cmd.Parameters.AddWithValue("@ProfilePic", profilePic ?? "");
                cmd.Parameters.AddWithValue("@OrgName", string.IsNullOrWhiteSpace(orgName) ? (object)DBNull.Value : orgName);
                cmd.Parameters.AddWithValue("@OrgAddress", string.IsNullOrWhiteSpace(orgAddress) ? (object)DBNull.Value : orgAddress);
                cmd.Parameters.AddWithValue("@OrgContact", string.IsNullOrWhiteSpace(orgContact) ? (object)DBNull.Value : orgContact);
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
                   ISNULL(o.Organization_Name,'') AS Organization_Name, -- <-- COMMA ADDED HERE
                   ISNULL(o.Organization_Address,'') AS Organization_Address,
                   ISNULL(o.Organization_Contact,'') AS Organization_Contact
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
                        ProfilePicturePath = r["ProfilePicturePath"].ToString() ?? "",
                        Organization_Address = r["Organization_Address"].ToString() ?? "",
                        Organization_Contact = r["Organization_Contact"].ToString() ?? "",
                    };
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetBeneficiaryById failed: " + ex.Message);
            }

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
        // ══════════════════════════════════════════════════════════════════════
        // AUTH — Independent Beneficiary login (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<(bool OK, string UserId, string FullName, string Username)>
            LoginIndependentBeneficiary(string username, string password)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT ib.IndepBene_ID, ib.FullName, ib.Username, u.IsActive,
                   u.Admin_Approval_Status
            FROM IndependentBeneficiaries ib
            INNER JOIN Users u ON u.UserID = ib.IndepBene_ID
            WHERE ib.Username = @uname AND u.Password = @pw", conn);
                cmd.Parameters.AddWithValue("@uname", username);
                cmd.Parameters.AddWithValue("@pw", password);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    string userId = r["IndepBene_ID"].ToString() ?? "";
                    string fullName = r["FullName"].ToString() ?? "";
                    string uname = r["Username"].ToString() ?? "";
                    bool isActive = Convert.ToBoolean(r["IsActive"]);
                    string approval = r["Admin_Approval_Status"].ToString() ?? "";
                    r.Close();

                    if (approval == "Pending")
                    {
                        System.Windows.MessageBox.Show(
                            "Your account is still under Admin review.\nPlease wait for approval before logging in.",
                            "Pending Approval", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return (false, "", "", "");
                    }
                    if (approval == "Rejected")
                    {
                        System.Windows.MessageBox.Show(
                            "Your account registration was rejected.\nPlease contact support.",
                            "Account Rejected", System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return (false, "", "", "");
                    }
                    if (!isActive)
                    {
                        var reactivate = System.Windows.MessageBox.Show(
                            "Your account is currently deactivated.\n\nWould you like to reactivate it?",
                            "Account Deactivated",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);
                        if (reactivate != System.Windows.MessageBoxResult.Yes)
                            return (false, "", "", "");
                        using var c1 = new SqlCommand(
                            "UPDATE Users SET IsActive = 1 WHERE UserID = @id", conn);
                        c1.Parameters.AddWithValue("@id", userId);
                        await c1.ExecuteNonQueryAsync();
                        using var c2 = new SqlCommand(
                            "UPDATE IndependentBeneficiaries SET AccountStatus = 'Active' WHERE IndepBene_ID = @id", conn);
                        c2.Parameters.AddWithValue("@id", userId);
                        await c2.ExecuteNonQueryAsync();
                    }
                    return (true, userId, fullName, uname);
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("LoginIndependentBeneficiary failed: " + ex.Message); }
            return (false, "", "", "");
        }

        // ══════════════════════════════════════════════════════════════════════
        // AUTH — Admin login (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<(bool OK, string UserId, string FullName)>
            LoginAdmin(string adminId, string password)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT a.Admin_ID, a.Admin_FullName, u.IsActive
            FROM Admins a
            INNER JOIN Users u ON u.UserID = a.Admin_ID
            WHERE a.Admin_ID = @id AND u.Password = @pw
              AND u.Role = 'Admin'", conn);
                cmd.Parameters.AddWithValue("@id", adminId);
                cmd.Parameters.AddWithValue("@pw", password);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    string userId = r["Admin_ID"].ToString() ?? "";
                    string fullName = r["Admin_FullName"].ToString() ?? "";
                    bool isActive = Convert.ToBoolean(r["IsActive"]);
                    r.Close();
                    if (!isActive)
                    {
                        System.Windows.MessageBox.Show("Admin account is deactivated.");
                        return (false, "", "");
                    }
                    // Update LastLogin
                    using var upd = new SqlCommand(
                        "UPDATE Admins SET LastLogin = GETDATE() WHERE Admin_ID = @id", conn);
                    upd.Parameters.AddWithValue("@id", userId);
                    await upd.ExecuteNonQueryAsync();
                    return (true, userId, fullName);
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("LoginAdmin failed: " + ex.Message); }
            return (false, "", "");
        }

        // ══════════════════════════════════════════════════════════════════════
        // REGISTRATION — Independent Beneficiary (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<string> GetNextIndependentBeneficiaryId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM IndependentBeneficiaries", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"IB{n + 1:D3}";
            }
            catch { return $"IB{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task RegisterIndependentBeneficiary(
            IndependentBeneficiaryModel bene, string password,
            string securityQuestion, string securityAnswer)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_RegisterIndependentBeneficiary", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IndepBeneId", bene.IndepBene_ID);
                cmd.Parameters.AddWithValue("@FullName", bene.FullName);
                cmd.Parameters.AddWithValue("@Username", bene.Username);
                cmd.Parameters.AddWithValue("@Sex", bene.Sex);
                cmd.Parameters.AddWithValue("@Contact", bene.ContactNumber);
                cmd.Parameters.AddWithValue("@Address",
                    string.IsNullOrWhiteSpace(bene.Address) ? (object)DBNull.Value : bene.Address);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@SecurityQ", securityQuestion);
                cmd.Parameters.AddWithValue("@SecurityA", securityAnswer);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("RegisterIndependentBeneficiary failed: " + ex.Message); throw; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // FEEDBACK (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<string> GetNextFeedbackId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Feedback", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"FB{n + 1:D3}";
            }
            catch { return $"FB{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task SubmitFeedback(FeedbackModel fb)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_SubmitFeedback", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FeedbackId", fb.Feedback_ID);
                cmd.Parameters.AddWithValue("@DonorId", fb.Donor_ID);
                cmd.Parameters.AddWithValue("@ClaimId", fb.Claim_ID);
                cmd.Parameters.AddWithValue("@Stars", fb.Stars);
                cmd.Parameters.AddWithValue("@Comment",
                    string.IsNullOrWhiteSpace(fb.Comment) ? (object)DBNull.Value : fb.Comment);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("SubmitFeedback failed: " + ex.Message); throw; }
        }

        public static async Task<double> GetDonorAverageRating(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT dbo.fn_GetDonorAverageRating(@did)", conn);
                cmd.Parameters.AddWithValue("@did", donorId);
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result is DBNull ? 0.0 : Convert.ToDouble(result);
            }
            catch { return 0.0; }
        }

        public static async Task<bool> HasAlreadyRatedClaim(string claimId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Feedback WHERE Claim_ID = @cid", conn);
                cmd.Parameters.AddWithValue("@cid", claimId);
                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
            catch { return false; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // USER REPORTS (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<string> GetNextReportId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM UserReports", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"RPT{n + 1:D3}";
            }
            catch { return $"RPT{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task FileUserReport(UserReportModel report)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            INSERT INTO UserReports
                (Report_ID, Reporter_ID, Reported_ID, Report_Type,
                 Description, Status, Admin_Action_Taken, Filed_At, Admin_Notes)
            VALUES
                (@rid, @reporter, @reported, @type,
                 @desc, 'Open', 'None', GETDATE(), NULL)", conn);
                cmd.Parameters.AddWithValue("@rid", report.Report_ID);
                cmd.Parameters.AddWithValue("@reporter", report.Reporter_ID);
                cmd.Parameters.AddWithValue("@reported", report.Reported_ID);
                cmd.Parameters.AddWithValue("@type", report.Report_Type);
                cmd.Parameters.AddWithValue("@desc", report.Description);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("FileUserReport failed: " + ex.Message); throw; }
        }

        public static async Task<List<UserReportModel>> GetOpenReports()
        {
            var list = new List<UserReportModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT r.Report_ID, r.Reporter_ID, r.Reported_ID,
                   ISNULL(d.Donor_FullName,
                     ISNULL(ib.Beneficiary_FullName,
                       ISNULL(ind.FullName, r.Reported_ID))) AS Reported_Name,
                   r.Report_Type, r.Description, r.Status,
                   r.Admin_Action_Taken, r.Filed_At,
                   ISNULL(r.Admin_Notes,'') AS Admin_Notes
            FROM UserReports r
            LEFT JOIN Donors d ON d.Donor_ID = r.Reported_ID
            LEFT JOIN InstitutionalBeneficiaries ib ON ib.Beneficiary_ID = r.Reported_ID
            LEFT JOIN IndependentBeneficiaries ind ON ind.IndepBene_ID = r.Reported_ID
            WHERE r.Status = 'Open'
            ORDER BY r.Filed_At DESC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new UserReportModel
                    {
                        Report_ID = reader["Report_ID"].ToString() ?? "",
                        Reporter_ID = reader["Reporter_ID"].ToString() ?? "",
                        Reported_ID = reader["Reported_ID"].ToString() ?? "",
                        Reported_Name = reader["Reported_Name"].ToString() ?? "",
                        Report_Type = reader["Report_Type"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        Status = reader["Status"].ToString() ?? "Open",
                        Admin_Action_Taken = reader["Admin_Action_Taken"].ToString() ?? "None",
                        Filed_At = Convert.ToDateTime(reader["Filed_At"]),
                        Admin_Notes = reader["Admin_Notes"].ToString() ?? ""
                    });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetOpenReports failed: " + ex.Message); }
            return list;
        }

        // ══════════════════════════════════════════════════════════════════════
        // NOTIFICATIONS (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<string> GetNextNotifId()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Notifications", conn);
                int n = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return $"NTF{n + 1:D3}";
            }
            catch { return $"NTF{DateTime.Now.Ticks % 900 + 100:D3}"; }
        }

        public static async Task CreateNotification(
            string recipientId, string notifType, string message, string referenceId = "")
        {
            try
            {
                string notifId = await GetNextNotifId();
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_CreateNotification", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotifId", notifId);
                cmd.Parameters.AddWithValue("@RecipientId", recipientId);
                cmd.Parameters.AddWithValue("@NotifType", notifType);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("CreateNotification failed: " + ex.Message); }
        }

        public static async Task<List<NotificationModel>> GetNotificationsForUser(string userId)
        {
            var list = new List<NotificationModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT Notif_ID, Recipient_ID, Notif_Type, Message,
                   IsRead, SentAt, Reference_ID
            FROM Notifications
            WHERE Recipient_ID = @uid
            ORDER BY SentAt DESC", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new NotificationModel
                    {
                        Notif_ID = reader["Notif_ID"].ToString() ?? "",
                        Recipient_ID = reader["Recipient_ID"].ToString() ?? "",
                        Notif_Type = reader["Notif_Type"].ToString() ?? "",
                        Message = reader["Message"].ToString() ?? "",
                        IsRead = Convert.ToBoolean(reader["IsRead"]),
                        SentAt = Convert.ToDateTime(reader["SentAt"]),
                        Reference_ID = reader["Reference_ID"].ToString() ?? ""
                    });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetNotificationsForUser failed: " + ex.Message); }
            return list;
        }

        public static async Task<int> GetUnreadNotificationCount(string userId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Notifications WHERE Recipient_ID = @uid AND IsRead = 0", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            catch { return 0; }
        }

        public static async Task MarkNotificationRead(string notifId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_MarkNotificationRead", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotifId", notifId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("MarkNotificationRead failed: " + ex.Message); }
        }

        public static async Task MarkAllNotificationsRead(string userId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE Notifications SET IsRead = 1 WHERE Recipient_ID = @uid AND IsRead = 0", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("MarkAllNotificationsRead failed: " + ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADMIN GATEKEEPER (NEW for finals)
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<ItemModel>> GetPendingItems()
        {
            var list = new List<ItemModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
            SELECT i.Item_ID, i.Item_Name, i.Item_Description, i.Item_Condition,
                   i.Item_Status, i.Date_Found, i.Donor_ID,
                   d.Donor_FullName AS Donor_Name,
                   i.Category_ID, c.Category_Name,
                   i.Post_ID, p.Post_Type AS PostType,
                   i.TargetBeneficiary_ID, i.Item_ImagePath,
                   i.Admin_Approval_Status
            FROM Items i
            JOIN Donors   d ON d.Donor_ID    = i.Donor_ID
            JOIN Category c ON c.Category_ID = i.Category_ID
            JOIN Post     p ON p.Post_ID     = i.Post_ID
            WHERE i.Admin_Approval_Status = 'Pending'
            ORDER BY i.Date_Found DESC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new ItemModel
                    {
                        Item_ID = reader["Item_ID"].ToString() ?? "",
                        Item_Name = reader["Item_Name"].ToString() ?? "",
                        Item_Description = reader["Item_Description"].ToString() ?? "",
                        Item_Condition = reader["Item_Condition"].ToString() ?? "",
                        Item_Status = reader["Item_Status"].ToString() ?? "",
                        Date_Found = Convert.ToDateTime(reader["Date_Found"]),
                        Donor_ID = reader["Donor_ID"].ToString() ?? "",
                        Donor_Name = reader["Donor_Name"].ToString() ?? "",
                        Category_ID = reader["Category_ID"].ToString() ?? "",
                        Category_Name = reader["Category_Name"].ToString() ?? "",
                        PostType = reader["PostType"].ToString() ?? "",
                        TargetBeneficiary_ID = reader["TargetBeneficiary_ID"].ToString() ?? "",
                        Item_ImagePath = reader["Item_ImagePath"].ToString() ?? "",
                        Admin_Approval_Status = reader["Admin_Approval_Status"].ToString() ?? "Pending"
                    });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetPendingItems failed: " + ex.Message); }
            return list;
        }

        public static async Task<List<BeneficiaryModel>> GetPendingBeneficiaries()
        {
            var list = new List<BeneficiaryModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
    -- Institutional
    SELECT b.Beneficiary_ID, b.Beneficiary_FullName, b.Beneficiary_Username,
           b.Beneficiary_Sex, b.Beneficiary_Contact,
           b.Beneficiaries_Status, b.Organization_ID,
           ISNULL(o.Organization_Name,'') AS Organization_Name,
           b.Admin_Approval_Status,
           'Institutional' AS BeneType
    FROM InstitutionalBeneficiaries b
    LEFT JOIN Organization o ON o.Organization_ID = b.Organization_ID
    WHERE b.Admin_Approval_Status = 'Pending'
    UNION ALL
    -- Independent
    SELECT ib.IndepBene_ID, ib.FullName, ib.Username,
           ib.Sex, ib.ContactNumber,
           ib.AccountStatus, '' AS Organization_ID,
           'Independent' AS Organization_Name,
           ib.Admin_Approval_Status,
           'Independent' AS BeneType
    FROM IndependentBeneficiaries ib
    WHERE ib.Admin_Approval_Status = 'Pending'
    ORDER BY Admin_Approval_Status DESC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new BeneficiaryModel
                    {
                        Beneficiary_ID = reader["Beneficiary_ID"].ToString() ?? "",
                        Beneficiary_FullName = reader["Beneficiary_FullName"].ToString() ?? "",
                        Beneficiary_Username = reader["Beneficiary_Username"].ToString() ?? "",
                        Beneficiary_Sex = reader["Beneficiary_Sex"].ToString() ?? "",
                        Beneficiary_Contact = reader["Beneficiary_Contact"].ToString() ?? "",
                        Beneficiaries_Status = reader["Beneficiaries_Status"].ToString() ?? "Active",
                        Organization_ID = reader["Organization_ID"].ToString() ?? "",
                        Organization_Name = reader["Organization_Name"].ToString() ?? ""
                    });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetPendingBeneficiaries failed: " + ex.Message); }
            return list;
        }

        public static async Task ApproveItem(string itemId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_ApproveItem", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("ApproveItem failed: " + ex.Message); throw; }
        }

        public static async Task RejectItem(string itemId, string rejectionNote)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
UPDATE Items
SET Admin_Approval_Status = 'Rejected',
    RejectionNote         = @note
WHERE Item_ID = @id", conn);
                cmd.Parameters.AddWithValue("@note", rejectionNote.Trim());
                cmd.Parameters.AddWithValue("@id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("RejectItem failed: " + ex.Message); throw; }
        }

        public static async Task ApproveBeneficiary(string beneficiaryId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_ApproveBeneficiary", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", beneficiaryId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("ApproveBeneficiary failed: " + ex.Message); throw; }
        }

        public static async Task RejectBeneficiary(string beneficiaryId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_RejectBeneficiary", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BeneficiaryId", beneficiaryId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("RejectBeneficiary failed: " + ex.Message); throw; }
        }

        public static async Task<(int TotalDonated, int TotalClaimed, int ActiveItems,
            int FulfilledNeeds, int ActiveInstBenes, int ActiveIndepBenes,
            int PendingItems, int PendingBeneficiaries, int OpenReports)>
            GetAdminImpactMetrics()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetImpactMetrics", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    return (
                        Convert.ToInt32(reader["TotalDonated"]),
                        Convert.ToInt32(reader["TotalClaimed"]),
                        Convert.ToInt32(reader["ActiveItems"]),
                        Convert.ToInt32(reader["FulfilledNeeds"]),
                        Convert.ToInt32(reader["ActiveInstBeneficiaries"]),
                        Convert.ToInt32(reader["ActiveIndepBeneficiaries"]),
                        Convert.ToInt32(reader["PendingItems"]),
                        Convert.ToInt32(reader["PendingBeneficiaries"]),
                        Convert.ToInt32(reader["OpenReports"])
                    );
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetAdminImpactMetrics failed: " + ex.Message); }
            return (0, 0, 0, 0, 0, 0, 0, 0, 0);


        }
        // ── UserProfileWindow helpers ─────────────────────────────────────────

        public static async Task<int> GetDonorTotalDonations(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT dbo.fn_GetDonorTotalDonations(@did)", conn);
                cmd.Parameters.AddWithValue("@did", donorId);
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result is DBNull ? 0 : Convert.ToInt32(result);
            }
            catch { return 0; }
        }

        public static async Task<List<ItemModel>> GetAvailableItemsByDonor(string donorId)
        {
            var all = await GetItemsByDonor(donorId);
            return all.FindAll(i =>
                i.Item_Status == "Available" &&
                i.Admin_Approval_Status == "Approved");
        }

        public static async Task<List<DonorModel>> GetPendingDonors()
        {
            var list = new List<DonorModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
SELECT d.Donor_ID, d.Donor_FullName, d.Donor_Username,
       d.Donor_ContactNumber, d.Donor_Address,
       d.Donor_AccountStatus, u.Admin_Approval_Status
FROM Donors d
JOIN Users u ON u.UserID = d.Donor_ID
WHERE u.Admin_Approval_Status = 'Pending'
ORDER BY d.Donor_ID", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new DonorModel
                    {
                        Donor_ID = r["Donor_ID"].ToString() ?? "",
                        Donor_FullName = r["Donor_FullName"].ToString() ?? "",
                        Donor_Username = r["Donor_Username"].ToString() ?? "",
                        Donor_ContactNumber = r["Donor_ContactNumber"].ToString() ?? "",
                        Donor_Address = r["Donor_Address"].ToString() ?? "",
                        Donor_AccountStatus = r["Donor_AccountStatus"].ToString() ?? "Active"
                    });
            }
            catch (Exception ex) { MessageBox.Show("GetPendingDonors failed: " + ex.Message); }
            return list;
        }

        public static async Task ApproveDonor(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
UPDATE Users SET Admin_Approval_Status = 'Approved', IsActive = 1
WHERE UserID = @id", conn);
                cmd.Parameters.AddWithValue("@id", donorId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("ApproveDonor failed: " + ex.Message); throw; }
        }

        public static async Task RejectDonor(string donorId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
UPDATE Users SET Admin_Approval_Status = 'Rejected', IsActive = 0
WHERE UserID = @id", conn);
                cmd.Parameters.AddWithValue("@id", donorId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("RejectDonor failed: " + ex.Message); throw; }
        }

        public static async Task<IndependentBeneficiaryModel?> GetIndependentBeneficiaryById(
            string indepBeneId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
                    SELECT IndepBene_ID, FullName, Username, Sex, ContactNumber,
                           Address, AccountStatus, ProfilePicturePath,
                           Admin_Approval_Status
                    FROM IndependentBeneficiaries
                    WHERE IndepBene_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", indepBeneId);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;
                return new IndependentBeneficiaryModel
                {
                    IndepBene_ID = r["IndepBene_ID"].ToString()!,
                    FullName = r["FullName"].ToString()!,
                    Username = r["Username"].ToString()!,
                    Sex = r["Sex"].ToString()!,
                    ContactNumber = r["ContactNumber"].ToString()!,
                    Address = r["Address"]?.ToString() ?? string.Empty,
                    AccountStatus = r["AccountStatus"].ToString()!,
                    ProfilePicturePath = r["ProfilePicturePath"]?.ToString() ?? string.Empty
                };
            }
            catch { return null; }
        }

        public static async Task ProcessUserReport(
            string reportId, string reportedId,
            string newStatus, string adminNotes, string actionTaken)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "EXEC sp_ProcessUserReport @ReportId, @ReportedId, @NewStatus, @AdminNotes, @ActionTaken",
                    conn);
                cmd.Parameters.AddWithValue("@ReportId", reportId);
                cmd.Parameters.AddWithValue("@ReportedId", reportedId);
                cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                cmd.Parameters.AddWithValue("@AdminNotes", adminNotes);
                cmd.Parameters.AddWithValue("@ActionTaken", actionTaken);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ProcessUserReport failed: " + ex.Message);
                throw;
            }
        }

        public static async Task AdminBanUser(string userId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
                    UPDATE Users
                    SET IsBlacklisted = 1, IsActive = 0
                    WHERE UserID = @uid", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("AdminBanUser failed: " + ex.Message);
                throw;
            }
        }

        public static async Task<string> GetUserRoleById(string userId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT Role FROM Users WHERE UserID = @uid", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }
        // ══════════════════════════════════════════════════════════════════════
        // NEEDS POST ADMIN GATEKEEPER
        // ══════════════════════════════════════════════════════════════════════

        public static async Task<List<NeedsPostModel>> GetPendingNeedsPosts()
        {
            var list = new List<NeedsPostModel>();
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
SELECT n.NeedsPost_ID, n.Org_ID, n.Title, n.Description,
       n.Urgency, n.Status, n.Post_Date,
       ISNULL(n.ImagePath,'')          AS ImagePath,
       ISNULL(o.Organization_Name,'') AS Org_Name,
       n.Admin_Approval_Status,
       n.PreviousTitle,
       n.PreviousDescription,
       n.PreviousUrgency,
       ISNULL(n.RejectionNote,'')     AS RejectionNote
FROM NeedsPosts n
LEFT JOIN Organization o ON o.Organization_ID = n.Org_ID
WHERE n.Admin_Approval_Status = 'Pending'
ORDER BY n.Post_Date DESC", conn);
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
                        ImagePath = r["ImagePath"].ToString() ?? "",
                        Admin_Approval_Status = r["Admin_Approval_Status"].ToString() ?? "Pending",
                        PreviousTitle = r["PreviousTitle"] == DBNull.Value ? null : r["PreviousTitle"].ToString(),
                        PreviousDescription = r["PreviousDescription"] == DBNull.Value ? null : r["PreviousDescription"].ToString(),
                        PreviousUrgency = r["PreviousUrgency"] == DBNull.Value ? null : r["PreviousUrgency"].ToString(),
                        RejectionNote = r["RejectionNote"].ToString() ?? "",
                    });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("GetPendingNeedsPosts failed: " + ex.Message); }
            return list;
        }
        public static async Task ApproveNeedsPost(string postId, string urgency)
        {
            // Admin approves AND sets the final urgency at the same time
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
    UPDATE NeedsPosts
    SET Admin_Approval_Status = 'Approved',
        Urgency = @urgency
    WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@urgency", urgency);
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("ApproveNeedsPost failed: " + ex.Message); throw; }
        }

        public static async Task RejectNeedsPost(string postId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
    UPDATE NeedsPosts
    SET Admin_Approval_Status = 'Rejected'
    WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("RejectNeedsPost failed: " + ex.Message); throw; }
        }
        public static async Task ResetItemApproval(string itemId)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE Items SET Admin_Approval_Status = 'Pending' WHERE Item_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", itemId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { MessageBox.Show("ResetItemApproval failed: " + ex.Message); }
        }

        public static async Task SubmitNeedsPostEditForReview(NeedsPostModel post)
        {
            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync();
            // First read the current live values as "previous"
            using (var read = new SqlCommand(
                "SELECT Title, Description, Urgency FROM NeedsPosts WHERE NeedsPost_ID=@Id", conn))
            {
               
            }
            using var cmd = new SqlCommand(
                "UPDATE NeedsPosts SET Title=@T, Description=@D, Urgency=@U, ImagePath=@I, " +
                "Admin_Approval_Status='Pending', Status='Open', " +
                "PreviousTitle=@PT, PreviousDescription=@PD, PreviousUrgency=@PU " +
                "WHERE NeedsPost_ID=@Id", conn);
            cmd.Parameters.AddWithValue("@T", post.Title);
            cmd.Parameters.AddWithValue("@D", post.Description ?? "");
            cmd.Parameters.AddWithValue("@U", post.Urgency);
            cmd.Parameters.AddWithValue("@I", post.ImagePath ?? "");
          
            cmd.Parameters.AddWithValue("@Id", post.NeedsPost_ID);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task RejectNeedsPost(string postId, string rejectionNote)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
UPDATE NeedsPosts
SET Admin_Approval_Status = 'Rejected',
    RejectionNote         = @note
WHERE NeedsPost_ID = @id", conn);
                cmd.Parameters.AddWithValue("@note", rejectionNote.Trim());
                cmd.Parameters.AddWithValue("@id", postId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("RejectNeedsPost failed: " + ex.Message); throw; }
        }
    }

}