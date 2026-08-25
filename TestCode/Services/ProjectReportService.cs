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

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var match = Builders<TimeEntry>.Filter.And(Builders<TimeEntry>.Filter.Gte(x => x.Date, startDate), Builders<TimeEntry>.Filter.Lt(x => x.Date, endDate));

            var pipeline = timeEntries.Aggregate().Match(match).Lookup("employees", "employeeId", "_id", "employee").Unwind("employee");
            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set",new BsonDocument{
            { "applicableRates", new BsonDocument("$filter", new BsonDocument{
                        { "input", "$employee.rates" },
                        { "as", "rate" },
                        {"cond", new BsonDocument("$lte", new BsonArray{
                                    "$$rate.from", "$date" })
                        }
                   })
            }
            }));

            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument{
            {"rate", new BsonDocument("$arrayElemAt", new BsonArray{
                        new BsonDocument( "$sortArray", new BsonDocument{
                                {"input", "$applicableRates"},
                                {"sortBy",new BsonDocument("from", -1)}
                        }), 0 })
            }
            }));

            pipeline = pipeline.Lookup("projects", "projectId", "_id","project").Unwind("project");
            pipeline = pipeline.AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument{
            { "amount", new BsonDocument("$multiply", new BsonArray{
                        "$hours","$rate.value"})
            }
            }));

            pipeline = pipeline.Group(new BsonDocument{
                {"_id", "$projectId"},
                {"projectCode", new BsonDocument("$first", "$project.projectCode")},
                {"projectName", new BsonDocument("$first", "$project.projectName")},
                {"budget", new BsonDocument("$first", "$project.budget")},
                {"hours", new BsonDocument("$sum",  "$hours")},
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
