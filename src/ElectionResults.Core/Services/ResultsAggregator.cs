using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;
using ElectionResults.Core.Storage;
using Newtonsoft.Json;

namespace ElectionResults.Core.Services
{
    public class ResultsAggregator : IResultsAggregator
    {
        private readonly IResultsRepository _resultsRepository;
        private readonly IElectionConfigurationSource _electionConfigurationSource;

        public ResultsAggregator(IResultsRepository resultsRepository, IElectionConfigurationSource electionConfigurationSource)
        {
            _resultsRepository = resultsRepository;
            _electionConfigurationSource = electionConfigurationSource;
        }

        private List<CandidateModel> ConvertCandidates(ElectionResultsData electionResultsData)
        {
            if (electionResultsData?.Candidates == null)
                return CreateListWithEmptyCandidates();
            electionResultsData.Candidates = StatisticsAggregator.CalculatePercentagesForCandidates(
                electionResultsData.Candidates,
                electionResultsData.Candidates.Sum(c => c.Votes));
            var candidates = electionResultsData.Candidates.Select(c => new CandidateModel
            {
                Id = c.Id,
                ImageUrl = c.ImageUrl,
                Name = c.Name,
                Percentage = c.Percentage,
                Votes = c.Votes
            }).ToList();
            return candidates;
        }

        private List<CandidateModel> CreateListWithEmptyCandidates()
        {
            return new List<CandidateModel>();
        }

        public async Task<Result<VoteMonitoringStats>> GetVoteMonitoringStats(string electionId)
        {
            var result = await _resultsRepository.Get(electionId, Consts.VoteMonitoringKey, FileType.VoteMonitoring.ConvertEnumToString());
            if (result.IsSuccess)
            {
                var voteMonitoringStats = JsonConvert.DeserializeObject<VoteMonitoringStats>(result.Value.StatisticsJson);
                return Result.Success(voteMonitoringStats);
            }
            return Result.Failure<VoteMonitoringStats>("Failed to retrieve vote monitoring stats");
        }

        public async Task<Result<VoterTurnout>> GetVoterTurnout(string electionId)
        {
            var result = await _resultsRepository.Get(electionId, Consts.VoteTurnoutKey, FileType.VoterTurnout.ConvertEnumToString());
            if (result.IsSuccess)
            {
                var voterTurnout = JsonConvert.DeserializeObject<VoterTurnout>(result.Value.StatisticsJson);
                return Result.Success(voterTurnout);
            }

            return Result.Failure<VoterTurnout>("Failed to retrieve voter turnout");
        }

        public async Task<Result<LiveResultsResponse>> GetElectionResults(ResultsQuery resultsQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resultsQuery.Source))
                {
                    var data = await CombineAllSources(resultsQuery);
                    if (data?.Candidates == null)
                    {
                        Log.LogWarning($"No data found for {resultsQuery}");
                        return Result.Failure<LiveResultsResponse>($"No data found for {resultsQuery}");
                    }
                    return Result.Success(CreateLiveResultsResponse(data));
                }

                var response = await _resultsRepository.Get(resultsQuery.ElectionId, resultsQuery.Source, FileType.Results.ConvertEnumToString());
                if (response.IsSuccess)
                {
                    var electionResultsData = JsonConvert.DeserializeObject<ElectionResultsData>(response.Value.StatisticsJson);
                    if (string.IsNullOrWhiteSpace(resultsQuery.County))
                        return Result.Success(CreateLiveResultsResponse(electionResultsData));
                    foreach (var candidate in electionResultsData.Candidates)
                    {
                        candidate.Votes = candidate.Counties[resultsQuery.County];
                    }
                    return Result.Success(CreateLiveResultsResponse(electionResultsData));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return Result.Success(new LiveResultsResponse());
        }

        public async Task<Result<VoteCountStats>> GetVoteCountStatistics(string electionId)
        {
            var electionReport = _electionConfigurationSource.GetElectionById(electionId);
            if (electionReport.IsFailure)
            {
                return Result.Failure<VoteCountStats>(electionReport.Error);
            }
            var election = electionReport.Value;
            if (election.ElectionId == Consts.FirstElectionRound)
            {
                var stats = new VoteCountStats
                {
                    Percentage = 100,
                    TotalCountedVotes = 9216515
                };
                return Result.Success(stats);
            }
            else
            {
                var stats = new VoteCountStats
                {
                    Percentage = 100,
                    TotalCountedVotes = 10031762
                };
                return Result.Success(stats);
            }
            var result = await GetElectionResults(new ResultsQuery
            {
                ElectionId = election.ElectionId
            });
            if (!result.IsSuccess) return Result.Failure<VoteCountStats>(result.Error);

            var voteCountStats = new VoteCountStats();
            voteCountStats.TotalCountedVotes = result.Value.Candidates.Sum(c => c.Votes);
            var voterTurnout = await GetVoterTurnout(election.ElectionId);
            if (voterTurnout.IsFailure)
            {
                Log.LogWarning($"Failed to retrieve voter turnout when gathering statistics: {voterTurnout.Error}");
                return Result.Failure<VoteCountStats>(voterTurnout.Error);
            }

            double allCountedVotes = voteCountStats.TotalCountedVotes + result.Value.CanceledVotes;
            voteCountStats.Percentage = Math.Round(allCountedVotes / voterTurnout.Value.TotalNationalVotes * 100, 2);
            if (voteCountStats.TotalCountedVotes == 0)
            {
                Log.LogWarning($"Total counted votes is 0");
                Log.LogWarning($"Percentage: {voteCountStats.Percentage}");
                return Result.Failure<VoteCountStats>("Vote turnout is 0");
            }

            return Result.Success(voteCountStats);

        }


        private async Task<ElectionResultsData> CombineAllSources(ResultsQuery resultsQuery)
        {
            var electionGetResult = _electionConfigurationSource.GetElectionById(resultsQuery.ElectionId);
            if (electionGetResult.IsFailure)
            {
                Log.LogWarning($"Could not retrieve election with id {resultsQuery.ElectionId} due to error {electionGetResult.Error}");
                return ElectionResultsData.Default;
            }
            var availableSources = electionGetResult.Value.Files.Where(f => f.Active && f.FileType == FileType.Results).Select(f => f.Name).ToList();
            var data = new ElectionResultsData();
            if (availableSources.Count == 0)
            {
                return CreateEmptyElectionResultsData(resultsQuery, data);
            }
            foreach (var source in availableSources)
            {
                var resultsResponse =
                    await _resultsRepository.Get(resultsQuery.ElectionId, source, FileType.Results.ConvertEnumToString());
                if (resultsResponse.IsSuccess)
                {
                    var deserializedData =
                        JsonConvert.DeserializeObject<ElectionResultsData>(resultsResponse.Value.StatisticsJson);
                    data = StatisticsAggregator.CombineResults(data, deserializedData);
                }
            }

            if (data?.Candidates?.Count == 0)
            {
                return CreateEmptyElectionResultsData(resultsQuery, data);
            }
            return data;
        }

        private ElectionResultsData CreateEmptyElectionResultsData(ResultsQuery resultsQuery, ElectionResultsData data)
        {
            var currentElectionResult = _electionConfigurationSource.GetElectionById(resultsQuery.ElectionId);
            if (currentElectionResult.IsSuccess)
            {
                data.Candidates = currentElectionResult.Value.Candidates;
                return data;
            }
            else
            {
                Log.LogWarning(currentElectionResult.Error);
                return ElectionResultsData.Default;
            }
        }

        private LiveResultsResponse CreateLiveResultsResponse(ElectionResultsData electionResultsData)
        {
            var candidates = ConvertCandidates(electionResultsData);
            var counties =
                electionResultsData?.Candidates?.FirstOrDefault()?.Counties.Select(c => new County
                {
                    Label = c.Key,
                    Id = "national",
                    CountyName = c.Key
                }).ToList();
            var liveResultsResponse = new LiveResultsResponse
            {
                Candidates = candidates,
                Counties = counties ?? new List<County>(),
                CanceledVotes = electionResultsData.CanceledVotes
            };
            return liveResultsResponse;
        }
    }
}
