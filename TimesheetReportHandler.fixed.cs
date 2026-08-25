// Учебный проект. Обработчик отчёта "стоимость трудозатрат по проектам за месяц".
// Код рабочий: на небольшой базе отчёт строится и цифры выглядят правдоподобно.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;

namespace Demo.Api.Queries.Reports
{
    public class ProjectReportRow
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public double Hours { get; set; }
        public double Amount { get; set; }
        public double Budget { get; set; }
        public double Percent { get; set; }
        public bool Overspent { get; set; }
    }

    public class GetProjectReportQuery : IRequest<List<ProjectReportRow>>
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class TimesheetReportHandler : IRequestHandler<GetProjectReportQuery, List<ProjectReportRow>>
    {
        private readonly IMongoDatabase _db;

        public TimesheetReportHandler(IMongoDatabase db)
        {
            _db = db;
        }

        public async Task<List<ProjectReportRow>> Handle(
            GetProjectReportQuery request,
            CancellationToken token)
        {
            // ИСПРАВЛЕНО:
            // Раньше здесь загружались ВСЕ записи из time_entries:
            //
            // var entries = await _db.GetCollection<TimeEntry>("time_entries")
            //     .Find(FilterDefinition<TimeEntry>.Empty)
            //     .ToListAsync();
            //
            // После этого нужный месяц выбирался уже в памяти через Where.
            // При нескольких миллионах записей это приводит к большому
            // потреблению памяти и лишней передаче данных из MongoDB.
            //
            // Теперь в MongoDB сразу выбираются только записи нужного месяца.
            var monthStart = new DateTime(request.Year, request.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var entries = await _db.GetCollection<TimeEntry>("time_entries")
                .Find(e => e.Date >= monthStart && e.Date < nextMonthStart)
                .ToListAsync(token);


            // ИСПРАВЛЕНО:
            // Раньше запрос к employees выполнялся внутри foreach:
            //
            // var employee = _db.GetCollection<Employee>("employees")
            //     .Find(e => e.Id == entry.EmployeeId)
            //     .FirstOrDefaultAsync().Result;
            //
            // В результате для каждой записи табеля выполнялся отдельный
            // запрос к MongoDB (N+1 запросов).
            //
            // Сначала получаем уникальные EmployeeId из уже отфильтрованных
            // записей, затем одним запросом загружаем всех необходимых сотрудников.
            var employeeIds = entries
                .Select(e => e.EmployeeId)
                .Distinct()
                .ToList();

            var employees = await _db.GetCollection<Employee>("employees")
                .Find(e => employeeIds.Contains(e.Id))
                .ToListAsync(token);

            var employeesById = employees
                .ToDictionary(e => e.Id);


            // ИСПРАВЛЕНО:
            // Раньше запрос к projects выполнялся внутри foreach при первом
            // появлении каждого ProjectId:
            //
            // var project = await _db.GetCollection<Project>("projects")
            //     .Find(p => p.Id == entry.ProjectId)
            //     .FirstOrDefaultAsync();
            //
            // Это создавало отдельный запрос к MongoDB для каждого проекта.
            //
            // Теперь получаем все необходимые проекты одним запросом.
            var projectIds = entries
                .Select(e => e.ProjectId)
                .Distinct()
                .ToList();

            var projects = await _db.GetCollection<Project>("projects")
                .Find(p => projectIds.Contains(p.Id))
                .ToListAsync(token);

            var projectsById = projects
                .ToDictionary(p => p.Id);


            var rows = new Dictionary<string, ProjectReportRow>();

            foreach (var entry in entries)
            {
                // Сотрудник уже загружен одним общим запросом выше.
                var employee = employeesById[entry.EmployeeId];

                var rate = employee.Rates.FirstOrDefault().Value;

                var amount = Math.Round(entry.Hours * rate, 2);

                if (!rows.ContainsKey(entry.ProjectId))
                {
                    // Проект уже загружен одним общим запросом выше.
                    var project = projectsById[entry.ProjectId];

                    rows[entry.ProjectId] = new ProjectReportRow
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        Budget = project.Budget
                    };
                }

                rows[entry.ProjectId].Hours += entry.Hours;
                rows[entry.ProjectId].Amount += amount;
            }

            foreach (var row in rows.Values)
            {
                row.Percent = Math.Round(row.Amount / row.Budget * 100, 2);
                row.Overspent = row.Percent > 100;
            }

            return rows.Values
                .OrderBy(r => r.ProjectName)
                .ToList();
        }
    }

    // --- сущности (упрощённо) ---

    public class TimeEntry
    {
        public string Id { get; set; }
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public string Comment { get; set; }
    }

    public class Employee
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Rate> Rates { get; set; }
    }

    public class Rate
    {
        public DateTime From { get; set; }
        public double Value { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Budget { get; set; }
    }
}