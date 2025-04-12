using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GhostfolioSidekick.Activities
{
    public class TrackAverageUnitPriceAndProfitLossTask : IScheduledWork
    {
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

        public TaskPriority Priority => TaskPriority.TrackAverageUnitPriceAndProfitLoss;

        public TimeSpan ExecutionFrequency => Frequencies.Daily;

        public bool ExceptionsAreFatal => false;

        public TrackAverageUnitPriceAndProfitLossTask(IDbContextFactory<DatabaseContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task DoWork()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var holdings = await dbContext.Holdings.Include(h => h.Activities).ToListAsync();

            foreach (var holding in holdings)
            {
                var firstPurchaseDate = holding.Activities
                    .Where(a => a is BuySellActivity)
                    .Min(a => a.Date);

                var currentDate = DateTime.UtcNow.Date;

                for (var date = firstPurchaseDate; date <= currentDate; date = date.AddDays(1))
                {
                    var activitiesUpToDate = holding.Activities
                        .Where(a => a.Date <= date)
                        .ToList();

                    var averageUnitPrice = CalculateAverageUnitPrice(activitiesUpToDate);
                    var profitLoss = CalculateProfitLoss(activitiesUpToDate, averageUnitPrice);

                    var result = new AverageUnitPriceAndProfitLoss
                    {
                        HoldingId = holding.Id,
                        Date = date,
                        AverageUnitPrice = averageUnitPrice,
                        ProfitLoss = profitLoss
                    };

                    dbContext.AverageUnitPriceAndProfitLoss.Add(result);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private decimal CalculateAverageUnitPrice(List<Activity> activities)
        {
            var totalQuantity = activities.OfType<BuySellActivity>().Sum(a => a.Quantity);
            var totalCost = activities.OfType<BuySellActivity>().Sum(a => a.Quantity * a.UnitPrice.Amount);

            return totalQuantity == 0 ? 0 : totalCost / totalQuantity;
        }

        private decimal CalculateProfitLoss(List<Activity> activities, decimal averageUnitPrice)
        {
            var totalValue = activities.OfType<BuySellActivity>().Sum(a => a.Quantity * a.UnitPrice.Amount);
            var totalCost = activities.OfType<BuySellActivity>().Sum(a => a.Quantity * averageUnitPrice);

            return totalValue - totalCost;
        }
    }

    public class AverageUnitPriceAndProfitLoss
    {
        public int Id { get; set; }
        public int HoldingId { get; set; }
        public DateTime Date { get; set; }
        public decimal AverageUnitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
    }
}
