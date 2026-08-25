using MongoDB.Driver;
using TestCode.Data;
using TestCode.DTOs;
using TestCode.Models;
using TestCode.Services;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void GetRateForDate_ShouldReturnRateThatWasActiveOnDate()
        {
            var employee = new Employee
            {
                LastName = "Иванов",
                Name = "Иван",
                MiddleName = "Иванович",
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

            var service = new TimeEntryService(null);

            var result = service.GetRateForDate(
                employee,
                new DateTime(2026, 2, 20));

            Assert.Equal(500, result.Value);
        }

        [Fact]
        public void ValidateHours_ShouldRejectInvalidHours()
        {
            var service = new TimeEntryService(null);

            var exception = Assert.Throws<BusinessException>(
                () => service.ValidateHours(3.7m));

            Assert.Equal("INVALID_HOURS", exception.Code);
        }

        [Fact]
        public void ValidateProjectDate_ShouldAcceptDateInsideProjectPeriod()
        {
            var service = new TimeEntryService(null);

            var project = new Project
            {
                ProjectStart = new DateTime(2026, 1, 1),
                ProjectEnd = new DateTime(2026, 3, 31)
            };

            service.ValidateProjectDate(
                project,
                new DateTime(2026, 3, 31));
        }

        [Fact]
        public async Task CreateAsync_ShouldRejectClosedPeriod()
        {
            var settings = new MongoDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "testwork"
            };

            var context = new MongoDbContext(settings);

            var service = new TimeEntryService(context);

            var request = new TimeEntryRequest
            {
                EmployeeId = "f92a965a-d4d3-4724-b2c0-23a710a15ca1",
                ProjectId = "4d0fbd60-976c-4b35-a77f-e0ffdfc4e1d7",
                Date = new DateTime(2026, 2, 20),
                Hours = 8,
                Comment = "Тест"
            };

            var exception = await Assert.ThrowsAsync<BusinessException>(
                () => service.CreateAsync(request));

            Assert.Equal("PERIOD_CLOSED", exception.Code);
        }

        [Fact]
        public async Task CreateAsync_ShouldRoundAmountToTwoDecimalPlaces()
        {
            var settings = new MongoDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "testwork"
            };

            var context = new MongoDbContext(settings);
            var service = new TimeEntryService(context);

            var employeeId = Guid.NewGuid().ToString();
            var projectId = Guid.NewGuid().ToString();

            var employee = new Employee
            {
                Id = employeeId,
                LastName = "Тестовый",
                Name = "Иван",
                MiddleName = "Иванович",
                Department = "Тестовый",
                Rates = new List<Rate>
        {
            new Rate
            {
                From = new DateTime(2026, 1, 1),
                Value = 333.333m
            }
        }
            };

            var project = new Project
            {
                Id = projectId,
                ProjectCode = "TEST-001",
                ProjectName = "Тестовый проект",
                Budget = 10000,
                ProjectStart = new DateTime(2026, 1, 1),
                ProjectEnd = new DateTime(2026, 12, 31)
            };

            await context.Database
                .GetCollection<Employee>("employees")
                .InsertOneAsync(employee);

            await context.Database
                .GetCollection<Project>("projects")
                .InsertOneAsync(project);

            try
            {
                var request = new TimeEntryRequest
                {
                    EmployeeId = employeeId,
                    ProjectId = projectId,
                    Date = new DateTime(2026, 4, 1),
                    Hours = 1.5m,
                    Comment = "Тест округления"
                };

                var result = await service.CreateAsync(request);

                Assert.Equal(500.00m, result.Amount);
            }
            finally
            {
                await context.Database
                    .GetCollection<Employee>("employees")
                    .DeleteOneAsync(x => x.Id == employeeId);

                await context.Database
                    .GetCollection<Project>("projects")
                    .DeleteOneAsync(x => x.Id == projectId);

                await context.Database
                    .GetCollection<TimeEntry>("time_entries")
                    .DeleteManyAsync(x => x.EmployeeId == employeeId);
            }
        }
    }
}