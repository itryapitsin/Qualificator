using System;
using System.Text.RegularExpressions;

namespace Qualificator.Web.Integration.Request
{
    public static class MvcUtils
    {
        public static string GetAntiForgeryToken(string htmlResponseText)
        {
            if (htmlResponseText == null) 
                throw new ArgumentNullException("htmlResponseText");

            var match = Regex.Match(htmlResponseText, @"\<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" \/\>");
            return match.Success 
                ? match.Groups[1].Captures[0].Value 
                : null;
        }
    }
}