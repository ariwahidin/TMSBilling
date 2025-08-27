using Microsoft.AspNetCore.Mvc;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TMSBilling.Controllers
{
    //public class GraphQLProxyController
    //{
    //}


    [ApiController]
    [Route("api/[controller]")]
    public class GraphQLProxyController : ControllerBase
    {
        private readonly GraphQLHttpClient _client;

        public GraphQLProxyController()
        {
            _client = new GraphQLHttpClient("https://api-staging.mceasy.cloud/graphql", new NewtonsoftJsonSerializer());

            // Set token Authorization
            _client.HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "Bearer uyad5itqHU5ECXaqdEUZLaoYyKZlrC5dkW0eowhm2N2sR96lha6q0barvCB555Yd3ao4Qaw9yekgREose6w959K2Qmea590E4OeaZ822y2Ia3n96apMMfzRQhWpm1lsmxa1dV0a2d82zPZtu8dGPCd222elccbqsp45BEaJjCdk0Gs0JXIA5n4arD1r2X4K6Z50P6wa5p0Fa7dI9abHZ2deuadgr57v7c4joO22Lbd2xwQrA42w05eq0PfQCup2r");

            // Tambahkan header tambahan jika diperlukan
            // _client.HttpClient.DefaultRequestHeaders.Add("X-Company-Id", "123");
            // _client.HttpClient.DefaultRequestHeaders.Add("X-User-Id", "456");
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GraphQLRequest request)
        {
            //try
            //{
            //    var response = await _client.SendQueryAsync<dynamic>(request);

            //    if (response.Errors != null && response.Errors.Length > 0)
            //        return BadRequest(response.Errors);

            //    return Ok(response.Data);
            //}
            //catch (GraphQLHttpRequestException ex)
            //{
            //    return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            //}

            try
            {
                // Konversi ke GraphQLRequest
                var gqlRequest = new GraphQLRequest
                {
                    Query = request.Query,
                    OperationName = request.OperationName,
                    Variables = request.Variables,
                };

                var response = await _client.SendQueryAsync<dynamic>(gqlRequest);

                if (response.Errors != null && response.Errors.Length > 0)
                    return BadRequest(response.Errors);

                return Ok(response.Data);
            }
            catch (GraphQLHttpRequestException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            }
        }
    }
}
