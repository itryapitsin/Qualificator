using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Linq;

namespace Qualificator.Web.Integration.Request
{
    internal class VirtualedWorkerRequest : SimpleWorkerRequest
    {
        private readonly string _httpVerbName;
        private readonly HttpCookieCollection _cookies;
        private readonly NameValueCollection _body;
        private readonly NameValueCollection _headers;

        public VirtualedWorkerRequest(
            string page,
            string query, 
            string httpVerbName, 
            HttpCookieCollection cookies,
            NameValueCollection body,
            NameValueCollection headers, 
            TextWriter output)
            : base(page, query, output)
        {
            _cookies = cookies;
            _httpVerbName = httpVerbName;
            _body = body;
            _headers = headers;
        }

        public override string GetHttpVerbName()
        {
            return _httpVerbName;
        }

        public override string GetKnownRequestHeader(int index)
        {
            // Override "Content-Type" header for POST requests, otherwise ASP.NET won't read the Form collection
            if (index == 12 && string.Equals(_httpVerbName, "post", StringComparison.OrdinalIgnoreCase))
                return "application/x-www-form-urlencoded";

            switch (index)
            {
                case 0x19:
                    return MakeCookieHeader();

                default:
                    if (_headers == null)
                        return null;

                    return _headers[GetKnownRequestHeaderName(index)];
            }
        }

        public override string GetUnknownRequestHeader(string name)
        {
            return _headers == null
                ? null
                : _headers[name];
        }

        public override string[][] GetUnknownRequestHeaders()
        {
            if (_headers == null)
                return null;

            var unknownHeaders = from key in _headers.Keys.Cast<string>()
                                 let knownRequestHeaderIndex = GetKnownRequestHeaderIndex(key)
                                 where knownRequestHeaderIndex < 0
                                 select new[] { key, _headers[key] };

            return unknownHeaders.ToArray();
        }

        public override byte[] GetPreloadedEntityBody()
        {
            if (_body == null)
                return base.GetPreloadedEntityBody();

            var sb = new StringBuilder();

            foreach (string key in _body)
                sb.AppendFormat("{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(_body[key]));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string MakeCookieHeader()
        {
            if ((_cookies == null) || (_cookies.Count == 0))
                return null;
            var sb = new StringBuilder();

            foreach (string cookieName in _cookies)
                sb.AppendFormat("{0}={1};", cookieName, _cookies[cookieName].Value);

            return sb.ToString();
        }
    }
}