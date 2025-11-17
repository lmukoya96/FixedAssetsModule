using Microsoft.Data.SqlClient;
using TestModule.Models;

namespace TestModule.Data
{
    public class AssetGroupRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<AssetGroup>> GetAllAsync()
        {
            var items = new List<AssetGroup>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT GroupCode, GroupDescription, Cost, AccDeprn, Depreciation, Revaluation, Disposal, Scrapping, DepreciationCode FROM AssetGroups", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new AssetGroup
                {
                    GroupCode = rdr.GetString(0),
                    GroupDescription = rdr.GetString(1),
                    Cost = rdr.GetString(2),
                    AccDeprn = rdr.GetString(3),
                    Depreciation = rdr.GetString(4),
                    Revaluation = rdr.GetString(5),
                    Disposal = rdr.GetString(6),
                    Scrapping = rdr.GetString(7),
                    DepreciationCode = rdr.GetString(8)
                });
            }
            return items;
        }

        public async Task<AssetGroup?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT GroupCode, GroupDescription, Cost, AccDeprn, Depreciation, Revaluation, Disposal, Scrapping, DepreciationCode FROM AssetGroups WHERE GroupCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new AssetGroup
                {
                    GroupCode = rdr.GetString(0),
                    GroupDescription = rdr.GetString(1),
                    Cost = rdr.GetString(2),
                    AccDeprn = rdr.GetString(3),
                    Depreciation = rdr.GetString(4),
                    Revaluation = rdr.GetString(5),
                    Disposal = rdr.GetString(6),
                    Scrapping = rdr.GetString(7),
                    DepreciationCode = rdr.GetString(8)
                };
            }
            return null;
        }

        public async Task<int> CreateAsync(AssetGroup model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate code
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM AssetGroups WHERE GroupCode = @code", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.GroupCode);
                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            // Check for duplicate description
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM AssetGroups WHERE GroupDescription = @desc", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.GroupDescription);
                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "INSERT INTO AssetGroups(GroupCode, GroupDescription, Cost, AccDeprn, Depreciation, Revaluation, Disposal, Scrapping, DepreciationCode) VALUES (@code, @desc, @cost, @acc_deprn, @depreciation, @revaluation, @disposal, @scrapping, @depr_code)", conn))
            {
                cmd.Parameters.AddWithValue("@code", model.GroupCode);
                cmd.Parameters.AddWithValue("@desc", model.GroupDescription);
                cmd.Parameters.AddWithValue("@cost", model.Cost);
                cmd.Parameters.AddWithValue("@acc_deprn", model.AccDeprn);
                cmd.Parameters.AddWithValue("@depreciation", model.Depreciation);
                cmd.Parameters.AddWithValue("@revaluation", model.Revaluation);
                cmd.Parameters.AddWithValue("@disposal", model.Disposal);
                cmd.Parameters.AddWithValue("@scrapping", model.Scrapping);
                cmd.Parameters.AddWithValue("@depr_code", model.DepreciationCode);

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> UpdateAsync(AssetGroup model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            //Check for duplicate code excluding current record
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM AssetGroups WHERE GroupCode = @newCode AND GroupCode <> @originalCode", conn))
            {
                checkCode.Parameters.AddWithValue("@newCode", model.GroupCode);
                checkCode.Parameters.AddWithValue("@originalCode", originalCode);

                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            //Check for duplicate description excluding current record
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM AssetGroups WHERE GroupDescription = @desc AND GroupCode <> @originalCode", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.GroupDescription);
                checkDesc.Parameters.AddWithValue("@originalCode", originalCode);

                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "UPDATE AssetGroups SET GroupCode = @newCode, GroupDescription = @desc, Cost = @cost, AccDeprn = @acc_deprn, Depreciation = @depreciation, Revaluation = @revaluation, Disposal = @disposal, Scrapping = @scrapping, DepreciationCode = @depr_code  WHERE GroupCode = @originalCode", conn))
            {
                cmd.Parameters.AddWithValue("@newCode", model.GroupCode);
                cmd.Parameters.AddWithValue("@desc", model.GroupDescription);
                cmd.Parameters.AddWithValue("@cost", model.Cost);
                cmd.Parameters.AddWithValue("@acc_deprn", model.AccDeprn);
                cmd.Parameters.AddWithValue("@depreciation", model.Depreciation);
                cmd.Parameters.AddWithValue("@revaluation", model.Revaluation);
                cmd.Parameters.AddWithValue("@disposal", model.Disposal);
                cmd.Parameters.AddWithValue("@scrapping", model.Scrapping);
                cmd.Parameters.AddWithValue("@depr_code", model.DepreciationCode);
                cmd.Parameters.AddWithValue("@originalCode", originalCode);

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM AssetGroups WHERE GroupCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}