﻿using ElectionResults.Core.Models;
using ElectionResults.Core.Services;
using FluentAssertions;
using Xunit;

namespace ElectionResults.Tests.FileNameParserTests
{
    public class BuildElectionStatisticsShould
    {
        [Fact]
        public void return_a_file_parsing_result()
        {
            var electionStatistics = FileNameParser.BuildElectionStatistics("FINAL_DSPR_1561818562.csv", new ElectionResultsData());

            electionStatistics.Should().NotBeNull();
        }

        [Fact]
        public void set_properties_from_file()
        {
            var electionStatistics = FileNameParser.BuildElectionStatistics("FINAL_DSPR_1561818562.csv", new ElectionResultsData());

            electionStatistics.Location.Should().Be("DSPR");
            electionStatistics.Id.Should().Be("FINAL_DSPR_1561818562");
            electionStatistics.FileTimestamp.Should().Be(1561818562);
            electionStatistics.StatisticsJson.Should().NotBeNullOrEmpty();
            electionStatistics.Type.Should().Be("FINAL");
        }
    }
}
