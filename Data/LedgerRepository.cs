using Microsoft.Data.SqlClient;
using TestModule.Models;

namespace TestModule.Data
{
    public class LedgerRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<Ledger>> GetAllAsync()
        {
            var items = new List<Ledger>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT Code, Description, Account, SubAccount FROM Ledger_", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Ledger
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    Account = rdr.GetString(2),
                    SubAccount = rdr.GetString(3)
                });
            }
            return items;
        }

        public async Task<Ledger?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Code, Description, Account, SubAccount FROM Ledger_ WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Ledger
                {
                    Code = rdr.GetString(0),
                    Description = rdr.GetString(1),
                    Account = rdr.GetString(2),
                    SubAccount = rdr.GetString(3)
                };
            }
            return null;
        }

        public async Task<int> CreateAsync(Ledger model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate code
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Code = @code", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.Code);
                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            // Check for duplicate description
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Description = @desc", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.Description);
                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Check for duplicate (Account, SubAccount) pair
            using (var checkAcc = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Account = @account AND SubAccount = @subAccount", conn))
            {
                checkAcc.Parameters.AddWithValue("@account", model.Account);
                checkAcc.Parameters.AddWithValue("@subAccount", model.SubAccount);
                var existsAcc = await checkAcc.ExecuteScalarAsync();
                if (existsAcc != null) return -3; // duplicate account+subaccount
            }

            // Insert if unique
            using (var cmd = new SqlCommand(
                "INSERT INTO Ledger_ (Code, Description, Account, SubAccount) VALUES (@code, @desc, @account, @subAccount)", conn))
            {
                cmd.Parameters.AddWithValue("@code", model.Code);
                cmd.Parameters.AddWithValue("@desc", model.Description);
                cmd.Parameters.AddWithValue("@account", model.Account);
                cmd.Parameters.AddWithValue("@subAccount", model.SubAccount);

                return await cmd.ExecuteNonQueryAsync(); // success → >0
            }
        }


        public async Task<int> UpdateAsync(Ledger model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            //Check for duplicate code excluding current record
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Code = @newCode AND Code <> @originalCode", conn))
            {
                checkCode.Parameters.AddWithValue("@newCode", model.Code);
                checkCode.Parameters.AddWithValue("@originalCode", originalCode);

                var existsCode = await checkCode.ExecuteScalarAsync();
                if (existsCode != null) return -1; // duplicate code
            }

            //Check for duplicate description excluding current record
            using (var checkDesc = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Description = @desc AND Code <> @originalCode", conn))
            {
                checkDesc.Parameters.AddWithValue("@desc", model.Description);
                checkDesc.Parameters.AddWithValue("@originalCode", originalCode);

                var existsDesc = await checkDesc.ExecuteScalarAsync();
                if (existsDesc != null) return -2; // duplicate description
            }

            // Check for duplicate (Account, SubAccount) pair excluding current record
            using (var checkAcc = new SqlCommand(
                "SELECT 1 FROM Ledger_ WHERE Account = @account AND SubAccount = @subAccount AND Code <> @originalCode", conn))
            {
                checkAcc.Parameters.AddWithValue("@account", model.Account);
                checkAcc.Parameters.AddWithValue("@subAccount", model.SubAccount);
                checkAcc.Parameters.AddWithValue("@originalCode", originalCode);

                var existsAcc = await checkAcc.ExecuteScalarAsync();
                if (existsAcc != null) return -3; // duplicate account+subaccount
            }

            // Update with original code
            using (var cmd = new SqlCommand(
                "UPDATE Ledger_ SET Code = @newCode, Description = @desc, Account = @account, SubAccount = @subAccount WHERE Code = @originalCode", conn))
            {
                cmd.Parameters.AddWithValue("@newCode", model.Code);
                cmd.Parameters.AddWithValue("@desc", model.Description);
                cmd.Parameters.AddWithValue("@account", model.Account);
                cmd.Parameters.AddWithValue("@subAccount", model.SubAccount);
                cmd.Parameters.AddWithValue("@originalCode", originalCode);

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM Ledger_ WHERE Code = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}