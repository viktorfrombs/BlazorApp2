using BlazorApp2.Helpers;
using BlazorApp2.Models.Configurations;
using BlazorApp2.Services.Interfaces;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BlazorApp2.Services
{
    public class GraphService2 : IGraphService2
    {
        private GraphClientConfiguration _configuration { get; set; }

        public GraphService2(GraphClientConfiguration graphClientConfiguration)
        {
            _configuration = graphClientConfiguration;
        }

        private IGraphServiceClient _client;

        private async Task CreateOnBehalfOfUserAsync(string userToken)
        {
            var clientApp = ConfidentialClientApplicationBuilder
                .Create(_configuration.ClientId)
                .WithTenantId(_configuration.TenantId)
                .WithClientSecret(_configuration.ClientSecret)
                .Build();

            var authResult = await clientApp
                .AcquireTokenOnBehalfOf(new[] { "User.Read", "GroupMember.Read.All" }, new UserAssertion(userToken))
                .ExecuteAsync();

            _client = new GraphServiceClient(
                "https://graph.microsoft.com/v1.0",
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);
                }));
        }

        public async Task<IEnumerable<string>> CheckMemberGroupsAsync(string userToken, IEnumerable<string> groupIds)
        {
            await CreateOnBehalfOfUserAsync(userToken);

            //You can check up to a maximum of 20 groups per request (see graph api doc).
            var batchSize = 20;

            var tasks = new List<Task<IDirectoryObjectCheckMemberGroupsCollectionPage>>();
            foreach (var groupsBatch in groupIds.Batch(batchSize))
            {
                tasks.Add(_client.Me.CheckMemberGroups(groupsBatch).Request().PostAsync());
            }
            await Task.WhenAll(tasks);

            return tasks.SelectMany(x => x.Result.ToList());
        }

        public async Task<List<string>> GetAllUserDisplayNames(string userToken)
        {
            await CreateOnBehalfOfUserAsync(userToken);

            var task = _client.Users.Request().GetAsync();
            var users = await task;
            var userDisplayNames = new List<string>();
            foreach (var user in users)
            {
                userDisplayNames.Add(user.DisplayName);
            }
            return userDisplayNames;
        }
    }
}
