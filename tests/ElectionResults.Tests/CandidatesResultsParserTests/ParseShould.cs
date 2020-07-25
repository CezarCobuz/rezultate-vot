using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Infrastructure.CsvModels;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ElectionResults.Tests.CandidatesResultsParserTests
{
    public class ParseShould
    {
        [Fact]
        public async Task not_return_null_list_of_candidates()
        {
            var source = Substitute.For<IElectionConfigurationSource>();
            source.GetElectionById("").ReturnsForAnyArgs(e => Result.Success(new Election
            {
                Candidates = new List<CandidateConfig>()
            }));
            var candidatesResultsParser = new TestableCandidatesResultsParser(null, source);
            candidatesResultsParser.ParsedCandidates = new List<CandidateConfig>();

            var result = await candidatesResultsParser.Parse(ElectionResultsData.Default, "", new ElectionResultsFile());

            result.Value.Candidates.Should().NotBeNull();
        }

        [Theory]
        [InlineData(60, 60, 20, 20, 20, 20)]
        [InlineData(30, 33.33, 30, 33.33, 30, 33.33)]
        [InlineData(62, 62, 37, 37, 1, 1)]
        public void set_percentages_for_each_candidate(int c1Votes, decimal c1Percentage,
            int c2Votes, decimal c2Percentage,
            int c3Votes, decimal c3Percentage)
        {
            var candidatesResultsParser = new TestableCandidatesResultsParser(null)
            {
                ParsedCandidates = CreateListOfCandidatesWithVotes(c1Votes, c2Votes, c3Votes)
            };
            var electionResultsData = new ElectionResultsData { Candidates = CreateListOfCandidatesWithVotes(c1Votes, c2Votes, c3Votes) };
            var sumOfVotes = candidatesResultsParser.ParsedCandidates.Sum(c => c.Votes);
            electionResultsData.Candidates = StatisticsAggregator.CalculatePercentagesForCandidates(electionResultsData.Candidates, sumOfVotes);

            electionResultsData.Candidates[0].Percentage.Should().Be(c1Percentage);
            electionResultsData.Candidates[1].Percentage.Should().Be(c2Percentage);
            electionResultsData.Candidates[2].Percentage.Should().Be(c3Percentage);
        }

        private static List<CandidateConfig> CreateListOfCandidatesWithVotes(int c1Votes, int c2Votes, int c3Votes)
        {
            return new List<CandidateConfig>
            {
                new CandidateConfig
                {
                    Id = "1",
                    Name = "Candidate1",
                    Votes = c1Votes
                },
                new CandidateConfig
                {
                    Id = "2",
                    Name = "Candidate2",
                    Votes = c2Votes
                },
                new CandidateConfig
                {
                    Id = "3",
                    Name = "Candidate3",
                    Votes = c3Votes
                }
            };
        }
    }
}
