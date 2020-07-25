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

namespace ElectionResults.Core.Services.CsvProcessing
{
    public class CandidatesResultsParser : ICsvParser
    {
        private readonly IOptions<AppConfig> _config;
        private readonly IElectionConfigurationSource _electionConfigurationSource;

        public CandidatesResultsParser(IOptions<AppConfig> config, IElectionConfigurationSource electionConfigurationSource)
        {
            _config = config;
            _electionConfigurationSource = electionConfigurationSource;
        }

        public async Task<Result<ElectionResultsData>> Parse(ElectionResultsData electionResultsData, string csvContent, ElectionResultsFile file)
        {
            if (electionResultsData == null)
                electionResultsData = new ElectionResultsData();
            var electionsConfigResponse = _electionConfigurationSource.GetElectionById(file.ElectionId);
            if (electionsConfigResponse.IsFailure)
            {
                return Result.Failure<ElectionResultsData>(electionsConfigResponse.Error);
            }

            var electionsConfig = electionsConfigResponse.Value;

            electionResultsData.Candidates = electionsConfig.Candidates.Select(c => new CandidateConfig
            {
                Id = c.CsvId,
                ImageUrl = c.ImageUrl,
                Name = c.Name
            }).ToList();
            
            await PopulateCandidatesListWithVotes(csvContent, electionResultsData, file);
            var sumOfVotes = electionResultsData.Candidates.Sum(c => c.Votes);
            electionResultsData.Candidates = StatisticsAggregator.CalculatePercentagesForCandidates(electionResultsData.Candidates, sumOfVotes);

            return Result.Success(electionResultsData);
        }


        protected virtual async Task PopulateCandidatesListWithVotes(string csvContent,
            ElectionResultsData electionResultsData, ElectionResultsFile file)
        {
            try
            {
                TextReader sr = new StringReader(csvContent);
                var csvParser = new CsvParser(sr, CultureInfo.CurrentCulture);
                var headers = (await csvParser.ReadAsync()).ToList();
                var totalCanceledVotes = 0;
                do
                {
                    var rowValues = await csvParser.ReadAsync();
                    if (rowValues == null)
                        break;
                    var canceledVotesColumn = "d";
                    if (file.ResultsSource == "mail")
                    {
                        canceledVotesColumn = "d2";
                    }
                    foreach (var candidate in electionResultsData.Candidates)
                    {
                        var votes = int.Parse(rowValues[headers.IndexOf(file.Prefix + candidate.Id)]);
                        candidate.Votes += votes;
                    }
                    var canceledVotes = int.Parse(rowValues[headers.IndexOf(canceledVotesColumn)]);
                    totalCanceledVotes += canceledVotes;
                } while (true);

                if (file.ElectionId == Consts.SecondElectionRound)
                {
                    Console.WriteLine($"Canceled votes for {file.URL} is {totalCanceledVotes}");
                }
                electionResultsData.CanceledVotes = totalCanceledVotes;
            }
            catch (Exception e)
            {
                Log.LogError(e, $"Couldn't populate list with votes for file {file.URL} and election {file.ElectionId}");
            }
        }
    }
}
