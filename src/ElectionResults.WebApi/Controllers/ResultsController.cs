using System;
using System.Threading.Tasks;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services;
using ElectionResults.Core.Services.CsvDownload;
using ElectionResults.Core.Storage;
using LazyCache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ElectionResults.WebApi.Controllers
{
    [Route("api/results")]
    public class ResultsController : Controller
    {
        private readonly IResultsAggregator _resultsAggregator;
        private readonly IOptionsSnapshot<AppConfig> _config;
        private readonly IAppCache _appCache;

        public ResultsController(IResultsAggregator resultsAggregator,
            IOptionsSnapshot<AppConfig> config,
            IAppCache appCache)
        {
            _resultsAggregator = resultsAggregator;
            _config = config;
            _appCache = appCache;
        }

        [HttpGet("")]
        public async Task<ActionResult<LiveResultsResponse>> GetResults([FromQuery] string electionId = Consts.FirstElectionRound, string source = null, string county = null, FileType fileType = FileType.Results)
        {
            try
            {
                var resultsQuery = new ResultsQuery(fileType, source, county, electionId);
                var key = resultsQuery.ToString();
                var result = await _appCache.GetOrAddAsync(
                    key, () => _resultsAggregator.GetElectionResults(resultsQuery),
                    DateTimeOffset.Now.AddSeconds(_config.Value.IntervalInSeconds));
                if (result.IsFailure)
                {
                    _appCache.Remove(key);
                    Log.LogWarning(result.Error);
                    return BadRequest(result);
                }
                var voteCountStatisticsResult = await _appCache.GetOrAddAsync(
                    Consts.ResultsCountKey + electionId, () => _resultsAggregator.GetVoteCountStatistics(electionId),
                    DateTimeOffset.Now.AddSeconds(_config.Value.IntervalInSeconds));
                if (voteCountStatisticsResult.IsSuccess)
                {
                    result.Value.TotalCountedVotes = voteCountStatisticsResult.Value.TotalCountedVotes;
                    result.Value.PercentageCounted = voteCountStatisticsResult.Value.Percentage;
                }

                if (electionId == Consts.SecondElectionRound && string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(county))
                {
                    result.Value.PercentageCounted = 100;
                    result.Value.CanceledVotes = 182648;
                    if (voteCountStatisticsResult.IsSuccess)
                        result.Value.TotalCountedVotes = voteCountStatisticsResult.Value.TotalCountedVotes - result.Value.CanceledVotes;
                    result.Value.Candidates[0].Votes = 6509135;
                    result.Value.Candidates[0].Percentage = (decimal)66.09;
                    result.Value.Candidates[1].Votes = 3339922;
                    result.Value.Candidates[1].Percentage = (decimal)33.91;
                }
                return result.Value;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while retrieving results");
                throw;
            }
        }

        [HttpGet("voter-turnout")]
        public async Task<ActionResult<VoterTurnout>> GetVoterTurnout([FromQuery] string electionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(electionId))
                    electionId = Consts.FirstElectionRound;
                var key = Consts.VoteTurnoutKey + electionId;
                var result = await _appCache.GetOrAddAsync(
                    key, () => _resultsAggregator.GetVoterTurnout(electionId),
                    DateTimeOffset.Now.AddSeconds(_config.Value.TurnoutCacheIntervalInSeconds));
                if (result.IsFailure)
                {
                    _appCache.Remove(key);
                    Log.LogWarning(result.Error);
                    return BadRequest(result.Error);
                }
                return result.Value;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while retrieving voter turnout stats");
                return StatusCode(500, e);
            }
        }

        [HttpGet("monitoring")]
        public async Task<ActionResult<VoteMonitoringStats>> GetVoteMonitoringStats([FromQuery] string electionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(electionId))
                    electionId = Consts.FirstElectionRound;
                var key = Consts.VoteMonitoringKey + electionId;
                var result = await _appCache.GetOrAddAsync(
                    key, () => _resultsAggregator.GetVoteMonitoringStats(electionId),
                    DateTimeOffset.Now.AddMinutes(5));
                if (result.IsFailure)
                {
                    _appCache.Remove(key);
                    Log.LogWarning(result.Error);
                    return BadRequest(result.Error);
                }
                return result.Value;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while retrieving vote monitoring stats");
                return StatusCode(500, e);
            }
        }
    }
}
