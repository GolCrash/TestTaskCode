using MongoDB.Driver;
using TestCode.Data;
using TestCode.DTOs;
using TestCode.Models;

namespace TestCode.Services
{
    public class PeriodService
    {
        private readonly MongoDbContext _context;

        public PeriodService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task CloseAsync(PeriodRequest request)
        {
            var periods = _context.Database.GetCollection<ClosedPeriod>("closed_periods");

            var existing = await periods.Find(p => p.Year == request.Year && p.Month == request.Month) .FirstOrDefaultAsync();

            if (existing != null)
                throw new BusinessException("PERIOD_ALREADY_CLOSED", "Период уже закрыт.");
            
            var period = new ClosedPeriod
            {
                Year = request.Year,
                Month = request.Month
            };

            await periods.InsertOneAsync(period);
        }

        public async Task OpenAsync(PeriodRequest request)
        {
            var periods = _context.Database.GetCollection<ClosedPeriod>("closed_periods");

            var result = await periods.DeleteOneAsync(p =>p.Year == request.Year && p.Month == request.Month);

            if (result.DeletedCount == 0)
                throw new BusinessException("PERIOD_NOT_CLOSED", "Период не был закрыт.");
            
        }
    }
}