using MongoDB.Driver;
using TestCode.Data;
using TestCode.DTOs;
using TestCode.Models;

namespace TestCode.Services
{
    public class TimeEntryService
    {
        private readonly MongoDbContext _context;

        public TimeEntryService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<TimeEntryResponse> CreateAsync(TimeEntryRequest request)
        {
            ValidateHours(request.Hours);

            var employees = _context.Database.GetCollection<Employee>("employees");

            var employee = await employees.Find(e => e.Id == request.EmployeeId).FirstOrDefaultAsync();

            if(employee == null)
                throw new BusinessException("EMPLOYEE_NOT_FOUND", "Сотрудник не найден");

            var projects = _context.Database.GetCollection<Project>("projects");

            var project = await projects.Find(p => p.Id == request.ProjectId).FirstOrDefaultAsync();

            if (project == null)
                throw new BusinessException("PROJECT_NOT_FOUND", "Проект не найден.");

            await ValidateClosedPeriod(request.Date);

            ValidateProjectDate(project, request.Date);

            var rate = GetRateForDate(employee, request.Date);

            var existingHours = await GetDailyHours(request.EmployeeId, request.Date);

            var totalHours = existingHours + request.Hours;

            var isOvertime = totalHours > 12;

            var entry = new TimeEntry
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                Date = request.Date,
                Hours = request.Hours,
                Comment = request.Comment,
                IsOvertime = isOvertime,
                Version = 1
            };

            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            await timeEntries.InsertOneAsync(entry);

            return new TimeEntryResponse
            {
                Id = entry.Id,
                EmployeeId = employee.Id,
                EmployeeName =
            $"{employee.LastName} {employee.Name} {employee.MiddleName}",
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                Date = entry.Date,
                Hours = entry.Hours,
                Comment = entry.Comment,
                IsOvertime = entry.IsOvertime,
                Rate = rate.Value,
                Amount = Math.Round(entry.Hours * rate.Value, 2),
                Version = entry.Version
            };
        }

        public async Task<TimeEntryResponse> UpdateAsync(string id, TimeEntryRequest request)
        {
            ValidateHours(request.Hours);

            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            var entry = await timeEntries.Find(e => e.Id == id).FirstOrDefaultAsync();

            if (entry == null)
                throw new BusinessException("NOT_FOUND", "Запись табеля не найдена.");

            await ValidateClosedPeriod(request.Date);

            var employees = _context.Database.GetCollection<Employee>("employees");

            var employee = await employees.Find(e => e.Id == request.EmployeeId).FirstOrDefaultAsync();

            if (employee == null)
                throw new BusinessException("EMPLOYEE_NOT_FOUND", "Сотрудник не найден.");
            
            var projects = _context.Database.GetCollection<Project>("projects");

            var project = await projects.Find(p => p.Id == request.ProjectId).FirstOrDefaultAsync();

            if (project == null)
                throw new BusinessException( "PROJECT_NOT_FOUND", "Проект не найден.");
            
            ValidateProjectDate(project, request.Date);

            var rate = GetRateForDate(employee, request.Date);

            var existingHours = await GetDailyHours(request.EmployeeId, request.Date, id);

            var totalHours = existingHours + request.Hours;

            if (totalHours > 24)
                throw new BusinessException("DAILY_HOURS_LIMIT", "Количество часов не должно превышать 24.");
            
            var isOvertime = totalHours > 12;

            var filter = Builders<TimeEntry>.Filter.And(Builders<TimeEntry>.Filter.Eq(e => e.Id, id), Builders<TimeEntry>.Filter.Eq(e => e.Version, request.Version));

            var update = Builders<TimeEntry>.Update
                .Set(e => e.EmployeeId, request.EmployeeId)
                .Set(e => e.ProjectId, request.ProjectId)
                .Set(e => e.Date, request.Date)
                .Set(e => e.Hours, request.Hours)
                .Set(e => e.Comment, request.Comment)
                .Set(e => e.IsOvertime, isOvertime)
                .Inc(e => e.Version, 1);

            var result = await timeEntries.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                throw new BusinessException("CONCURRENCY_CONFLICT", "Запись была изменена другим пользователем. Обновите данные и попробуйте снова.");
            
            var updatedEntry = await timeEntries.Find(e => e.Id == id).FirstOrDefaultAsync();

            return new TimeEntryResponse
            {
                Id = updatedEntry.Id,
                EmployeeId = employee.Id,
                EmployeeName =
                    $"{employee.LastName} {employee.Name} {employee.MiddleName}",
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                Date = updatedEntry.Date,
                Hours = updatedEntry.Hours,
                Comment = updatedEntry.Comment,
                IsOvertime = updatedEntry.IsOvertime,
                Rate = rate.Value,
                Amount = Math.Round(
                    updatedEntry.Hours * rate.Value,
                    2,
                    MidpointRounding.AwayFromZero),
                Version = updatedEntry.Version
            };
        }

        public async Task DeleteAsync(string id, int version)
        {
            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            var entry = await timeEntries.Find(e => e.Id == id).FirstOrDefaultAsync();

            if (entry == null)
                throw new BusinessException("NOT_FOUND", "Запись табеля не найдена.");

            await ValidateClosedPeriod(entry.Date);

            var filter = Builders<TimeEntry>.Filter.And(Builders<TimeEntry>.Filter.Eq(e => e.Id, id), Builders<TimeEntry>.Filter.Eq(e => e.Version, version));

            var result = await timeEntries.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
                throw new BusinessException("CONCURRENCY_CONFLICT", "Запись была изменена другим пользователем. Обновите данные и попробуйте снова.");
        }

        private Rate GetRateForDate(Employee employee,DateTime date)
        {
            var rate = employee.Rates.Where(r => r.From <= date).OrderByDescending(r => r.From).FirstOrDefault();

            if (rate == null)
                throw new BusinessException("RATE_NOT_FOUND", $"У сотрудника {employee.LastName} {employee.Name} " + $"нет ставки на {date:dd.MM.yyyy}.");

            return rate;
        }

        private void ValidateHours(decimal hour)
        {
            if (hour <= 0)
                throw new BusinessException("INVALID_HOURS", "Количество часов должно быть больше 0.");

            if (hour > 24)
                throw new BusinessException("INVALID_HOURS", "Количество часов не должно превышать 24.");

            if (hour % 0.5m != 0)
                throw new BusinessException("INVALID_HOURS", "Количество часов должно быть кратно 0,5.");
        }

        private async Task<Decimal> GetDailyHours(string id, DateTime date, string excludedEntryId = null)
        {
            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            var start = date.Date;
            var end = start.AddDays(1);

            var filter = Builders<TimeEntry>.Filter.And(Builders<TimeEntry>.Filter.Eq(x => x.EmployeeId, id), Builders<TimeEntry>.Filter.Gte(x => x.Date, start), Builders<TimeEntry>.Filter.Lt(x => x.Date, end));

            if (excludedEntryId != null)
                filter &= Builders<TimeEntry>.Filter.Ne(x => x.Id, excludedEntryId);

            var entries = await timeEntries.Find(filter).ToListAsync();

            return entries.Sum(x => x.Hours);
        }

        private async Task ValidateClosedPeriod(DateTime date)
        {
            var periods = _context.Database.GetCollection<ClosedPeriod>("closed_periods");

            var period = await periods.Find(p => p.Year == date.Year && p.Month == date.Month).FirstOrDefaultAsync();

            if (period != null)
                throw new BusinessException("PERIOD_CLOSED", "Период закрыт для редактирования.");
        }

        private void ValidateProjectDate(Project project, DateTime date)
        {
            if (date < project.ProjectStart)
                throw new BusinessException("PROJECT_DATE_INVALID", "Дата записи раньше даты начала проекта.");

            if (project.ProjectEnd.HasValue && date > project.ProjectEnd.Value)
                throw new BusinessException("PROJECT_DATE_INVALID", "Дата записи позже даты окончания проекта.");
        }

        private async Task ValidateDailyHours(string id, DateTime date, decimal hour, string excludedEntryId = null)
        {
            var existingHours = await GetDailyHours(id, date, excludedEntryId);

            var totalHours = existingHours + hour;

            if (totalHours > 24)
                throw new BusinessException("DAILY_HOURS_LIMIT", "Количество часов не должно превышать 24.");
        }
    }
}