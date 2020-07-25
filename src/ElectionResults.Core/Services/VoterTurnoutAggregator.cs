using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Models;
using Newtonsoft.Json;

namespace ElectionResults.Core.Services
{
    public class VoterTurnoutAggregator : IVoterTurnoutAggregator
    {
        public virtual async Task<Result<VoterTurnout>> GetVoterTurnoutFromBEC(ElectionResultsFile turnoutJson)
        {
            try
            {
                var httpClient = new HttpClient();
                var voterTurnout = await GetVoterTurnoutByUrl(httpClient, turnoutJson.URL);
                voterTurnout.ElectionId = turnoutJson.ElectionId;
                return Result.Success(voterTurnout);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<VoterTurnout>(e.Message);
            }
        }

        private static async Task<VoterTurnout> GetVoterTurnoutByUrl(HttpClient httpClient, string url)
        {
            var json = await httpClient.GetStringAsync(url);
            var voterTurnoutResponse = JsonConvert.DeserializeObject<VoterTurnoutResponce>(json);
            var permanentLists = voterTurnoutResponse.Counties.Sum(c => c.VotersOnPermanentLists);
            var specialLists = voterTurnoutResponse.Counties.Sum(c => c.VotersOnSpecialLists);
            var mobileVotes = voterTurnoutResponse.Counties.Sum(c => c.MobileVotes);
            var mailDiasporaVotes = voterTurnoutResponse.Precinct.Sum(p => p.MailDiasporaVotes);

            var diasporaVoters = voterTurnoutResponse.Precinct.Sum(c => c.VotersOnSpecialLists);
            var enlistedVoters = voterTurnoutResponse.Counties.Sum(c => c.InitialCount);
            var totalVoters = permanentLists + mobileVotes + specialLists + (mailDiasporaVotes - diasporaVoters);
            var voterTurnoutPercentage = totalVoters / (decimal)enlistedVoters;

            var voterTurnout = new VoterTurnout
            {
                EnlistedVoters = enlistedVoters,
                TurnoutPercentage = Math.Round(voterTurnoutPercentage * 100, 2),
                TotalNationalVotes = totalVoters,
                TotalDiasporaVotes = mailDiasporaVotes,
                PermanentLists = permanentLists,
                AdditionalLists = specialLists,
                MobileVotes = mobileVotes,
                DiasporaWithoutMailVotes = diasporaVoters 
            };
            return voterTurnout;
        }

        public async Task<Result<VoteMonitoringStats>> GetVoteMonitoringStats(ElectionResultsFile monitoringJson)
        {
            var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync(monitoringJson.URL);
            var response = JsonConvert.DeserializeObject<List<MonitoringInfo>>(json);
            return Result.Success(new VoteMonitoringStats
            {
                Statistics = response,
                ElectionId = monitoringJson.ElectionId
            });
        }
    }
}