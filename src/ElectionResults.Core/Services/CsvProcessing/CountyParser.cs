using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Infrastructure.CsvModels;
using ElectionResults.Core.Models;
using ElectionResults.Core.Storage;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ElectionResults.Core.Services.CsvProcessing
{
    public class CountyParser : ICsvParser
    {
        private readonly IOptions<AppConfig> _config;
        private readonly IElectionConfigurationSource _electionConfigurationSource;

        public CountyParser(IOptions<AppConfig> config, IElectionConfigurationSource electionConfigurationSource)
        {
            _config = config;
            _electionConfigurationSource = electionConfigurationSource;
        }

        public async Task<Result<ElectionResultsData>> Parse(ElectionResultsData electionResultsData, string csvContent,
            ElectionResultsFile file)
        {
            try
            {
                if (electionResultsData == null)
                    electionResultsData = new ElectionResultsData();
                var electionsConfigResponse = _electionConfigurationSource.GetElectionById(file.ElectionId);
                var electionsConfig = electionsConfigResponse.Value;
                var pollingStations = await CalculateVotesByCounty(csvContent, electionsConfig, file);
                var sumOfVotes = electionResultsData.Candidates.Sum(c => c.Votes);
                electionResultsData.Candidates = StatisticsAggregator.CalculatePercentagesForCandidates(electionResultsData.Candidates, sumOfVotes);
                var counties = pollingStations.GroupBy(g => g.County).Select(c => c.Key).OrderBy(c => c).ToList();
                foreach (var candidate in electionResultsData.Candidates)
                {
                    foreach (var county in counties)
                    {
                        var votesForCandidate = pollingStations.Where(p => p.County == county)
                            .Sum(p => p.Candidates.FirstOrDefault(c => c.Id == candidate.Id)?.Votes ?? 0);
                        candidate.Counties[county] = votesForCandidate;
                    }
                }

                return Result.Success(electionResultsData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<ElectionResultsData>(e.Message);
            }
        }

        private Election DeserializeElectionsConfig()
        {
            try
            {
                return JsonConvert.DeserializeObject<Election>(_config.Value.ElectionsConfig);
            }
            catch (Exception)
            {
                return Election.Default;
            }
        }

        protected virtual async Task<List<PollingStation>> CalculateVotesByCounty(string csvContent,
            Election election, ElectionResultsFile file)
        {
            TextReader sr = new StringReader(csvContent);
            var csvParser = new CsvParser(sr, CultureInfo.CurrentCulture);
            var pollingStations = new List<PollingStation>();
            var headers = (await csvParser.ReadAsync()).ToList();
            do
            {
                var rowValues = await csvParser.ReadAsync();
                if (rowValues == null)
                    break;
                var pollingStation = new PollingStation();
                pollingStation.County = rowValues[1].StartsWith("SECTOR") ? "BUCUREȘTI" : rowValues[1];
                pollingStation.Name = rowValues[4];
                foreach (var candidate in election.Candidates)
                {
                    var votes = int.Parse(rowValues[headers.IndexOf(file.Prefix + candidate.CsvId)]);
                    pollingStation.Candidates.Add(new CandidateConfig
                    {
                        Id = candidate.CsvId,
                        Votes = votes
                    });
                    pollingStation.TotalVotes += votes;
                }
                pollingStations.Add(pollingStation);
            } while (true);

            return pollingStations;
        }
    }
}