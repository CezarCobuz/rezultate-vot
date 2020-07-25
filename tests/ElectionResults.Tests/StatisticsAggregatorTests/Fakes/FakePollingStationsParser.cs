using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;

namespace ElectionResults.Tests.StatisticsAggregatorTests.Fakes
{
    public class FakePollingStationsParser : ICsvParser
    {
        public Task<Result<ElectionResultsData>> Parse(ElectionResultsData electionResultsData, string csvContent,
            ElectionResultsFile file)
        {
            return Task.FromResult(Result.Success(electionResultsData));
        }
    }
}