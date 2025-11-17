using Microsoft.Data.SqlClient;
using TestModule.Models;
using TestModule.Services;

namespace TestModule.Data
{
    public class TransactionRepository
    {
        private readonly Database _db = Database.DB_Connection();
        private readonly DepreciationService _depService = new DepreciationService();
        public async Task<List<Transaction>> GetAllAsync()
        {
            var items = new List<Transaction>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"SELECT * FROM Transactions", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Transaction
                {
                    TransactionID = rdr.GetInt32(0),
                    AssetCode = rdr.GetString(1),
                    Period = rdr.GetString(2),
                    TransactionType = rdr.GetString(3),
                    TransactionDate = rdr.GetDateTime(4),
                    Amount = rdr.GetDecimal(5),
                    Cost = rdr.GetDecimal(6)
                });
            }
            return items;
        }

        public async Task<List<(string Description, string Period, string TransactionType, DateTime TransactionDate, decimal Amount, decimal Cost)>> GetAllWithAssetsAsync()
        {
            var items = new List<(string Description, string Period, string TransactionType, DateTime TransactionDate, decimal Amount, decimal Cost)>();

            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT a.Description, t.Period, t.TransactionType, t.TransactionDate, t.Amount, t.Cost
            FROM Transactions t
            JOIN Assets a ON t.AssetCode = a.Code;", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add((
                    rdr.GetString(0),   // Description
                    _depService.FormatPeriodDisplay(rdr.GetString(1)),   // Period
                    rdr.GetString(2),   // TransactionType
                    rdr.GetDateTime(3), // TransactionDate
                    rdr.GetDecimal(4),  // Amount
                    rdr.GetDecimal(5)   // Cost
                ));
            }

            return items;
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT TransactionID, AssetCode, Period, TransactionType, TransactionDate, Amount, Cost
            FROM Transactions WHERE TransactionID = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Transaction
                {
                    TransactionID = rdr.GetInt32(0),
                    AssetCode = rdr.GetString(1),
                    Period = rdr.GetString(2),
                    TransactionType = rdr.GetString(3),
                    TransactionDate = rdr.GetDateTime(4),
                    Amount = rdr.GetDecimal(5),
                    Cost = rdr.GetDecimal(6)
                };
            }
            return null;
        }

        public async Task<List<Transaction>> GetByCodeAsync(string code)
        {
            var items = new List<Transaction>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT TransactionID, Period, TransactionType, TransactionDate, Amount, Cost FROM Transactions WHERE AssetCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Transaction
                {
                    TransactionID = rdr.GetInt32(0),
                    Period = _depService.FormatPeriodDisplay(rdr.GetString(1)),
                    TransactionType = rdr.GetString(2),
                    TransactionDate = rdr.GetDateTime(3),
                    Amount = rdr.GetDecimal(4),
                    Cost = rdr.GetDecimal(5)
                });
            }
            return items;
        }

        public async Task<int> CreateAsync(Transaction model, string Period)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Insert transaction with current period.
            using var cmd = new SqlCommand(@"
            INSERT INTO Transactions (AssetCode, Period, TransactionType, TransactionDate, Amount, Cost) 
            VALUES (@assetCode, @period, @type, @date, @amount, @cost)", conn);

            cmd.Parameters.AddWithValue("@assetCode", model.AssetCode);
            cmd.Parameters.AddWithValue("@period", Period); 
            cmd.Parameters.AddWithValue("@type", model.TransactionType);
            cmd.Parameters.AddWithValue("@date", model.TransactionDate);
            cmd.Parameters.AddWithValue("@amount", model.Amount);
            cmd.Parameters.AddWithValue("@cost", model.Cost);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateAsync(Transaction model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
            UPDATE Transactions 
            SET AssetCode = @assetCode, 
                Period = @period,
                TransactionType = @type, 
                TransactionDate = @date, 
                Amount = @amount, 
                Cost = @cost
            WHERE TransactionID = @id", conn);

            cmd.Parameters.AddWithValue("@assetCode", model.AssetCode);
            cmd.Parameters.AddWithValue("@period", model.Period);
            cmd.Parameters.AddWithValue("@type", model.TransactionType);
            cmd.Parameters.AddWithValue("@date", model.TransactionDate);
            cmd.Parameters.AddWithValue("@amount", model.Amount);
            cmd.Parameters.AddWithValue("@cost", model.Cost);
            cmd.Parameters.AddWithValue("@id", model.TransactionID);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> DeleteAsync(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("DELETE FROM Transactions WHERE TransactionID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}