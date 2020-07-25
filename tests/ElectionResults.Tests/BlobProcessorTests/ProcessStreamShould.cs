using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;
using ElectionResults.Core.Storage;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ElectionResults.Tests.BlobProcessorTests
{
    public class ProcessStreamShould
    {
        private readonly IStatisticsAggregator _statisticsAggregator;
        private readonly IResultsRepository _resultsRepository;

        public ProcessStreamShould()
        {
            _statisticsAggregator = Substitute.For<IStatisticsAggregator>();
            _resultsRepository = Substitute.For<IResultsRepository>();
        }

        [Fact]
        public async Task convert_stream_to_string()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapStatisticsAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), new ElectionResultsFile());

            blobProcessor.CsvWasReadAsString.Should().BeTrue();
        }

        [Fact]
        public async Task apply_data_aggregators_to_the_csv_content()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapStatisticsAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), new ElectionResultsFile());

            await _statisticsAggregator.ReceivedWithAnyArgs(1).RetrieveElectionData("", new ElectionResultsFile());
        }

        [Fact]
        public async Task apply_at_least_one_aggregator()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapStatisticsAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), new ElectionResultsFile());

            _statisticsAggregator.CsvParsers.Should().NotBeEmpty();
        }

        [Fact]
        public async Task save_json_in_database()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapStatisticsAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), new ElectionResultsFile());

            await _resultsRepository.ReceivedWithAnyArgs(1).InsertResults(null);
        }

        private TestableFileProcessor CreateTestableBlobProcessor()
        {
            return new TestableFileProcessor(_resultsRepository, _statisticsAggregator, null);
        }

        private void MapStatisticsAggregatorToSuccessfulResult()
        {
            _statisticsAggregator.RetrieveElectionData("", new ElectionResultsFile())
                .ReturnsForAnyArgs(Task.FromResult(Result.Success(new ElectionResultsData())));
        }
    }
}
