using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure.CsvModels;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;

namespace ElectionResults.Tests.StatisticsAggregatorTests.Fakes
{
    public class FakeCandidatesParser : ICsvParser
    {
        public Task<Result<ElectionResultsData>> Parse(ElectionResultsData electionResultsData, string csvContent,
            ElectionResultsFile file)
        {
            WasInvoked = true;
            electionResultsData.Candidates = new List<CandidateConfig>();
            return Task.FromResult(Result.Success(electionResultsData));
        }

        public bool WasInvoked { get; set; }
    }
}