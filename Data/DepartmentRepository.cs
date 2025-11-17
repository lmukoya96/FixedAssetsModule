using Microsoft.Data.SqlClient;
using TestModule.Models;

namespace TestModule.Data
{
    public class DepartmentRepository
    {
        private readonly Database _db = Database.DB_Connection();

        public async Task<List<Department>> GetAllAsync()
        {
            var items = new List<Department>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT DepartmentCode, DepartmentName, CreatedAt FROM Departments ORDER BY DepartmentName", conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new Department
                {
                    DepartmentCode = rdr.GetString(0),
                    DepartmentName = rdr.GetString(1),
                    CreatedAt = rdr.GetDateTime(2)
                });
            }
            return items;
        }

        public async Task<Department?> GetByCodeAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT DepartmentCode, DepartmentName, CreatedAt FROM Departments WHERE DepartmentCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new Department
                {
                    DepartmentCode = rdr.GetString(0),
                    DepartmentName = rdr.GetString(1),
                    CreatedAt = rdr.GetDateTime(2)
                };
            }
            return null;
        }

        public async Task<int> CreateAsync(Department model)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check for duplicate DepartmentName
            using (var check = new SqlCommand(
                "SELECT 1 FROM Departments WHERE DepartmentName = @name", conn))
            {
                check.Parameters.AddWithValue("@name", model.DepartmentName);
                var exists = await check.ExecuteScalarAsync();
                if (exists != null) return -2; // duplicate department name
            }

            // Check for duplicate DepartmentCode
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Departments WHERE DepartmentCode = @code", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.DepartmentCode);
                var exists = await checkCode.ExecuteScalarAsync();
                if (exists != null) return -1; // duplicate department code
            }


            using var cmd = new SqlCommand(
                "INSERT INTO Departments (DepartmentCode, DepartmentName) " +
                "VALUES (@code, @name)", conn);

            cmd.Parameters.AddWithValue("@code", model.DepartmentCode);
            cmd.Parameters.AddWithValue("@name", model.DepartmentName);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateAsync(Department model, string originalCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using (var check = new SqlCommand(
                "SELECT 1 FROM Departments WHERE DepartmentName = @name AND DepartmentCode <> @originalCode", conn))
            {
                check.Parameters.AddWithValue("@name", model.DepartmentName);
                check.Parameters.AddWithValue("@originalCode", originalCode);
                var exists = await check.ExecuteScalarAsync();
                if (exists != null) return -2; // duplicate name
            }

            // Check for duplicate DepartmentCode excluding current
            using (var checkCode = new SqlCommand(
                "SELECT 1 FROM Departments WHERE DepartmentCode = @code AND DepartmentCode <> @originalCode", conn))
            {
                checkCode.Parameters.AddWithValue("@code", model.DepartmentCode);
                checkCode.Parameters.AddWithValue("@originalCode", originalCode);
                var exists = await checkCode.ExecuteScalarAsync();
                if (exists != null) return -1; // duplicate code
            }

            using var cmd = new SqlCommand(
                "UPDATE Departments SET DepartmentCode = @code, DepartmentName = @name " +
                "WHERE DepartmentCode = @originalCode", conn);

            cmd.Parameters.AddWithValue("@code", model.DepartmentCode);
            cmd.Parameters.AddWithValue("@name", model.DepartmentName);
            cmd.Parameters.AddWithValue("@originalCode", originalCode);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> DeleteAsync(string code)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(
                "DELETE FROM Departments WHERE DepartmentCode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}