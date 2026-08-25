using MongoDB.Bson;
using MongoDB.Driver;
using TestCode.Data;
using TestCode.DTOs.Reports;
using TestCode.Models;

namespace TestCode.Services
{
    public class ProjectReportService
    {
        private readonly MongoDbContext _context;

        public ProjectReportService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<ProjectReportResponseList> GetAsync(int year, int month)
        {
            var timeEntries = _context.Database.GetCollection<TimeEntry>("time_entries");

            if (month < 1 || month > 12)
                throw new BusinessException("INVALID_MONTH", "Месяц должен быть от 1 до 12.");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var match = Builders<TimeEntry>.Filter.And(Builders<TimeEntry>.Filter.Gte(x => x.Date, startDate), Builders<TimeEntry>.Filter.Lt(x => x.Date, endDate));

            var pipeline = timeEntries.Aggregate().Match(match).Lookup("employees", "EmployeeId", "_id", "employee").Unwind("employee");

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set",new BsonDocument{
            { "applicableRates", new BsonDocument("$filter", new BsonDocument{
                        { "input", "$employee.Rates" },
                        { "as", "rate" },
                        {"cond", new BsonDocument("$lte", new BsonArray{
                                    "$$rate.From", "$Date" })
                        }
                   })
            }
            }));

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument{
            {"rate", new BsonDocument("$arrayElemAt", new BsonArray{
                        new BsonDocument( "$sortArray", new BsonDocument{
                                {"input", "$applicableRates"},
                                {"sortBy",new BsonDocument("From", -1)}
                        }), 0 })
            }
            }));

            pipeline = pipeline.Lookup("projects", "ProjectId", "_id", "project").Unwind("project");

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument{
            { "amount", new BsonDocument("$multiply", new BsonArray{
                        "$Hours","$rate.Value"})
            }
            }));

            pipeline = pipeline.Group(new BsonDocument{
                {"_id", "$ProjectId"},
                {"projectCode", new BsonDocument("$first", "$project.ProjectCode")},
                {"projectName", new BsonDocument("$first", "$project.ProjectName")},
                {"budget", new BsonDocument("$first", "$project.Budget")},
                {"hours", new BsonDocument("$sum",  "$Hours")},
                {"amount", new BsonDocument("$sum", "$amount")}
            });

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument{
                {"percent",new BsonDocument("$cond", new BsonArray{
                        new BsonDocument("$eq",new BsonArray{
                                "$budget", 0 }), 0, new BsonDocument("$multiply",new BsonArray{
                                new BsonDocument("$divide", new BsonArray{"$amount", "$budget" }), 100
                                })
                        })
                }
            }));

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set",new BsonDocument{
                {"overspent", new BsonDocument("$gt", new BsonArray{
                        "$percent",100})
                }, {"risk", new BsonDocument("$gt",new BsonArray{
                    "$percent", 80})
                }
            }));

            pipeline = pipeline.Sort(new BsonDocument("projectCode", 1));

            var result = await pipeline.ToListAsync();

            var items = result.Select(x => new ProjectReportResponse
            {
                ProjectId = x["_id"].AsString,
                ProjectCode = x["projectCode"].AsString,
                ProjectName = x["projectName"].AsString,
                Hours = x["hours"].ToDecimal(),
                Amount = x["amount"].ToDecimal(),
                Budget = x["budget"].ToDecimal(),
                Percent = Math.Round(
        x["percent"].ToDecimal(),
        2),
                Overspent = x["overspent"].AsBoolean,
                Risk = x["risk"].AsBoolean
            }).ToList();

            return new ProjectReportResponseList
            {
                Items = items,
                TotalHours = items.Sum(x => x.Hours),
                TotalAmount = items.Sum(x => x.Amount)
            };
        }
    }
}
