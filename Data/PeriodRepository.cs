using Microsoft.Data.SqlClient;
using TestModule.Models;

namespace TestModule.Data
{
    public class PeriodRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<Period>> GetAllAsync()
        {
            var items = new List<Period>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Period, Month, Year, StartDate, EndDate, IsCurrent FROM Periods ORDER BY StartDate", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Period
                {
                    PeriodNum = rdr.GetByte(0),
                    Month = rdr.GetByte(1),
                    Year = rdr.GetInt16(2),
                    StartDate = rdr.GetDateTime(3),
                    EndDate = rdr.GetDateTime(4),
                    IsCurrent = rdr.GetBoolean(5)
                });
            }
            return items;
        }

        public async Task<Period?> GetByPeriodAndYearAsync(byte period, short year)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Period, Month, Year, StartDate, EndDate, IsCurrent FROM Periods WHERE Period = @period AND Year = @year", conn);
            cmd.Parameters.AddWithValue("@period", period);
            cmd.Parameters.AddWithValue("@year", year);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Period
                {
                    PeriodNum = rdr.GetByte(0),
                    Month = rdr.GetByte(1),
                    Year = rdr.GetInt16(2),
                    StartDate = rdr.GetDateTime(3),
                    EndDate = rdr.GetDateTime(4),
                    IsCurrent = rdr.GetBoolean(5)
                };
            }
            return null;
        }

        public async Task<List<Period>> GetPeriodsByYearAsync(short year)
        {
            var periods = new List<Period>();

            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Period, Month, Year, StartDate, EndDate, IsCurrent FROM Periods WHERE Year = @year ORDER BY Period",
                conn);

            cmd.Parameters.AddWithValue("@year", year);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                periods.Add(new Period
                {
                    PeriodNum = rdr.GetByte(0),
                    Month = rdr.GetByte(1),
                    Year = rdr.GetInt16(2),
                    StartDate = rdr.GetDateTime(3),
                    EndDate = rdr.GetDateTime(4),
                    IsCurrent = rdr.GetBoolean(5)
                });
            }

            return periods;
        }

        public async Task<int> CreateAsync(Period model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate Period + Year
            using (var check = new SqlCommand(
                "SELECT 1 FROM Periods WHERE Period = @period AND Year = @year", conn))
            {
                check.Parameters.AddWithValue("@period", model.PeriodNum);
                check.Parameters.AddWithValue("@year", model.Year);

                var exists = await check.ExecuteScalarAsync();
                if (exists != null) return -1; // duplicate period/year
            }

            using var cmd = new SqlCommand(
                "INSERT INTO Periods (Period, Month, Year, StartDate, EndDate, IsCurrent) " +
                "VALUES (@period, @month, @year, @start, @end, @isCurrent)", conn);

            cmd.Parameters.AddWithValue("@period", model.PeriodNum);
            cmd.Parameters.AddWithValue("@month", model.Month);
            cmd.Parameters.AddWithValue("@year", model.Year);
            cmd.Parameters.AddWithValue("@start", model.StartDate);
            cmd.Parameters.AddWithValue("@end", model.EndDate);
            cmd.Parameters.AddWithValue("@isCurrent", model.IsCurrent);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateAsync(Period model, byte originalPeriod, short originalYear)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // 1. Check for duplicate Period + Year, excluding this record
            using (var check = new SqlCommand(
                "SELECT 1 FROM Periods WHERE Period = @period AND Year = @year AND NOT (Period = @originalPeriod AND Year = @originalYear)", conn))
            {
                check.Parameters.AddWithValue("@period", model.PeriodNum);
                check.Parameters.AddWithValue("@year", model.Year);
                check.Parameters.AddWithValue("@originalPeriod", originalPeriod);
                check.Parameters.AddWithValue("@originalYear", originalYear);

                var exists = await check.ExecuteScalarAsync();
                if (exists != null) return -1; // duplicate period/year
            }


            // 3. Ensure only one period is current.
            if (model.IsCurrent)
            {
                using var reset = new SqlCommand(
                    "UPDATE Periods SET IsCurrent = 0 WHERE IsCurrent = 1", conn);
                await reset.ExecuteNonQueryAsync();
            }

            //4. Update the target period
            using var cmd = new SqlCommand(
                "UPDATE Periods SET Period = @period, Month = @month, Year = @year, " +
                "StartDate = @start, EndDate = @end, IsCurrent = @isCurrent " +
                "WHERE Period = @originalPeriod AND Year = @originalYear", conn);

            cmd.Parameters.AddWithValue("@period", model.PeriodNum);
            cmd.Parameters.AddWithValue("@month", model.Month);
            cmd.Parameters.AddWithValue("@year", model.Year);
            cmd.Parameters.AddWithValue("@start", model.StartDate);
            cmd.Parameters.AddWithValue("@end", model.EndDate);
            cmd.Parameters.AddWithValue("@isCurrent", model.IsCurrent);
            cmd.Parameters.AddWithValue("@originalPeriod", originalPeriod);
            cmd.Parameters.AddWithValue("@originalYear", originalYear);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Period?> GetCurrentPeriodAsync()
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT Period, Month, Year, StartDate, EndDate, IsCurrent FROM Periods WHERE IsCurrent = 1", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Period
                {
                    PeriodNum = rdr.GetByte(0),
                    Month = rdr.GetByte(1),
                    Year = rdr.GetInt16(2),
                    StartDate = rdr.GetDateTime(3),
                    EndDate = rdr.GetDateTime(4),
                    IsCurrent = rdr.GetBoolean(5)
                };
            }
            return null;
        }

        public async Task<int> DeleteAsync(byte period, short year)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM Periods WHERE Period = @period and Year = @year", conn);
            cmd.Parameters.AddWithValue("@period", period);
            cmd.Parameters.AddWithValue("@year", year);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}