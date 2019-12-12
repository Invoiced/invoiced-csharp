﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;

namespace Invoiced
{
    
    public class Connection
    {

        private static readonly string jsonAccept = "application/json";
        private string _apikey;
        private Environment _env;

        private HttpClient _client;

        public Connection(string apikey,Environment env) {
            this._apikey = apikey;
            this._env = env;
            this._client = Client.httpClient;
        }

        public void TestClient(HttpClient testClient) {
            if (this._env == Environment.test) {
                this._client = testClient;
            }
        }

        public Customer NewCustomer() {
            Customer customer = new Customer(this);
            return customer;
        }

        public Invoice NewInvoice() {
            Invoice invoice = new Invoice(this);
            return invoice;
        }

        public CreditNote NewCreditNote() {
            CreditNote creditNote = new CreditNote(this);
            return creditNote;
        }

        public Estimate NewEstimate() {
            Estimate estimate = new Estimate(this);
            return estimate;
        }

        public Transaction NewTransaction() {
            Transaction transaction = new Transaction(this);
            return transaction;
        }

        public Subscription NewSubscription() {
            Subscription subscription = new Subscription(this);
            return subscription;
        }

        public Event NewEvent() {
            Event ev = new Event(this);
            return ev;
        }

        public CatalogItem NewCatalogItem() {
            CatalogItem catalogItem = new CatalogItem(this);
            return catalogItem;
        }

        public TaxRate NewTaxRate() {
            TaxRate taxRate = new TaxRate(this);
            return taxRate;
        }

        public Coupon NewCoupon() {
            Coupon coupon = new Coupon(this);
            return coupon;
        }

        public Plan NewPlan() {
            Plan plan = new Plan(this);
            return plan;
        }

        internal string Post(string url, Dictionary<string,Object> queryParams, string jsonBody)
        {

            url = BaseUrl() + url;
            
            string uri = AddQueryParamsToUri(url,queryParams);
            var response = ExecuteRequest(HttpMethod.Post,uri, jsonBody);
            var responseText = ProcessResponse(response);
            
            return responseText;
        }

        internal string Patch(string url, string jsonBody) {
            
            url = BaseUrl() + url;

            var httpPatch = new HttpMethod("PATCH");
            var response = ExecuteRequest(httpPatch,url, jsonBody);
            var responseText = ProcessResponse(response);

            return responseText;
        }

        internal string Get(string url, Dictionary<string,Object> queryParams) {
            
            url = BaseUrl() + url;
            
            string uri = AddQueryParamsToUri(url,queryParams);
            var response = ExecuteRequest(HttpMethod.Get,uri, null);

            var responseText = ProcessResponse(response);

            return responseText;
        }

        internal ListResponse GetList(string url, Dictionary<string,Object> queryParams = null) {
            
            url = BaseUrl() + url;

            string uri = AddQueryParamsToUri(url,queryParams);
            var response = ExecuteRequest(HttpMethod.Get,uri, null);
            var responseText = ProcessResponse(response);
            var linkString = HttpUtil.GetHeaderFirstValue(response,"Link");
            var totalCount = Int32.Parse(HttpUtil.GetHeaderFirstValue(response, "X-Total-Count"));

            var links = CommonUtil.parseLinks(linkString);

            var listResponse =  new ListResponse(responseText,links,totalCount);

            return listResponse;
            
        }

        internal void Delete(string url) {
            
            url = BaseUrl() + url;

            var response = ExecuteRequest(HttpMethod.Delete,url, null);
            ProcessResponse(response);

        }

        internal String BaseUrl() {

            if (this._env == Environment.local) {
                return ConnectionURL.invoicedLocal;
            } else if (this._env == Environment.sandbox) {
                return ConnectionURL.invoicedSandbox;
            } else if (this._env == Environment.production) {
                return ConnectionURL.invoicedProduction;
            } else if (this._env == Environment.test) {
                return ConnectionURL.invoicedTest; 
            } else {
                throw new ConnException("Environment not recognized");
            }
        }

        private HttpResponseMessage ExecuteRequest(HttpMethod method, string url, string jsonBody)
        {

            var request = new HttpRequestMessage(method,url);

            if (!string.IsNullOrEmpty(jsonBody)) {
                request.Content = new StringContent(jsonBody,Encoding.UTF8,jsonAccept);
            }
            request.Headers.Add("Authorization", "Basic " + HttpUtil.BasicAuth(_apikey,""));
        
            var response = _client.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        
            return response;
        }

        private string ProcessResponse(HttpResponseMessage response) {

            if (response.StatusCode == HttpStatusCode.NoContent) {
                return "";
            }
            var responseText = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            
            if (!response.IsSuccessStatusCode) {
                throw HandleApiError((int)response.StatusCode,responseText);
            }

            return responseText;
        }

        private string AddQueryParamsToUri(string uri, Dictionary<string,Object> queryParams ) {

            var builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query);

            if (queryParams != null) {
                foreach (var param in queryParams) {
                    query.Add(param.Key.ToString(),param.Value.ToString());
                }

                builder.Query = query.ToString();
            }
        
            return Uri.EscapeUriString(builder.ToString());
        }

        private InvoicedException HandleApiError(int responseCode, String responseBody) {
            if (responseCode == 401) {
                return new AuthException(responseBody);
            } else if (responseCode == 400) {
                return new InvalidRequestException(responseBody);
            } else if (responseCode == 429) {
                return new RateLimitException(responseBody);
            }  else if (responseCode >= 500) {
                return new InternalServerException(responseBody);
            } else {
                return new ApiException(responseBody);
            }
        }

    }

}
