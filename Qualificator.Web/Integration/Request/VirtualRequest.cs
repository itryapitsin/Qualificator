using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using Qualificator.Web.Integration.Interception;

namespace Qualificator.Web.Integration.Request
{
    public class VirtualRequest
    {
        public HttpSessionState Session { get; private set; }
        public HttpCookieCollection Cookies { get; private set; }

        public VirtualRequest()
        {
            Cookies = new HttpCookieCollection();
        }

        public RequestResult ProcessRequest(string url)
        {
            return ProcessRequest(url, HttpVerbs.Get, null);
        }

        public RequestResult ProcessRequest(
            string url,
            HttpVerbs httpVerb, 
            NameValueCollection formValues)
        {
            return ProcessRequest(url, httpVerb, formValues, null);
        }

        public RequestResult ProcessRequest(
            string url, 
            HttpVerbs httpVerb, 
            NameValueCollection formValues, 
            NameValueCollection headers)
        {
            if (url == null) 
                throw new ArgumentNullException("url");

            // Fix up URLs that incorrectly start with / or ~/
            if (url.StartsWith("~/"))
                url = url.Substring(2);

            else if(url.StartsWith("/"))
                url = url.Substring(1);

            // Parse out the querystring if provided
            var query = "";
            var querySeparatorIndex = url.IndexOf("?", StringComparison.Ordinal);
            if (querySeparatorIndex >= 0) {
                query = url.Substring(querySeparatorIndex + 1);
                url = url.Substring(0, querySeparatorIndex);
            }                

            // Perform the request
            LastRequestData.Reset();
            var output = new StringWriter();
            var httpVerbName = httpVerb.ToString().ToLower();
            var workerRequest = new VirtualedWorkerRequest(url, query, httpVerbName, Cookies, formValues, headers, output);
            HttpRuntime.ProcessRequest(workerRequest);

            // Capture the output
            AddAnyNewCookiesToCookieCollection();
            Session = LastRequestData.HttpSessionState;

            return new RequestResult
            {
                ResponseText = output.ToString(),
                ActionExecutedContext = LastRequestData.ActionExecutedContext,
                ResultExecutedContext = LastRequestData.ResultExecutedContext,
                Response = LastRequestData.Response,
            };
        }

        private void AddAnyNewCookiesToCookieCollection()
        {
            if(LastRequestData.Response == null)
                return;

            var lastResponseCookies = LastRequestData.Response.Cookies;

            foreach (string cookieName in lastResponseCookies) {
                var cookie = lastResponseCookies[cookieName];

                if (Cookies[cookieName] != null)
                    Cookies.Remove(cookieName);

                if((cookie.Expires == default(DateTime)) || (cookie.Expires > DateTime.Now))
                    Cookies.Add(cookie);
            }
        }
    }
}