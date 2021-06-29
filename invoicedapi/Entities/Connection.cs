using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Invoiced
{
    public class Connection
    {
        private const string jsonAccept = "application/json";
        private readonly string _apikey;

        private HttpClient _client;
        private readonly Environment _env;

        public Connection(string apikey, Environment env)
        {
            _apikey = apikey;
            _env = env;
            _client = Client.httpClient;
        }

        public void TestClient(HttpClient testClient)
        {
            if (_env == Environment.test) _client = testClient;
        }

        public Charge NewCharge()
        {
            return new Charge(this);
        }

        public Coupon NewCoupon()
        {
            return new Coupon(this);
        }

        public CreditBalanceAdjustment NewCreditBalanceAdjustment()
        {
            return new CreditBalanceAdjustment(this);
        }

        public CreditNote NewCreditNote()
        {
            return new CreditNote(this);
        }

        public Customer NewCustomer()
        {
            return new Customer(this);
        }

        public Estimate NewEstimate()
        {
            return new Estimate(this);
        }

        public Event NewEvent()
        {
            return new Event(this);
        }

        public File NewFile()
        {
            return new File(this);
        }

        public Invoice NewInvoice()
        {
            return new Invoice(this);
        }

        public Item NewItem()
        {
            return new Item(this);
        }

        public Note NewNote()
        {
            return new Note(this);
        }

        public Payment NewPayment()
        {
            return new Payment(this);
        }

        public Plan NewPlan()
        {
            return new Plan(this);
        }

        public Refund NewRefund()
        {
            return new Refund(this);
        }

        public Subscription NewSubscription()
        {
            return new Subscription(this);
        }

        public Task NewTask()
        {
            return new Task(this);
        }

        public TaxRate NewTaxRate()
        {
            return new TaxRate(this);
        }

        #region "Async Methods"

        internal async Task<string> PostAsync(string url, Dictionary<string, object> queryParams, string jsonBody)
        {
            url = BaseUrl() + url;

            var uri = AddQueryParamsToUri(url, queryParams);
            var response = await ExecuteRequestAsync(HttpMethod.Post, uri, jsonBody).ConfigureAwait(false);
            return await ProcessResponseAsync(response).ConfigureAwait(false);
        }

        internal async Task<string> PatchAsync(string url, string jsonBody)
        {
            url = BaseUrl() + url;

            var httpPatch = new HttpMethod("PATCH");
            var response = await ExecuteRequestAsync(httpPatch, url, jsonBody).ConfigureAwait(false);
            return await ProcessResponseAsync(response).ConfigureAwait(false);
        }

        internal async Task<string> GetAsync(string url, Dictionary<string, object> queryParams)
        {
            url = BaseUrl() + url;

            var uri = AddQueryParamsToUri(url, queryParams);
            var response = await ExecuteRequestAsync(HttpMethod.Get, uri, null).ConfigureAwait(false);

            return await ProcessResponseAsync(response).ConfigureAwait(false);
        }

        internal async System.Threading.Tasks.Task DeleteAsync(string url)
        {
            url = BaseUrl() + url;

            var response = await ExecuteRequestAsync(HttpMethod.Delete, url, null).ConfigureAwait(false);
            await ProcessResponseAsync(response).ConfigureAwait(false);
        }


        internal async Task<ListResponse> GetListAsync(string url, Dictionary<string, object> queryParams = null)
        {
            var uri = MergeUrl(url, queryParams);
            var response = await ExecuteRequestAsync(HttpMethod.Get, uri, null).ConfigureAwait(false);
            var ResponseText = await ProcessResponseAsync(response);
            var linkString = HttpUtil.GetHeaderFirstValue(response, "Link");
            var totalCount = int.Parse(HttpUtil.GetHeaderFirstValue(response, "X-Total-Count"));

            var links = CommonUtil.parseLinks(linkString);

            return new ListResponse(ResponseText, links, totalCount);
        }

        private Task<HttpResponseMessage> ExecuteRequestAsync(HttpMethod method, string url, string jsonBody)
        {
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(jsonBody))
                request.Content = new StringContent(jsonBody, Encoding.UTF8, jsonAccept);

            request.Headers.Add("Authorization", "Basic " + HttpUtil.BasicAuth(_apikey, ""));

            return _client.SendAsync(request);
        }

        private static async Task<string> ProcessResponseAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NoContent) return "";

            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) throw HandleApiError((int)response.StatusCode, responseText);

            return responseText;
        }

        #endregion

        internal string Post(string url, Dictionary<string, object> queryParams, string jsonBody)
        {
            url = BaseUrl() + url;

            var uri = AddQueryParamsToUri(url, queryParams);
            var response = ExecuteRequest(HttpMethod.Post, uri, jsonBody);
            return ProcessResponse(response);
        }

        internal string Patch(string url, string jsonBody)
        {
            url = BaseUrl() + url;

            var httpPatch = new HttpMethod("PATCH");
            var response = ExecuteRequest(httpPatch, url, jsonBody);
            return ProcessResponse(response);
        }

        internal string Get(string url, Dictionary<string, object> queryParams)
        {
            url = BaseUrl() + url;

            var uri = AddQueryParamsToUri(url, queryParams);
            var response = ExecuteRequest(HttpMethod.Get, uri, null);

            return ProcessResponse(response);
        }

        internal ListResponse GetList(string url, Dictionary<string, object> queryParams = null)
        {
            var uri = MergeUrl(url, queryParams);
            var response = ExecuteRequest(HttpMethod.Get, uri, null);
            var responseText = ProcessResponse(response);
            var linkString = HttpUtil.GetHeaderFirstValue(response, "Link");
            var totalCount = int.Parse(HttpUtil.GetHeaderFirstValue(response, "X-Total-Count"));

            var links = CommonUtil.parseLinks(linkString);

            return new ListResponse(responseText, links, totalCount);
        }

        internal void Delete(string url)
        {
            url = BaseUrl() + url;

            var response = ExecuteRequest(HttpMethod.Delete, url, null);
            ProcessResponse(response);
        }

        /// <summary>
        /// Merges the given url with the Base Url of the connection environment.
        /// </summary>
        /// <param name="url">The URL to merge with the BaseUrl</param>
        ///<param name="queryParams">Any additional query parameters to add to the final URL</param>
        /// <returns>The final, constructed, URL that combines the base URL, the passed in URL, and any additional query parameters.</returns>
        private string MergeUrl(string url, Dictionary<string, object> queryParams = null)
        {
            var result = BaseUrl();

            if (!string.IsNullOrWhiteSpace(url) && url.StartsWith("/"))
            {
                result += url;
                url = null;
            }

            if (!string.IsNullOrEmpty(url))
            {
                UriBuilder first = new UriBuilder(result);
                UriBuilder second = new UriBuilder(url);

                first.Path = second.Path;

                var pFirst = ChangeDictionarySignature(System.Web.HttpUtility.ParseQueryString(first.Query));
                var pSecond = ChangeDictionarySignature(System.Web.HttpUtility.ParseQueryString(second.Query));
                Dictionary<string, object> pOutput = MergeQueryParams(pFirst, pSecond);

                if (queryParams != null)
                    pOutput = MergeQueryParams(pOutput, queryParams);

                if (pOutput.Count > 0)
                {
                    StringBuilder query = new StringBuilder();

                    foreach (string key in pOutput.Keys)
                    {
                        if (query.Length > 0)
                            query.Append('&');

                        query.Append(WebUtility.UrlEncode(key));
                        query.Append('=');
                        query.Append(WebUtility.UrlEncode(pOutput[key].ToString()));
                    }

                    first.Query = query.ToString();
                }

                result = first.Uri.AbsoluteUri;
            }
            else
                result = AddQueryParamsToUri(result, queryParams);

            return result;
        }

        private static Dictionary<string, object> ChangeDictionarySignature(System.Collections.Specialized.NameValueCollection incoming)
        {
            Dictionary<string, object> output = new Dictionary<string, object>(incoming.Count);

            foreach (string key in incoming.AllKeys)
                output.Add(key, incoming[key]);

            return output;
        }

        internal string BaseUrl()
        {
            if (_env == Environment.local)
                return ConnectionURL.invoicedLocal;
            if (_env == Environment.sandbox)
                return ConnectionURL.invoicedSandbox;
            if (_env == Environment.production)
                return ConnectionURL.invoicedProduction;
            if (_env == Environment.test)
                return ConnectionURL.invoicedTest;
            throw new ConnException("Environment not recognized");
        }

        private HttpResponseMessage ExecuteRequest(HttpMethod method, string url, string jsonBody)
        {
            return ExecuteRequestAsync(method, url, jsonBody).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static string ProcessResponse(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NoContent) return "";
            var responseText = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode) throw HandleApiError((int)response.StatusCode, responseText);

            return responseText;
        }

        private static Dictionary<string, object> MergeQueryParams(Dictionary<string, object> firstParams, Dictionary<string, object> secondParams)
        {
            Dictionary<string, object> Merged = new Dictionary<string, object>();

            var Subset = firstParams.Keys.Intersect(secondParams.Keys);

            foreach (string key in Subset)
                Merged.Add(key, secondParams[key]);

            Subset = firstParams.Keys.Except(secondParams.Keys);

            foreach (string key in Subset)
                Merged.Add(key, firstParams[key]);

            Subset = secondParams.Keys.Except(firstParams.Keys);

            foreach (string key in Subset)
                Merged.Add(key, secondParams[key]);

            return Merged;
        }

        private static string AddQueryParamsToUri(string uri, Dictionary<string, object> queryParams)
        {
            var builder = new UriBuilder(uri);

            if (queryParams != null && queryParams.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(builder.Query))
                {
                    Dictionary<string, object> first = new Dictionary<string, object>();
                    var orig = System.Web.HttpUtility.ParseQueryString(builder.Query);

                    foreach (string key in orig.AllKeys)
                        first.Add(key, orig[key]);

                    queryParams = MergeQueryParams(first, queryParams);
                }

                var querySegments = new List<string>();
                foreach (var param in queryParams)
                    querySegments.Add(WebUtility.UrlEncode(param.Key) + "=" +
                                      WebUtility.UrlEncode(param.Value.ToString()));
                builder.Query = string.Join("&", querySegments);
            }

            return builder.ToString();
        }

        private static InvoicedException HandleApiError(int responseCode, string responseBody)
        {
            if (responseCode == 401)
                return new AuthException(responseBody);
            if (responseCode == 400)
                return new InvalidRequestException(responseBody);
            if (responseCode == 429)
                return new RateLimitException(responseBody);
            if (responseCode >= 500)
                return new InternalServerException(responseBody);
            return new ApiException(responseBody);
        }
    }
}