using Microsoft.Data.SqlClient;
using TestModule.Models;
using TestModule.Services;

namespace TestModule.Data
{
    public class DepreciationRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<Depreciation>> GetAllAsync()
        {
            var items = new List<Depreciation>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT Code, Description, DepreciationRate, DepreciationMethod, TaxRate, TaxMethod FROM Depreciation", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Depreciation
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    DepreciationRate = rdr.GetDecimal(2),
                    DepreciationMethod = rdr.GetString(3),
                    TaxRate = rdr.GetDecimal(4),
                    TaxMethod = rdr.GetString(5)
                });
            }
            return items;
        }

        public async Task<Depreciation?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Code, Description, DepreciationRate, DepreciationMethod, TaxRate, TaxMethod FROM Depreciation WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Depreciation
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    DepreciationRate = rdr.GetDecimal(2),
                    DepreciationMethod = rdr.GetString(3),
                    TaxRate = rdr.GetDecimal(4),
                    TaxMethod = rdr.GetString(5)
                };
            }
            return null;
        }

        public async Task<int> CreateAsync(Depreciation model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate code
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Depreciation WHERE Code = @code", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.Code);
                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            // Check for duplicate description
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM Depreciation WHERE Description = @desc", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.Description);
                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "INSERT INTO Depreciation(Code, Description, DepreciationRate, DepreciationMethod, TaxRate, TaxMethod) VALUES (@code, @desc, @desc_rate, @desc_method, @tax_rate, @tax_method)", conn))
            {
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@desc", model.Description);
                cmd.Parameters.AddWithValue("@desc_rate", model.DepreciationRate);
                cmd.Parameters.AddWithValue("@desc_method", model.DepreciationMethod);
                cmd.Parameters.AddWithValue("@tax_rate", model.TaxRate);
                cmd.Parameters.AddWithValue("@tax_method", model.TaxMethod);

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> UpdateAsync(Depreciation model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Get the old depreciation record first
            var existingDepreciation = await GetByCodeAsync(originalCode);
            decimal oldRate = existingDepreciation?.DepreciationRate ?? 0m;

            //Check for duplicate code excluding current record
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Depreciation WHERE Code = @newCode AND Code <> @originalCode", conn))
            {
                checkCode.Parameters.AddWithValue("@newCode", model.Code);
                checkCode.Parameters.AddWithValue("@originalCode", originalCode);

                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            //Check for duplicate description excluding current record
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM Depreciation WHERE Description = @desc AND Code <> @originalCode", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.Description);
                checkDesc.Parameters.AddWithValue("@originalCode", originalCode);

                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "UPDATE Depreciation SET Code = @newCode, Description = @desc, DepreciationRate = @desc_rate, DepreciationMethod = @desc_method, TaxRate =@tax_rate, TaxMethod = @tax_method WHERE Code = @originalCode", conn))
            {
                cmd.Parameters.AddWithValue("@newCode", model.Code);
                cmd.Parameters.AddWithValue("@desc", model.Description);
                cmd.Parameters.AddWithValue("@desc_rate", model.DepreciationRate);
                cmd.Parameters.AddWithValue("@desc_method", model.DepreciationMethod);
                cmd.Parameters.AddWithValue("@tax_rate", model.TaxRate);
                cmd.Parameters.AddWithValue("@tax_method", model.TaxMethod);
                cmd.Parameters.AddWithValue("@originalCode", originalCode);

                int result = await cmd.ExecuteNonQueryAsync();

                // If update was successful AND rate changed, recalculate all affected assets
                if (result > 0 && existingDepreciation != null && existingDepreciation.DepreciationRate != model.DepreciationRate)
                {
                    try
                    {
                        var depreciationService = new DepreciationService();
                        await depreciationService.RecalculateAllAssetsDepreciationAsync(model.Code);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the update
                        Console.WriteLine($"Warning: Could not update asset depreciation records: {ex.Message}");
                    }
                }

                return result;
            }
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM Depreciation WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}