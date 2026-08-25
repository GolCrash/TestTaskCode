using MongoDB.Bson;
using MongoDB.Driver;
using TestCode.Models;

namespace TestCode.Data
{
    public class MongoDbInitializer
    {
        protected readonly MongoDbContext _context;
        protected readonly MongoDbIndexes _indexes;
        public MongoDbInitializer(MongoDbContext context, MongoDbIndexes indexes)
        {
            _context = context;
            _indexes = indexes;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            await _indexes.CreateIndexesAsync();

            Console.WriteLine("MongoDB подключена");
        }

        public async Task SeedAsync()
        {
            var employees = _context.Database.GetCollection<Employee>("employees");
            var projects = _context.Database.GetCollection<Project>("projects");
            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");
            var closedPeriods = _context.Database.GetCollection<ClosedPeriod>("closed_periods");



            var emptyFilter = Builders<Employee>.Filter.Empty;

            if (await employees.CountDocumentsAsync(emptyFilter) > 0)
                return;

            var employee1Id = Guid.NewGuid().ToString();
            var employee2Id = Guid.NewGuid().ToString();

            var project1Id = Guid.NewGuid().ToString();
            var project2Id = Guid.NewGuid().ToString();

            var employee1 = new Employee
            {
                Id = employee1Id,
                LastName = "Иванов",
                Name = "Иван",
                MiddleName = "Иванович",
                Department = "Разработка",
                Rates = new List<Rate>
        {
            new Rate
            {
                From = new DateTime(2026, 1, 1),
                Value = 500
            },
            new Rate
            {
                From = new DateTime(2026, 3, 1),
                Value = 600
            }
        }
            };

            var employee2 = new Employee
            {
                Id = employee2Id,
                LastName = "Петрова",
                Name = "Анна",
                MiddleName = "Сергеевна",
                Department = "Разработка",
                Rates = new List<Rate>
        {
            new Rate
            {
                From = new DateTime(2026, 1, 1),
                Value = 700
            }
        }
            };

            var project1 = new Project
            {
                Id = project1Id,
                ProjectCode = "П-001",
                ProjectName = "ERP-система",
                Budget = 20000,
                //ProjectStart = new DateTime(2026, 1, 1),
                //ProjectEnd = new DateTime(2026, 12, 31)
                ProjectStart = new DateTime(2025, 1, 1),
                ProjectEnd = new DateTime(2025, 12, 31)
            };

            var project2 = new Project
            {
                Id = project2Id,
                ProjectCode = "П-002",
                ProjectName = "Мобильное приложение",
                Budget = 5000,
                ProjectStart = new DateTime(2026, 2, 1),
                ProjectEnd = new DateTime(2026, 10, 31)
            };

            await employees.InsertManyAsync(
                new[] { employee1, employee2 });

            await projects.InsertManyAsync(
                new[] { project1, project2 });

            var closedPeriod = new ClosedPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Year = 2026,
                Month = 2
            };

            await closedPeriods.InsertOneAsync(closedPeriod);

            var timeEntry1 = new TimeEntry
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employee1Id,
                ProjectId = project1Id,
                Date = new DateTime(2026, 3, 5),
                Hours = 8,
                Comment = "Разработка backend",
                IsOvertime = false,
                Version = 1
            };

            var timeEntry2 = new TimeEntry
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employee2Id,
                ProjectId = project1Id,
                Date = new DateTime(2026, 3, 6),
                Hours = 4,
                Comment = "Разработка frontend",
                IsOvertime = false,
                Version = 1
            };

            var timeEntry3 = new TimeEntry
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employee2Id,
                ProjectId = project2Id,
                Date = new DateTime(2026, 3, 7),
                Hours = 10,
                Comment = "Разработка приложения",
                IsOvertime = false,
                Version = 1
            };

            await timeEntries.InsertManyAsync(
                new[] { timeEntry1, timeEntry2, timeEntry3 });
        }
    }
}
