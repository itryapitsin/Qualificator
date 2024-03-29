using System.Web;
using System.Web.Mvc;

namespace Qualificator.Web.Integration.Request
{
    /// <summary>
    /// Represents the result of a simulated request
    /// </summary>
    public class RequestResult
    {
        public HttpResponse Response { get; set; }
        public string ResponseText { get; set; }
        public ActionExecutedContext ActionExecutedContext { get; set; }
        public ResultExecutedContext ResultExecutedContext { get; set; }        
    }
}