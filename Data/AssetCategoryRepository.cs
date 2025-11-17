using Microsoft.Data.SqlClient;
using TestModule.Models;

namespace TestModule.Data
{
    public class AssetCategoryRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<AssetCategory>> GetAllAsync()
        {
            var items = new List<AssetCategory>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT CategoryCode, CategoryDescription FROM AssetCategories;", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new AssetCategory
                {
                    CategoryCode = rdr.GetString(0),
                    CategoryDescription = rdr.GetString(1)
                });
            }
            return items;
        }

        public async Task<AssetCategory?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT CategoryCode, CategoryDescription FROM AssetCategories WHERE CategoryCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new AssetCategory
                {
                    CategoryCode = rdr.GetString(0),
                    CategoryDescription = rdr.GetString(1)
                };
            }
            return null;
        }

        public async Task<int> CreateAsync(AssetCategory model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate code
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM AssetCategories WHERE CategoryCode = @code", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.CategoryCode);
                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            // Check for duplicate description
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM AssetCategories WHERE CategoryDescription = @desc", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.CategoryDescription);
                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "INSERT INTO AssetCategories (CategoryCode, CategoryDescription) VALUES (@code, @desc)", conn))
            {
                cmd.Parameters.AddWithValue("@code", model.CategoryCode);
                cmd.Parameters.AddWithValue("@desc", model.CategoryDescription);

                return await cmd.ExecuteNonQueryAsync(); // success → >0
            }
        }


        public async Task<int> UpdateAsync(AssetCategory model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            //Check for duplicate code excluding current record
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM AssetCategories WHERE CategoryCode = @newCode AND CategoryCode <> @originalCode", conn))
            {
                checkCode.Parameters.AddWithValue("@newCode", model.CategoryCode);
                checkCode.Parameters.AddWithValue("@originalCode", originalCode);

                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            //Check for duplicate description excluding current record
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM AssetCategories WHERE CategoryDescription = @desc AND CategoryCode <> @originalCode", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.CategoryDescription);
                checkDesc.Parameters.AddWithValue("@originalCode", originalCode);

                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Update if unique.
            using (var cmd = new SqlCommand(
                "UPDATE AssetCategories SET CategoryCode = @newCode, CategoryDescription = @desc WHERE CategoryCode = @originalCode", conn))
            {
                cmd.Parameters.AddWithValue("@newCode", model.CategoryCode);
                cmd.Parameters.AddWithValue("@desc", model.CategoryDescription);
                cmd.Parameters.AddWithValue("@originalCode", originalCode);

                return await cmd.ExecuteNonQueryAsync(); // success → >0
            }
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM AssetCategories WHERE CategoryCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}