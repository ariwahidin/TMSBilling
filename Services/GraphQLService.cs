using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TMSBilling.Services
{
    public class GraphQLService
    {
        private readonly GraphQLHttpClient _client;

        public GraphQLService(string token)
        {
            _client = new GraphQLHttpClient("https://api-staging.mceasy.cloud/graphql", new NewtonsoftJsonSerializer());

            // Set Authorization header
            _client.HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Tambahkan header tambahan jika API membutuhkannya
            // _client.HttpClient.DefaultRequestHeaders.Add("X-Company-Id", "123");
            // _client.HttpClient.DefaultRequestHeaders.Add("X-User-Id", "456");
        }

        public async Task<dynamic> GetGeofences(Guid? customerId)
        {
            var request = new GraphQLRequest
            {
                OperationName = "Geofences",
                Query = @"
query Geofences($filter: GetGeofencesFilter) {
  geofences(filter: $filter) {
    status
    isSuccessful
    message
    errorName
    errorCode
    geofences {
      data {
        geofenceId
        fenceName
        customerName
      }
      pagination {
        total
        page
        show
      }
    }
  }
}",
                Variables = new
                {
                    filter = new
                    {
                        customerId = customerId,
                        isCustomer = true,
                        pagination = new { page = 1, show = 10 }
                    }
                }
            };

            var response = await _client.SendQueryAsync<dynamic>(request);

            if (response.Errors != null && response.Errors.Length > 0)
                throw new HttpRequestException(response.Errors[0].Message);

            return response.Data;
        }
    }
}
