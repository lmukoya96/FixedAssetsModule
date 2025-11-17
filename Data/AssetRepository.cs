using Microsoft.Data.SqlClient;
using TestModule.Models;
using TestModule.Services;

namespace TestModule.Data
{
    public class AssetRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<Asset>> GetAllAsync()
        {
            var items = new List<Asset>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT Code, Description, AssetGroup, AssetCategory, Department, Location, PurchaseDate, DepreciationStartDate, TransactionDate, PurchaseAmount, Cost, TrackingCode, SerialNumber FROM Assets", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Asset
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    AssetGroup = rdr.GetString(2),
                    AssetCategory = rdr.GetString(3),
                    Department = rdr.GetString(4),
                    Location = rdr.GetString(5),
                    PurchaseDate = rdr.GetDateTime(6),
                    DepreciationStartDate = rdr.GetDateTime(7),
                    TransactionDate = rdr.IsDBNull(8) ? (DateTime?)null : rdr.GetDateTime(8),
                    PurchaseAmount = rdr.GetDecimal(9),
                    Cost = rdr.GetDecimal(10),
                    TrackingCode = rdr.IsDBNull(11) ? string.Empty : rdr.GetString(11),
                    SerialNumber = rdr.IsDBNull(12) ? string.Empty : rdr.GetString(12)
                });
            }
            return items;
        }

        public async Task<Asset?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT Code, Description, AssetGroup, AssetCategory, Department, Location, PurchaseDate, DepreciationStartDate, TransactionDate, PurchaseAmount, Cost, TrackingCode, SerialNumber 
            FROM Assets WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Asset
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    AssetGroup = rdr.GetString(2),
                    AssetCategory = rdr.GetString(3),
                    Department = rdr.GetString(4),
                    Location = rdr.GetString(5),
                    PurchaseDate = rdr.GetDateTime(6),
                    DepreciationStartDate = rdr.GetDateTime(7),
                    TransactionDate = rdr.IsDBNull(8) ? (DateTime?)null : rdr.GetDateTime(8),
                    PurchaseAmount = rdr.GetDecimal(9),
                    Cost = rdr.GetDecimal(10),
                    TrackingCode = rdr.IsDBNull(11) ? string.Empty : rdr.GetString(11),
                    SerialNumber = rdr.IsDBNull(12) ? string.Empty : rdr.GetString(12)
                };
            }
            return null;
        }

        public async Task<List<Asset>> GetByAssetCategoryAsync(string category)
        {
            var items = new List<Asset>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT Code, Description, AssetGroup, AssetCategory, Department, Location, PurchaseDate, DepreciationStartDate, TransactionDate, PurchaseAmount, Cost, TrackingCode, SerialNumber
            FROM Assets WHERE AssetCategory = @category", conn);

            cmd.Parameters.AddWithValue("@category", category);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Asset
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    AssetGroup = rdr.GetString(2),
                    AssetCategory = rdr.GetString(3),
                    Department = rdr.GetString(4),
                    Location = rdr.GetString(5),
                    PurchaseDate = rdr.GetDateTime(6),
                    DepreciationStartDate = rdr.GetDateTime(7),
                    TransactionDate = rdr.IsDBNull(8) ? (DateTime?)null : rdr.GetDateTime(8),
                    PurchaseAmount = rdr.GetDecimal(9),
                    Cost = rdr.GetDecimal(10),
                    TrackingCode = rdr.IsDBNull(11) ? string.Empty : rdr.GetString(11),
                    SerialNumber = rdr.IsDBNull(12) ? string.Empty : rdr.GetString(12)
                });
            }
            return items;
        }


        public async Task<int> CreateAsync(Asset model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate Code
            using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE Code = @code", conn))
            {
                checkCmd.Parameters.AddWithValue("@code", model.Code);
                if (await checkCmd.ExecuteScalarAsync() != null) return -1; // Duplicate code
            }

            // Check for duplicate TrackingCode (if provided)
            if (!string.IsNullOrEmpty(model.TrackingCode))
            {
                using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE TrackingCode = @trackingCode", conn))
                {
                    checkCmd.Parameters.AddWithValue("@trackingCode", model.TrackingCode);
                    if (await checkCmd.ExecuteScalarAsync() != null) return -2; // Duplicate TrackingCode
                }
            }

            // Check for duplicate SerialNumber (if provided)
            if (!string.IsNullOrEmpty(model.SerialNumber))
            {
                using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE SerialNumber = @serialNumber", conn))
                {
                    checkCmd.Parameters.AddWithValue("@serialNumber", model.SerialNumber);
                    if (await checkCmd.ExecuteScalarAsync() != null) return -3; // Duplicate SerialNumber
                }
            }

            // Insert Asset
            using (var cmd = new SqlCommand(@"
            INSERT INTO Assets 
            (Code, Description, AssetGroup, AssetCategory, Department, Location, PurchaseDate, DepreciationStartDate, TransactionDate, PurchaseAmount, Cost, TrackingCode, SerialNumber) 
            VALUES 
            (@code, @desc, @group, @cat, @dept, @loc, @pdate, @dstart, @tdate, @pamount, @cost, @track, @serial)", conn))
            {
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@desc", model.Description);
                cmd.Parameters.AddWithValue("@group", model.AssetGroup);
                cmd.Parameters.AddWithValue("@cat", model.AssetCategory);
                cmd.Parameters.AddWithValue("@dept", model.Department);
                cmd.Parameters.AddWithValue("@loc", model.Location);
                cmd.Parameters.AddWithValue("@pdate", model.PurchaseDate);
                cmd.Parameters.AddWithValue("@dstart", model.DepreciationStartDate);
                cmd.Parameters.AddWithValue("@tdate", (object?)model.TransactionDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pamount", model.PurchaseAmount);
                cmd.Parameters.AddWithValue("@cost", model.Cost);
                cmd.Parameters.AddWithValue("@track", (object?)model.TrackingCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@serial", (object?)model.SerialNumber ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            try
            {
                var depreciationService = new DepreciationService();
                await depreciationService.GenerateFullYearDepreciationAsync(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not generate depreciation records: {ex.Message}");
            }

            return 1; // success
        }

        public async Task<int> UpdateAsync(Asset model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Get the existing asset first to check if cost changed
            var existingAsset = await GetByCodeAsync(originalCode);
            decimal oldCost = existingAsset?.Cost ?? 0m;

            // Check for duplicate Code (excluding the current asset)
            using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE Code = @code AND Code != @originalCode", conn))
            {
                checkCmd.Parameters.AddWithValue("@code", model.Code);
                checkCmd.Parameters.AddWithValue("@originalCode", originalCode);
                if (await checkCmd.ExecuteScalarAsync() != null) return -1; // Duplicate code
            }

            // Check for duplicate TrackingCode (if provided, excluding the current asset)
            if (!string.IsNullOrEmpty(model.TrackingCode))
            {
                // This query checks for a duplicate TrackingCode that isn't owned by the current asset (originalCode)
                using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE TrackingCode = @trackingCode AND Code != @originalCode", conn))
                {
                    checkCmd.Parameters.AddWithValue("@trackingCode", model.TrackingCode);
                    checkCmd.Parameters.AddWithValue("@originalCode", originalCode);
                    if (await checkCmd.ExecuteScalarAsync() != null) return -2; // Duplicate TrackingCode
                }
            }

            // Check for duplicate SerialNumber (if provided, excluding the current asset)
            if (!string.IsNullOrEmpty(model.SerialNumber))
            {
                // This query checks for a duplicate SerialNumber that isn't owned by the current asset (originalCode)
                using (var checkCmd = new SqlCommand("SELECT 1 FROM Assets WHERE SerialNumber = @serialNumber AND Code != @originalCode", conn))
                {
                    checkCmd.Parameters.AddWithValue("@serialNumber", model.SerialNumber);
                    checkCmd.Parameters.AddWithValue("@originalCode", originalCode);
                    if (await checkCmd.ExecuteScalarAsync() != null) return -3; // Duplicate SerialNumber
                }
            }

            using var cmd = new SqlCommand(@"
            UPDATE Assets 
            SET Code = @code, 
                Description = @desc, 
                AssetGroup = @group, 
                AssetCategory = @cat,
                Department = @dept,
                Location = @loc, 
                PurchaseDate = @pdate, 
                DepreciationStartDate = @dstart, 
                TransactionDate = @tdate, 
                PurchaseAmount = @pamount, 
                Cost = @cost, 
                TrackingCode = @track, 
                SerialNumber = @serial 
            WHERE Code = @originalCode", conn);

            cmd.Parameters.AddWithValue("@code", model.Code);
            cmd.Parameters.AddWithValue("@desc", model.Description);
            cmd.Parameters.AddWithValue("@group", model.AssetGroup);
            cmd.Parameters.AddWithValue("@cat", model.AssetCategory);
            cmd.Parameters.AddWithValue("@dept", model.Department);
            cmd.Parameters.AddWithValue("@loc", model.Location);
            cmd.Parameters.AddWithValue("@pdate", model.PurchaseDate);
            cmd.Parameters.AddWithValue("@dstart", model.DepreciationStartDate);
            cmd.Parameters.AddWithValue("@tdate", (object?)model.TransactionDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pamount", model.PurchaseAmount);
            cmd.Parameters.AddWithValue("@cost", model.Cost);
            cmd.Parameters.AddWithValue("@track", (object?)model.TrackingCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@serial", (object?)model.SerialNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@originalCode", originalCode);

            
            int result = await cmd.ExecuteNonQueryAsync();

            return result;
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("DELETE FROM Assets WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}