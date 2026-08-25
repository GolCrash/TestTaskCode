using MongoDB.Driver;
using TestCode.Models;

namespace TestCode.Data
{
    public class MongoDbIndexes
    {
        private readonly MongoDbContext _context;

        public MongoDbIndexes(MongoDbContext context)
        {
            _context = context;
        }

        public async Task CreateIndexesAsync()
        {
            var timeEntries =
                _context.Database.GetCollection<TimeEntry>("time_entries");

            var projects =
                _context.Database.GetCollection<Project>("projects");

            var closedPeriods =
                _context.Database.GetCollection<ClosedPeriod>("closed_periods");

            var timeEntryDateIndex =
                new CreateIndexModel<TimeEntry>(
                    Builders<TimeEntry>.IndexKeys
                        .Ascending(x => x.Date));

            var employeeDateIndex =
                new CreateIndexModel<TimeEntry>(
                    Builders<TimeEntry>.IndexKeys
                        .Ascending(x => x.EmployeeId)
                        .Ascending(x => x.Date));

            var projectDateIndex =
                new CreateIndexModel<TimeEntry>(
                    Builders<TimeEntry>.IndexKeys
                        .Ascending(x => x.ProjectId)
                        .Ascending(x => x.Date));

            var projectCodeIndex =
                new CreateIndexModel<Project>(
                    Builders<Project>.IndexKeys
                        .Ascending(x => x.ProjectCode),
                    new CreateIndexOptions
                    {
                        Unique = true
                    });

            var closedPeriodIndex =
                new CreateIndexModel<ClosedPeriod>(
                    Builders<ClosedPeriod>.IndexKeys
                        .Ascending(x => x.Year)
                        .Ascending(x => x.Month),
                    new CreateIndexOptions
                    {
                        Unique = true
                    });

            await timeEntries.Indexes.CreateManyAsync(new[]
            {
                timeEntryDateIndex,
                employeeDateIndex,
                projectDateIndex
            });

            await projects.Indexes.CreateOneAsync(projectCodeIndex);

            await closedPeriods.Indexes.CreateOneAsync(closedPeriodIndex);
        }
    }
}