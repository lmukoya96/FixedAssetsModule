using Microsoft.Data.SqlClient;
using TestModule.Models;
using TestModule.Data;

namespace TestModule.Services
{
    public class DepreciationService
    {
        private readonly Database _db = Database.DB_Connection();
        private readonly AssetRepository _assetRepo = new AssetRepository();
        private readonly PeriodRepository _periodRepo = new PeriodRepository();

        public async Task GenerateFullYearDepreciationAsync(Asset asset)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // 1. Get the CURRENT PERIOD when the asset is being added
            var currentPeriod = await _periodRepo.GetCurrentPeriodAsync();
            if (currentPeriod == null) throw new Exception("No current period found.");

            // 2. Get ALL periods from the current period onward (not just current year)
            var allPeriods = await GetAllPeriodsFromAsync(currentPeriod);
            if (allPeriods == null || !allPeriods.Any())
                throw new Exception($"No periods found from period {currentPeriod.PeriodNum}-{currentPeriod.Year}");

            // 3. Create INITIAL cost records from CURRENT PERIOD onward (using theoretical calculation)
            await CreateInitialCostRecordsForAssetAsync(conn, asset, currentPeriod, allPeriods);

            // 4. Calculate depreciation from DEPRECIATION START DATE (or current period, whichever is later)
            await CalculateFullYearDepreciationAsync(conn, asset, currentPeriod, allPeriods);

            // 5. UPDATE cost records for future years based on ACTUAL depreciation
            await UpdateFutureCostRecordsBasedOnDepreciationAsync(conn, asset, currentPeriod, allPeriods);
        }

        private async Task CreateInitialCostRecordsForAssetAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> allPeriods)
        {
            // For Reducing Balance method, we need to calculate cost for each year
            string depreciationMethod = await GetDepreciationMethodForAssetAsync(conn, asset.AssetGroup);

            if (depreciationMethod == "Reducing Balance")
            {
                await CreateInitialCostRecordsForReducingBalanceAsync(conn, asset, startPeriod, allPeriods);
            }
            else
            {
                await CreateCostRecordsForEqualInstallmentsAsync(conn, asset, startPeriod, allPeriods);
            }
        }

        private async Task CreateInitialCostRecordsForReducingBalanceAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> allPeriods)
        {
            decimal depreciationRate = await GetDepreciationRateForAssetAsync(conn, asset.AssetGroup);
            decimal currentCost = asset.Cost;

            // Group periods by year
            var periodsByYear = allPeriods
                .Where(p => p.PeriodNum >= startPeriod.PeriodNum || p.Year > startPeriod.Year)
                .GroupBy(p => p.Year)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var yearGroup in periodsByYear)
            {
                short year = yearGroup.Key;
                var yearPeriods = yearGroup.OrderBy(p => p.PeriodNum).ToList();

                // For the start year, if we're starting mid-year, use the original cost
                // For subsequent years, calculate the reduced cost
                decimal yearCost = (year == startPeriod.Year) ? asset.Cost : currentCost;

                foreach (var period in yearPeriods)
                {
                    string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                    string insertQuery = @"
                    INSERT INTO AssetCost (AssetCode, Period, Cost)
                    VALUES (@assetCode, @period, @cost)";

                    using var insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                    insertCmd.Parameters.AddWithValue("@period", periodString);
                    insertCmd.Parameters.AddWithValue("@cost", yearCost);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Calculate annual depreciation for next year (theoretical)
                if (year != periodsByYear.Last().Key)
                {
                    decimal annualDepreciation = yearCost * (depreciationRate / 100m);
                    currentCost = yearCost - annualDepreciation;
                }
            }
        }

        private async Task UpdateFutureCostRecordsBasedOnDepreciationAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> allPeriods)
        {
            // Get the depreciation method for this asset
            string depreciationMethod = await GetDepreciationMethodForAssetAsync(conn, asset.AssetGroup);

            // Only run this logic for Reducing Balance
            if (depreciationMethod != "Reducing Balance")
                return;

            // Group periods by year, excluding the start year
            var futureYears = allPeriods
                .Where(p => p.Year > startPeriod.Year)
                .Select(p => p.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            foreach (var year in futureYears)
            {
                // Get the actual book value from the end of the previous year
                decimal bookValueAtEndOfPreviousYear = await GetBookValueAtEndOfYearAsync(conn, asset.Code, (short)(year - 1));

                // Update all cost records for this year with the actual book value
                await UpdateCostForYearAsync(conn, asset.Code, year, bookValueAtEndOfPreviousYear);
            }
        }

        private async Task UpdateCostForYearAsync(SqlConnection conn, string assetCode, short year, decimal newCost)
        {
            // Get all periods for the year
            var yearPeriods = await _periodRepo.GetPeriodsByYearAsync(year);

            foreach (var period in yearPeriods)
            {
                string periodString = $"{period.PeriodNum:D2}-{year}";

                string updateQuery = @"
                UPDATE AssetCost 
                SET Cost = @cost 
                WHERE AssetCode = @assetCode AND Period = @period";

                using var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@assetCode", assetCode);
                updateCmd.Parameters.AddWithValue("@period", periodString);
                updateCmd.Parameters.AddWithValue("@cost", newCost);

                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task CreateCostRecordsForEqualInstallmentsAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> allPeriods)
        {
            // For Equal Installments, cost remains the same for all periods
            foreach (var period in allPeriods.Where(p => p.PeriodNum >= startPeriod.PeriodNum || p.Year > startPeriod.Year))
            {
                string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                string insertQuery = @"
                INSERT INTO AssetCost (AssetCode, Period, Cost)
                VALUES (@assetCode, @period, @cost)";

                using var insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                insertCmd.Parameters.AddWithValue("@period", periodString);
                insertCmd.Parameters.AddWithValue("@cost", asset.Cost);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task CalculateFullYearDepreciationAsync(SqlConnection conn, Asset asset, Period currentPeriod, List<Period> periodsInYear)
        {
            decimal depreciationRate = await GetDepreciationRateForAssetAsync(conn, asset.AssetGroup);
            if (depreciationRate <= 0) return;

            string depreciationMethod = await GetDepreciationMethodForAssetAsync(conn, asset.AssetGroup);

            // Determine when to start depreciation
            Period depreciationStartPeriod;
            if (asset.DepreciationStartDate > currentPeriod.EndDate)
            {
                var depreciationStart = periodsInYear.FirstOrDefault(p =>
                    p.Month == asset.DepreciationStartDate.Month &&
                    p.Year == asset.DepreciationStartDate.Year);
                depreciationStartPeriod = depreciationStart ?? currentPeriod;
            }
            else
            {
                depreciationStartPeriod = currentPeriod;
            }

            if (depreciationMethod == "Reducing Balance")
            {
                await CalculateReducingBalanceDepreciationAsync(conn, asset, depreciationStartPeriod, periodsInYear, depreciationRate);
            }
            else
            {
                await CalculateEqualInstallmentsDepreciationAsync(conn, asset, depreciationStartPeriod, periodsInYear, depreciationRate);
            }
        }

        private async Task CalculateReducingBalanceDepreciationAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> periodsInYear, decimal depreciationRate)
        {
            // Filter periods to only include those from the start period onward
            var periodsToCalculate = periodsInYear.Where(p =>
                p.Year > startPeriod.Year ||
                (p.Year == startPeriod.Year && p.PeriodNum >= startPeriod.PeriodNum)
            ).ToList();

            // Group periods by year
            var periodsByYear = periodsToCalculate
                .GroupBy(p => p.Year)
                .OrderBy(g => g.Key)
                .ToList();

            decimal currentBookValue = asset.Cost;

            foreach (var yearGroup in periodsByYear)
            {
                short year = yearGroup.Key;
                var yearPeriods = yearGroup.OrderBy(p => p.PeriodNum).ToList();

                // Calculate annual depreciation based on current book value at start of year
                decimal annualDepreciation = currentBookValue * (depreciationRate / 100m);
                decimal monthlyDepreciationRaw = annualDepreciation / 12m; // high-precision internal value

                // optional: rounding used only for storage/display
                decimal monthlyDepreciationStored = Math.Round(monthlyDepreciationRaw, 4);

                decimal accumulatedDepreciationForYear = 0m;

                foreach (var period in yearPeriods)
                {
                    string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                    // Use the raw value for accumulation to avoid rounding errors in book value.
                    accumulatedDepreciationForYear += monthlyDepreciationRaw;

                    // compute period-end book value using raw accumulated value, then round for storage
                    decimal periodEndBookValueRaw = currentBookValue - accumulatedDepreciationForYear;
                    decimal periodEndBookValueStored = Math.Round(periodEndBookValueRaw, 4);

                    string insertQuery = @"
                    INSERT INTO AssetDepreciation (AssetCode, Period, DepreciationRate, DepreciationAmount, Value)
                    VALUES (@assetCode, @period, @rate, @amount, @value)";

                    using var insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                    insertCmd.Parameters.AddWithValue("@period", periodString);
                    insertCmd.Parameters.AddWithValue("@rate", depreciationRate);
                    // store the rounded value (for display) but use raw for calculations
                    insertCmd.Parameters.AddWithValue("@amount", monthlyDepreciationStored);
                    insertCmd.Parameters.AddWithValue("@value", periodEndBookValueStored);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Subtract the actual applied depreciation (accumulatedDepreciationForYear),
                // not the full annualDepreciation when the year had fewer than 12 periods ---
                currentBookValue -= accumulatedDepreciationForYear;

                // Update cost records for the next year if this isn't the last year
                if (year != periodsByYear.Last().Key)
                {
                    await UpdateCostForNextYearAsync(conn, asset.Code, (short)(year + 1), currentBookValue);
                }
            }
        }


        private async Task CalculateEqualInstallmentsDepreciationAsync(SqlConnection conn, Asset asset, Period startPeriod, List<Period> periodsInYear, decimal depreciationRate)
        {
            // Filter periods to only include those from the start period onward
            var periodsToCalculate = periodsInYear.Where(p =>
                p.Year > startPeriod.Year ||
                (p.Year == startPeriod.Year && p.PeriodNum >= startPeriod.PeriodNum)
            ).ToList();

            decimal monthlyDepreciation = asset.Cost * (depreciationRate / 100m) / 12m;
            decimal accumulatedDepreciation = 0m;

            foreach (var period in periodsToCalculate)
            {
                string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                accumulatedDepreciation += monthlyDepreciation;
                decimal bookValue = asset.Cost - accumulatedDepreciation;

                string insertQuery = @"
                INSERT INTO AssetDepreciation (AssetCode, Period, DepreciationRate, DepreciationAmount, Value)
                VALUES (@assetCode, @period, @rate, @amount, @value)";

                using var insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                insertCmd.Parameters.AddWithValue("@period", periodString);
                insertCmd.Parameters.AddWithValue("@rate", depreciationRate);
                insertCmd.Parameters.AddWithValue("@amount", monthlyDepreciation);
                insertCmd.Parameters.AddWithValue("@value", bookValue);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateCostForNextYearAsync(SqlConnection conn, string assetCode, short nextYear, decimal newCost)
        {
            // Get all periods for the next year
            var nextYearPeriods = await _periodRepo.GetPeriodsByYearAsync(nextYear);

            foreach (var period in nextYearPeriods)
            {
                string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                string updateQuery = @"
                UPDATE AssetCost 
                SET Cost = @cost 
                WHERE AssetCode = @assetCode AND Period = @period";

                using var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@assetCode", assetCode);
                updateCmd.Parameters.AddWithValue("@period", periodString);
                updateCmd.Parameters.AddWithValue("@cost", newCost);

                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateAssetValuationAsync(Asset updatedAsset, decimal oldCost, string combinedPeriod)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Split into two parts
            var parts = combinedPeriod.Split('-');
            string stringPeriod = parts[0];
            short year = short.Parse(parts[1]);
            byte period = ConvertMonthAbbreviationToNumber(stringPeriod);

            // 1. Get the current period
            var currentPeriod = await _periodRepo.GetByPeriodAndYearAsync(period, year);
            if (currentPeriod == null) throw new Exception("No current period found.");

            // 2. Get ALL periods from current period onward (not just current year)
            var allPeriods = await GetAllPeriodsFromAsync(currentPeriod);
            if (allPeriods == null || !allPeriods.Any())
                throw new Exception($"No periods found from period {currentPeriod.PeriodNum}-{currentPeriod.Year}");

            // 3. Delete all future cost and depreciation records
            await DeleteFutureCostRecordsAsync(conn, updatedAsset.Code, currentPeriod);
            await DeleteFutureDepreciationRecordsAsync(conn, updatedAsset.Code, currentPeriod);

            // 4. Create cost records from CURRENT PERIOD onward (using theoretical calculation)
            await CreateInitialCostRecordsForAssetAsync(conn, updatedAsset, currentPeriod, allPeriods);

            // 5. Calculate depreciation from current period
            await CalculateFullYearDepreciationAsync(conn, updatedAsset, currentPeriod, allPeriods);

            // 6. Update the Transactions table with the revaluation
            var transactionRepo = new TransactionRepository();
            var transaction = new Transaction
            {
                AssetCode = updatedAsset.Code,
                TransactionType = "Asset Revalued",
                TransactionDate = DateTime.Today,
                Amount = updatedAsset.Cost - oldCost,
                Cost = updatedAsset.Cost
            };

            await transactionRepo.CreateAsync(transaction, combinedPeriod);

            // 7. Update the asset's cost in the Assets table
            await UpdateAssetCostAsync(updatedAsset.Code, updatedAsset.Cost);
        }

        private async Task RecalculateFutureDepreciationAsync(SqlConnection conn, Asset asset, Period currentPeriod, List<Period> periodsInYear, decimal oldCost)
        {
            decimal depreciationRate = await GetDepreciationRateForAssetAsync(conn, asset.AssetGroup);
            if (depreciationRate <= 0) return;

            string depreciationMethod = await GetDepreciationMethodForAssetAsync(conn, asset.AssetGroup);

            // Get accumulated depreciation up to the previous period
            decimal accumulatedDepreciationUpToNow = await GetAccumulatedDepreciationUpToPeriodAsync(conn, asset.Code, currentPeriod);

            // Delete future depreciation records
            string deleteQuery = @"
            DELETE FROM AssetDepreciation 
            WHERE AssetCode = @assetCode AND Period >= @currentPeriod";

            using var deleteCmd = new SqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@assetCode", asset.Code);
            deleteCmd.Parameters.AddWithValue("@currentPeriod", $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}");
            await deleteCmd.ExecuteNonQueryAsync();

            if (depreciationMethod == "Reducing Balance")
            {
                await RecalculateReducingBalanceDepreciationAsync(conn, asset, currentPeriod, periodsInYear, depreciationRate, accumulatedDepreciationUpToNow);
            }
            else
            {
                await RecalculateEqualInstallmentsDepreciationAsync(conn, asset, currentPeriod, periodsInYear, depreciationRate, accumulatedDepreciationUpToNow);
            }
        }

        private async Task RecalculateReducingBalanceDepreciationAsync(SqlConnection conn, Asset asset, Period currentPeriod, List<Period> periodsInYear, decimal depreciationRate, decimal accumulatedDepreciationUpToNow)
        {
            // Calculate current book value at the start of the current period
            decimal currentBookValue = asset.Cost - accumulatedDepreciationUpToNow;

            // Group future periods by year
            var futurePeriodsByYear = periodsInYear
                .Where(p => p.PeriodNum >= currentPeriod.PeriodNum)
                .GroupBy(p => p.Year)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var yearGroup in futurePeriodsByYear)
            {
                short year = yearGroup.Key;
                var yearPeriods = yearGroup.OrderBy(p => p.PeriodNum).ToList();

                // Calculate annual depreciation based on current book value at start of year
                decimal annualDepreciation = currentBookValue * (depreciationRate / 100m);
                decimal monthlyDepreciation = annualDepreciation / 12m;

                decimal accumulatedDepreciationForYear = 0m;

                foreach (var period in yearPeriods)
                {
                    string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                    accumulatedDepreciationForYear += monthlyDepreciation;
                    decimal periodEndBookValue = currentBookValue - accumulatedDepreciationForYear;

                    string insertQuery = @"
                    INSERT INTO AssetDepreciation (AssetCode, Period, DepreciationRate, DepreciationAmount, Value)
                    VALUES (@assetCode, @period, @rate, @amount, @value)";

                    using var insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                    insertCmd.Parameters.AddWithValue("@period", periodString);
                    insertCmd.Parameters.AddWithValue("@rate", depreciationRate);
                    insertCmd.Parameters.AddWithValue("@amount", monthlyDepreciation);
                    insertCmd.Parameters.AddWithValue("@value", periodEndBookValue);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Update book value for next year (reduce by the full annual depreciation)
                currentBookValue -= annualDepreciation;

                // Update cost records for the next year if this isn't the last year
                if (year != futurePeriodsByYear.Last().Key)
                {
                    await UpdateCostForNextYearAsync(conn, asset.Code, (short)(year + 1), currentBookValue);
                }
            }
        }

        private async Task RecalculateEqualInstallmentsDepreciationAsync(SqlConnection conn, Asset asset, Period currentPeriod, List<Period> periodsInYear, decimal depreciationRate, decimal accumulatedDepreciationUpToNow)
        {
            decimal monthlyDepreciation = asset.Cost * (depreciationRate / 100m) / 12m;
            decimal accumulatedDepreciation = accumulatedDepreciationUpToNow;

            foreach (var period in periodsInYear.Where(p => p.PeriodNum >= currentPeriod.PeriodNum))
            {
                string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                accumulatedDepreciation += monthlyDepreciation;
                decimal bookValue = asset.Cost - accumulatedDepreciation;

                string insertQuery = @"
                INSERT INTO AssetDepreciation (AssetCode, Period, DepreciationRate, DepreciationAmount, Value)
                VALUES (@assetCode, @period, @rate, @amount, @value)";

                using var insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@assetCode", asset.Code);
                insertCmd.Parameters.AddWithValue("@period", periodString);
                insertCmd.Parameters.AddWithValue("@rate", depreciationRate);
                insertCmd.Parameters.AddWithValue("@amount", monthlyDepreciation);
                insertCmd.Parameters.AddWithValue("@value", bookValue);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<decimal> GetAccumulatedDepreciationUpToPeriodAsync(SqlConnection conn, string assetCode, Period currentPeriod)
        {
            // Get sum of depreciation up to the period BEFORE the current one
            string query = @"
            SELECT ISNULL(SUM(DepreciationAmount), 0) 
            FROM AssetDepreciation 
            WHERE AssetCode = @assetCode 
            AND Period < @currentPeriod";

            string currentPeriodString = $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@assetCode", assetCode);
            cmd.Parameters.AddWithValue("@currentPeriod", currentPeriodString);

            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? (decimal)result : 0m;
        }

        public async Task UpdateAssetCostAsync(string assetCode, decimal newCost)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string query = "UPDATE Assets SET Cost = @newCost WHERE Code = @code";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@newCost", newCost);
            cmd.Parameters.AddWithValue("@code", assetCode);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new Exception($"Asset with code {assetCode} not found.");
            }
        }

        public async Task ScrapAssetAsync(string assetCode, string combinedPeriod)
        {
            string reason = "Asset Scrapped";

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            try
            {
                // Split into two parts
                var parts = combinedPeriod.Split('-');

                byte period = byte.Parse(parts[0]);
                short year = short.Parse(parts[1]);

                // 1. Get the current period
                var currentPeriod = await _periodRepo.GetByPeriodAndYearAsync(period, year);
                if (currentPeriod == null) throw new Exception("No current period found.");

                // 2. Get the asset details
                var asset = await _assetRepo.GetByCodeAsync(assetCode);
                if (asset == null) throw new Exception($"Asset {assetCode} not found.");

                // 3. Get all periods for the current year
                var periodsInYear = await _periodRepo.GetPeriodsByYearAsync(currentPeriod.Year);
                if (periodsInYear == null || !periodsInYear.Any())
                    throw new Exception($"No periods found for year {currentPeriod.Year}");

                // 4. Update AssetCost table: Set cost to 0 from current period onward
                await UpdateCostRecordsForScrappedAssetAsync(conn, assetCode, currentPeriod, periodsInYear);

                // 5. Update AssetDepreciation table: Set depreciation to 0 from current period onward
                await UpdateDepreciationForScrappedAssetAsync(conn, assetCode, currentPeriod, periodsInYear);

                // 6. Create transaction record
                await CreateScrappingTransactionAsync(conn, assetCode, asset.Cost, reason, currentPeriod);

                // 7. Update the asset's cost in the Assets table to 0.
                await UpdateAssetCostAsync(assetCode, 0m);
            }
            catch
            {
                throw;
            }
        }

        private async Task UpdateCostRecordsForScrappedAssetAsync(SqlConnection conn, string assetCode, Period currentPeriod, List<Period> periodsInYear)
        {
            foreach (var period in periodsInYear.Where(p => p.PeriodNum >= currentPeriod.PeriodNum))
            {
                string periodString = $"{period.PeriodNum:D2}-{period.Year}";

                string updateQuery = @"
                UPDATE AssetCost 
                SET Cost = 0 
                WHERE AssetCode = @assetCode AND Period = @period";

                using var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@assetCode", assetCode);
                updateCmd.Parameters.AddWithValue("@period", periodString);

                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateDepreciationForScrappedAssetAsync(SqlConnection conn, string assetCode, Period currentPeriod, List<Period> periodsInYear)
        {
            decimal accumulatedDepreciation = await GetAccumulatedDepreciationUpToPeriodAsync(conn, assetCode, currentPeriod);

            // Delete future depreciation records (from current period onward)
            string deleteQuery = @"
            DELETE FROM AssetDepreciation 
            WHERE AssetCode = @assetCode AND Period >= @currentPeriod";

            using var deleteCmd = new SqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@assetCode", assetCode);
            deleteCmd.Parameters.AddWithValue("@currentPeriod", $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}");
            await deleteCmd.ExecuteNonQueryAsync();

            // Insert final depreciation record showing the asset is fully written off
            string finalPeriodString = $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}";

            string insertQuery = @"
            INSERT INTO AssetDepreciation (AssetCode, Period, DepreciationRate, DepreciationAmount, Value)
            VALUES (@assetCode, @period, 0, 0, 0)";

            using var insertCmd = new SqlCommand(insertQuery, conn);
            insertCmd.Parameters.AddWithValue("@assetCode", assetCode);
            insertCmd.Parameters.AddWithValue("@period", finalPeriodString);
            await insertCmd.ExecuteNonQueryAsync();
        }

        private async Task CreateScrappingTransactionAsync(SqlConnection conn, string assetCode, decimal originalCost, string reason, Period currentPeriod)
        {
            string periodString = $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}";

            string insertQuery = @"
            INSERT INTO Transactions (AssetCode, Period, TransactionType, TransactionDate, Amount, Cost)
            VALUES (@assetCode, @period, @type, @date, @amount, 0)";

            using var insertCmd = new SqlCommand(insertQuery, conn);
            insertCmd.Parameters.AddWithValue("@assetCode", assetCode);
            insertCmd.Parameters.AddWithValue("@period", periodString);
            insertCmd.Parameters.AddWithValue("@type", "Asset Scrapped");
            insertCmd.Parameters.AddWithValue("@date", DateTime.Today);
            insertCmd.Parameters.AddWithValue("@amount", -originalCost); // Negative amount for disposal

            await insertCmd.ExecuteNonQueryAsync();
        }

        // ===== REPORTS & QUERIES =====

        public async Task<List<(string Period, decimal Cost)>> GetAssetCostHistoryAsync(string assetCode)
        {
            var costHistory = new List<(string Period, decimal Cost)>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string query = @"
                SELECT Period, Cost
                FROM AssetCost 
                WHERE AssetCode = @assetCode
                ORDER BY AssetCostID";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@assetCode", assetCode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                costHistory.Add((FormatPeriodDisplay(reader.GetString(0)), reader.GetDecimal(1)));
            }

            return costHistory;
        }

        public async Task<List<(string Period, decimal DepreciationRate, decimal DepreciationAmount, decimal BookValue)>> GetAssetDepreciationHistoryAsync(string assetCode)
        {
            var depreciationHistory = new List<(string, decimal, decimal, decimal)>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string query = @"
                SELECT Period, DepreciationRate, DepreciationAmount, Value 
                FROM AssetDepreciation 
                WHERE AssetCode = @assetCode
                ORDER BY DepreciationID";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@assetCode", assetCode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                depreciationHistory.Add((
                    FormatPeriodDisplay(reader.GetString(0)),
                    reader.GetDecimal(1),
                    reader.GetDecimal(2),
                    reader.GetDecimal(3)
                ));
            }

            return depreciationHistory;
        }

        private async Task<List<Asset>> GetAssetsByDepreciationCodeAsync(SqlConnection conn, string depreciationCode)
        {
            var assets = new List<Asset>();

            string query = @"
            SELECT a.Code, a.Description, a.AssetGroup, a.AssetCategory, a.Department, 
                   a.Location, a.PurchaseDate, a.DepreciationStartDate, a.TransactionDate, 
                   a.PurchaseAmount, a.Cost, a.TrackingCode, a.SerialNumber
            FROM Assets a
            INNER JOIN AssetGroups ag ON a.AssetGroup = ag.GroupCode
            WHERE ag.DepreciationCode = @depreciationCode";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@depreciationCode", depreciationCode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                assets.Add(new Asset
                {
                    Code = reader.GetString(0),
                    Description = reader.GetString(1),
                    AssetGroup = reader.GetString(2),
                    AssetCategory = reader.GetString(3),
                    Department = reader.GetString(4),
                    Location = reader.GetString(5),
                    PurchaseDate = reader.GetDateTime(6),
                    DepreciationStartDate = reader.GetDateTime(7),
                    TransactionDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8),
                    PurchaseAmount = reader.GetDecimal(9),
                    Cost = reader.GetDecimal(10),
                    TrackingCode = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                    SerialNumber = reader.IsDBNull(12) ? string.Empty : reader.GetString(12)
                });
            }

            return assets;
        }

        public async Task RecalculateAllAssetsDepreciationAsync(string depreciationCode)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // 1. Get the current period
            var currentPeriod = await _periodRepo.GetCurrentPeriodAsync();
            if (currentPeriod == null) throw new Exception("No current period found.");

            // 2. Get all periods for the current year
            var periodsInYear = await _periodRepo.GetPeriodsByYearAsync(currentPeriod.Year);
            if (periodsInYear == null || !periodsInYear.Any())
                throw new Exception($"No periods found for year {currentPeriod.Year}");

            // 3. Get all assets that use this depreciation code
            var assetsToUpdate = await GetAssetsByDepreciationCodeAsync(conn, depreciationCode);

            // 4. Recalculate depreciation for each asset
            foreach (var asset in assetsToUpdate)
            {
                await RecalculateFutureDepreciationAsync(conn, asset, currentPeriod, periodsInYear, asset.Cost);
            }
        }

        // ===== HELPER METHODS =====

        private async Task<decimal> GetBookValueAtEndOfYearAsync(SqlConnection conn, string assetCode, short year)
        {
            // Get the last period of the year
            string lastPeriodOfYear = $"12-{year}";

            string query = @"
            SELECT Value 
            FROM AssetDepreciation 
            WHERE AssetCode = @assetCode AND Period = @period
            ORDER BY DepreciationID DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@assetCode", assetCode);
            cmd.Parameters.AddWithValue("@period", lastPeriodOfYear);

            var result = await cmd.ExecuteScalarAsync();

            // If no depreciation record exists for the end of the year, 
            // fall back to the cost from AssetCost table
            if (result == DBNull.Value || result == null)
            {
                string costQuery = "SELECT Cost FROM AssetCost WHERE AssetCode = @assetCode AND Period = @period";
                using var costCmd = new SqlCommand(costQuery, conn);
                costCmd.Parameters.AddWithValue("@assetCode", assetCode);
                costCmd.Parameters.AddWithValue("@period", lastPeriodOfYear);

                var costResult = await costCmd.ExecuteScalarAsync();
                return costResult != DBNull.Value ? (decimal)costResult : 0m;
            }

            return (decimal)result;
        }

        private async Task DeleteFutureCostRecordsAsync(SqlConnection conn, string assetCode, Period currentPeriod)
        {
            string maxIdQuery = @"
            SELECT MAX(AssetCostID)
            FROM AssetCost
            WHERE AssetCode = @assetCode
            AND (
                CAST(RIGHT(Period, 4) AS INT) < @year
                OR (
                    CAST(RIGHT(Period, 4) AS INT) = @year
                    AND CAST(LEFT(Period, 2) AS INT) < @periodNum
                )
            )";

            using var maxIdCmd = new SqlCommand(maxIdQuery, conn);
            maxIdCmd.Parameters.AddWithValue("@assetCode", assetCode);
            maxIdCmd.Parameters.AddWithValue("@year", currentPeriod.Year);
            maxIdCmd.Parameters.AddWithValue("@periodNum", currentPeriod.PeriodNum);

            var maxIdResult = await maxIdCmd.ExecuteScalarAsync();

            if (maxIdResult != DBNull.Value && maxIdResult != null)
            {
                int maxAssetCostId = (int)maxIdResult;

                string deleteQuery = @"
                DELETE FROM AssetCost 
                WHERE AssetCode = @assetCode 
                AND AssetCostID > @maxAssetCostId";

                using var deleteCmd = new SqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@assetCode", assetCode);
                deleteCmd.Parameters.AddWithValue("@maxAssetCostId", maxAssetCostId);

                await deleteCmd.ExecuteNonQueryAsync();
            }
            else
            {
                string deleteAllQuery = "DELETE FROM AssetCost WHERE AssetCode = @assetCode";
                using var deleteAllCmd = new SqlCommand(deleteAllQuery, conn);
                deleteAllCmd.Parameters.AddWithValue("@assetCode", assetCode);
                await deleteAllCmd.ExecuteNonQueryAsync();
            }
        }


        private async Task DeleteFutureDepreciationRecordsAsync(SqlConnection conn, string assetCode, Period currentPeriod)
        {
            string maxIdQuery = @"
            SELECT MAX(DepreciationID)
            FROM AssetDepreciation
            WHERE AssetCode = @assetCode
            AND (
                CAST(RIGHT(Period, 4) AS INT) < @year
                OR (
                    CAST(RIGHT(Period, 4) AS INT) = @year
                    AND CAST(LEFT(Period, 2) AS INT) < @periodNum
                )
            )";

            using var maxIdCmd = new SqlCommand(maxIdQuery, conn);
            maxIdCmd.Parameters.AddWithValue("@assetCode", assetCode);
            maxIdCmd.Parameters.AddWithValue("@year", currentPeriod.Year);
            maxIdCmd.Parameters.AddWithValue("@periodNum", currentPeriod.PeriodNum);

            var maxIdResult = await maxIdCmd.ExecuteScalarAsync();

            if (maxIdResult != DBNull.Value && maxIdResult != null)
            {
                int maxDepreciationId = (int)maxIdResult;

                string deleteQuery = @"
                DELETE FROM AssetDepreciation 
                WHERE AssetCode = @assetCode 
                AND DepreciationID > @maxDepreciationId";

                using var deleteCmd = new SqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@assetCode", assetCode);
                deleteCmd.Parameters.AddWithValue("@maxDepreciationId", maxDepreciationId);

                await deleteCmd.ExecuteNonQueryAsync();
            }
            else
            {
                string deleteAllQuery = "DELETE FROM AssetDepreciation WHERE AssetCode = @assetCode";
                using var deleteAllCmd = new SqlCommand(deleteAllQuery, conn);
                deleteAllCmd.Parameters.AddWithValue("@assetCode", assetCode);
                await deleteAllCmd.ExecuteNonQueryAsync();
            }
        }


        private async Task<List<Period>> GetAllPeriodsFromAsync(Period startPeriod)
        {
            var allPeriods = new List<Period>();

            // Get ALL periods for the start year (not just from start period)
            var currentYearPeriods = await _periodRepo.GetPeriodsByYearAsync(startPeriod.Year);
            allPeriods.AddRange(currentYearPeriods);

            // Get periods for all subsequent years
            short nextYear = (short)(startPeriod.Year + 1);
            var maxYears = 10; // Safety limit to prevent infinite loop

            for (int i = 0; i < maxYears; i++)
            {
                var nextYearPeriods = await _periodRepo.GetPeriodsByYearAsync(nextYear);
                if (nextYearPeriods == null || !nextYearPeriods.Any())
                    break;

                allPeriods.AddRange(nextYearPeriods);
                nextYear++;
            }

            return allPeriods.OrderBy(p => p.Year).ThenBy(p => p.PeriodNum).ToList();
        }

        private async Task<string> GetDepreciationMethodForAssetAsync(SqlConnection conn, string assetGroupCode)
        {
            string query = @"
            SELECT d.DepreciationMethod
            FROM Depreciation d
            INNER JOIN AssetGroups ag ON d.Code = ag.DepreciationCode
            WHERE ag.GroupCode = @groupCode";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@groupCode", assetGroupCode);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? result.ToString() : "Equal Instalments";
        }

        public string FormatPeriodDisplay(string period)
        {
            if (string.IsNullOrEmpty(period))
                return period;

            var parts = period.Split('-');
            if (parts.Length != 2)
                return period;

            if (byte.TryParse(parts[0], out byte monthNumber) && short.TryParse(parts[1], out short year))
            {
                string monthAbbreviation = monthNumber switch
                {
                    1 => "JAN",
                    2 => "FEB",
                    3 => "MAR",
                    4 => "APR",
                    5 => "MAY",
                    6 => "JUN",
                    7 => "JUL",
                    8 => "AUG",
                    9 => "SEP",
                    10 => "OCT",
                    11 => "NOV",
                    12 => "DEC",
                    _ => parts[0] // Return original if invalid month
                };

                return $"{monthAbbreviation}-{year}";
            }

            return period; // Return original if parsing fails
        }

        public byte ConvertMonthAbbreviationToNumber(string monthAbbreviation)
        {
            if (string.IsNullOrEmpty(monthAbbreviation))
                return 0;

            return monthAbbreviation.ToUpper() switch
            {
                "JAN" => 1,
                "FEB" => 2,
                "MAR" => 3,
                "APR" => 4,
                "MAY" => 5,
                "JUN" => 6,
                "JUL" => 7,
                "AUG" => 8,
                "SEP" => 9,
                "OCT" => 10,
                "NOV" => 11,
                "DEC" => 12,
                _ => 0 // Return 0 for invalid month abbreviations
            };
        }

        private async Task<decimal> GetDepreciationRateForAssetAsync(SqlConnection conn, string assetGroupCode)
        {
            string query = @"
                SELECT d.DepreciationRate
                FROM Depreciation d
                INNER JOIN AssetGroups ag ON d.Code = ag.DepreciationCode
                WHERE ag.GroupCode = @groupCode";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@groupCode", assetGroupCode);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (decimal)result : 0m;
        }

        public async Task<List<string>> GetAssetCostPeriodsAsync(string assetCode)
        {
            var periods = new List<string>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string query = "SELECT Period FROM AssetCost WHERE AssetCode = @code ORDER BY AssetCostID";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@code", assetCode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                periods.Add(FormatPeriodDisplay(reader.GetString(0)));
            }

            return periods;
        }

        public async Task<List<Dictionary<string, object>>> GetDepreciationInRangeAsync(string assetCode, DateTime startDate, DateTime endDate)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var startPeriod = new DateTime(startDate.Year, startDate.Month, 1);
            var isEndOfMonth = endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month);
            var endPeriod = new DateTime(endDate.Year, endDate.Month, 1);
            if (!isEndOfMonth)
            {
                endPeriod = endPeriod.AddMonths(-1);
            }

            var startKey = int.Parse(startPeriod.ToString("yyyyMM"));
            var endKey = int.Parse(endPeriod.ToString("yyyyMM"));

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT * 
            FROM AssetDepreciation
            WHERE AssetCode = @assetCode
              AND (CAST(RIGHT(Period, 4) + SUBSTRING(Period, 1, 2) AS INT) 
                   BETWEEN @startPeriod AND @endPeriod)
            ORDER BY DepreciationID";

            cmd.Parameters.AddWithValue("@assetCode", assetCode);
            cmd.Parameters.AddWithValue("@startPeriod", startKey);
            cmd.Parameters.AddWithValue("@endPeriod", endKey);

            var result = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetAssetCostInRangeAsync(string assetCode, DateTime startDate, DateTime endDate)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var startPeriod = new DateTime(startDate.Year, startDate.Month, 1);
            var isEndOfMonth = endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month);
            var endPeriod = new DateTime(endDate.Year, endDate.Month, 1);
            if (!isEndOfMonth)
            {
                endPeriod = endPeriod.AddMonths(-1);
            }

            var startKey = int.Parse(startPeriod.ToString("yyyyMM"));
            var endKey = int.Parse(endPeriod.ToString("yyyyMM"));

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT * 
            FROM AssetCost
            WHERE AssetCode = @assetCode
              AND (CAST(RIGHT(Period, 4) + SUBSTRING(Period, 1, 2) AS INT) 
                   BETWEEN @startPeriod AND @endPeriod)
            ORDER BY AssetCostID";

            cmd.Parameters.AddWithValue("@assetCode", assetCode);
            cmd.Parameters.AddWithValue("@startPeriod", startKey);
            cmd.Parameters.AddWithValue("@endPeriod", endKey);

            var result = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetTotalDepreciationAsync()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT 
                a.Code AS AssetCode,
                a.Description,
                ISNULL(SUM(ad.DepreciationAmount), 0) AS TotalDepreciation
            FROM 
                Assets a
            LEFT JOIN 
                AssetDepreciation ad
                ON a.Code = ad.AssetCode
            GROUP BY 
                a.Code,
                a.Description
            ORDER BY 
                a.Code";

            var result = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetTotalDepreciationByCategoryAsync(string assetCategory)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT 
                a.Code AS AssetCode,
                a.Description,
                ISNULL(SUM(ad.DepreciationAmount), 0) AS TotalDepreciation
            FROM 
                Assets a
            LEFT JOIN 
                AssetDepreciation ad
                ON a.Code = ad.AssetCode
            WHERE
                (@assetCategory IS NULL OR a.AssetCategory = @assetCategory)
            GROUP BY 
                a.Code,
                a.Description
            ORDER BY 
                a.Code";

            cmd.Parameters.AddWithValue("@assetCategory", string.IsNullOrEmpty(assetCategory) ? (object)DBNull.Value : assetCategory);

            var result = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetTotalDepreciationInRangeAsync(string assetCategory, DateTime startDate, DateTime endDate)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT 
                a.Code AS AssetCode,
                a.Description,
                ISNULL(SUM(ad.DepreciationAmount), 0) AS TotalDepreciation
            FROM 
                Assets a
            LEFT JOIN 
                AssetDepreciation ad
                ON a.Code = ad.AssetCode
                AND TRY_CONVERT(DATE, '01-' + ad.Period, 105) BETWEEN @startDate AND @endDate
            WHERE 
                (@assetCategory IS NULL OR a.AssetCategory = @assetCategory)
            GROUP BY 
                a.Code,
                a.Description
            ORDER BY 
                a.Code";

            cmd.Parameters.AddWithValue("@assetCategory", string.IsNullOrEmpty(assetCategory) ? (object)DBNull.Value : assetCategory);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            var result = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }
    }
}