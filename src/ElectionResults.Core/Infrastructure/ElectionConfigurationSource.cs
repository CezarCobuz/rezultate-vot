using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Models;
using ElectionResults.Core.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ElectionResults.Core.Infrastructure
{
    public class ElectionConfigurationSource : IElectionConfigurationSource
    {
        private readonly AppConfig _config;
        private readonly AmazonSimpleSystemsManagementClient _amazonSettingsClient;
        private readonly string _parameterStoreName;

        public ElectionConfigurationSource(IOptions<AppConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _config = config.Value;
            _parameterStoreName = Consts.ParameterStoreName;
            if (hostingEnvironment.IsDevelopment())
            {
                var systemsManagementConfig = new AmazonSimpleSystemsManagementConfig
                {
                    ServiceURL = Consts.SSMServiceUrl,
                    UseHttp = true
                };
                _amazonSettingsClient = new AmazonSimpleSystemsManagementClient(new BasicAWSCredentials("abc", "def"), systemsManagementConfig);
                _parameterStoreName += "-dev";
            }
            if (hostingEnvironment.IsStaging())
                _parameterStoreName += "-stag";
        }

        public async Task<Result> UpdateInterval(int seconds)
        {
            var putParameterRequest = new PutParameterRequest
            {
                Name = $"/{_parameterStoreName}/settings/intervalInSeconds",
                Value = seconds.ToString(),
                Type = ParameterType.String,
                Overwrite = true
            };
            var response = await _amazonSettingsClient.PutParameterAsync(putParameterRequest);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                return Result.Success();
            return Result.Failure("Couldn't update the job timer");
        }

        public async Task<Result<int>> GetInterval()
        {
            try
            {
                var getParameterRequest = new GetParameterRequest
                {
                    Name = $"/{_parameterStoreName}/settings/intervalInSeconds",
                };
                var response = await _amazonSettingsClient.GetParameterAsync(getParameterRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                    return Result.Success(int.Parse(response.Parameter.Value));
                return Result.Failure<int>("Couldn't retrieve the job timer");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(e.Message);
            }
        }

        public async Task<Result> UpdateElectionConfig(List<Election> elections)
        {
            var putParameterRequest = new PutParameterRequest
            {
                Name = $"/{_parameterStoreName}/settings/electionsConfig",
                Value = JsonConvert.SerializeObject(elections),
                Type = ParameterType.String,
                Overwrite = true
            };
            var response = await _amazonSettingsClient.PutParameterAsync(putParameterRequest);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                return Result.Success();
            return Result.Failure("Couldn't update the job timer");
        }

        public async Task<Result<string>> GetConfigAsync()
        {
            var getParameterRequest = new GetParameterRequest
            {
                Name = $"/{_parameterStoreName}/settings/electionsConfig",
            };
            var response = await _amazonSettingsClient.GetParameterAsync(getParameterRequest);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                return Result.Success(response.Parameter.Value);
            return Result.Failure<string>("Couldn't update the job timer");
        }

        public Result<Election> GetElectionById(string electionId)
        {
            if (string.IsNullOrEmpty(_config?.ElectionsConfig))
            {
                return Result.Failure<Election>($"Could not find election with id {electionId}");
            }
            var electionsConfig = JsonConvert.DeserializeObject<List<Election>>(_config.ElectionsConfig);
            var foundElection = electionsConfig.SingleOrDefault(e => e.ElectionId == electionId);
            if (foundElection == null)
                return Result.Failure<Election>($"Could not find election with id {electionId}");
            return Result.Success(foundElection);
        }
    }
}