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

        public async Task<TimeEntryListResponse> GetAsync(TimeEntryQuery query)
        {
            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            var filters = new List<FilterDefinition<TimeEntry>>();

            var startDate = new DateTime(query.Year, query.Month, 1);
            var endDate = startDate.AddMonths(1);

            filters.Add(
                Builders<TimeEntry>.Filter.Gte(
                    x => x.Date,
                    startDate));

            filters.Add(
                Builders<TimeEntry>.Filter.Lt(
                    x => x.Date,
                    endDate));

            if (!string.IsNullOrEmpty(query.EmployeeId))
            {
                filters.Add(
                    Builders<TimeEntry>.Filter.Eq(
                        x => x.EmployeeId,
                        query.EmployeeId));
            }

            if (!string.IsNullOrEmpty(query.ProjectId))
            {
                filters.Add(
                    Builders<TimeEntry>.Filter.Eq(
                        x => x.ProjectId,
                        query.ProjectId));
            }

            var filter = Builders<TimeEntry>.Filter.And(filters);

            var totalCount = await timeEntries.CountDocumentsAsync(filter);

            var entries = await timeEntries.Find(filter).SortByDescending(x => x.Date).Skip((query.Page - 1) * query.PageSize).Limit(query.PageSize).ToListAsync();

            var items = new List<TimeEntryResponse>();

            foreach (var entry in entries)
            {
                var employee = await _context.Database.GetCollection<Employee>("employees").Find(x => x.Id == entry.EmployeeId).FirstOrDefaultAsync();

                var project = await _context.Database.GetCollection<Project>("projects").Find(x => x.Id == entry.ProjectId).FirstOrDefaultAsync();

                var rate = GetRateForDate(employee, entry.Date);

                items.Add(new TimeEntryResponse
                {
                    Id = entry.Id,
                    EmployeeId = entry.EmployeeId,
                    EmployeeName =
                        $"{employee.LastName} {employee.Name} {employee.MiddleName}",

                    ProjectId = entry.ProjectId,
                    ProjectCode = project.ProjectCode,

                    Date = entry.Date,
                    Hours = entry.Hours,
                    Comment = entry.Comment,
                    IsOvertime = entry.IsOvertime,

                    Rate = rate.Value,

                    Amount = Math.Round(
                        entry.Hours * rate.Value,
                        2,
                        MidpointRounding.AwayFromZero),

                    Version = entry.Version
                });
            }

            return new TimeEntryListResponse
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = (int)totalCount,
                TotalPage = (int)Math.Ceiling(
                    (double)totalCount / query.PageSize)
            };
        }

        public async Task<List<EmployeeResponse>> GetEmployeesAsync()
        {
            var employees = _context.Database.GetCollection<Employee>("employees");

            var result = await employees.Find(FilterDefinition<Employee>.Empty).ToListAsync();

            return result.Select(e => new EmployeeResponse
                {
                    Id = e.Id,
                    FullName = $"{e.LastName} {e.Name} {e.MiddleName}",
                    Department = e.Department
                }).ToList();
        }

        public async Task<List<ProjectResponse>> GetProjectsAsync()
        {
            var projects = _context.Database.GetCollection<Project>("projects");

            var result = await projects.Find(FilterDefinition<Project>.Empty).ToListAsync();

            return result.Select(p => new ProjectResponse
                {
                    Id = p.Id,
                    ProjectCode = p.ProjectCode,
                    Name = p.ProjectName
                }).ToList();
        }

        public Rate GetRateForDate(Employee employee,DateTime date)
        {
            var rate = employee.Rates.Where(r => r.From <= date).OrderByDescending(r => r.From).FirstOrDefault();

            if (rate == null)
                throw new BusinessException("RATE_NOT_FOUND", $"У сотрудника {employee.LastName} {employee.Name} " + $"нет ставки на {date:dd.MM.yyyy}.");

            return rate;
        }

        public void ValidateHours(decimal hour)
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

        public void ValidateProjectDate(Project project, DateTime date)
        {
            if (date < project.ProjectStart)
                throw new BusinessException("PROJECT_DATE_INVALID", "Дата записи раньше даты начала проекта.");

            if (project.ProjectEnd.HasValue && date > project.ProjectEnd.Value)
                throw new BusinessException("PROJECT_DATE_INVALID", "Дата записи позже даты окончания проекта.");
        }
    }
}