using System;
using Qualificator.Web.Integration.Request;

namespace Qualificator.Web.Integration.Hosting
{
    /// <summary>
    /// Simply provides a remoting gateway to execute code within the ASP.NET-hosting appdomain
    /// </summary>
    internal class AppDomainProxy : MarshalByRefObject
    {
        public void RunCodeInAppDomain(Action codeToRun)
        {
            codeToRun();
        }

        public void RunVirtualizationInAppDomain(SerializableDelegate<Action<VirtualRequest>> script)
        {
            var virtualRequest = new VirtualRequest();
            script.Delegate(virtualRequest);
        }

        public override object InitializeLifetimeService()
        {
            return null; // Tells .NET not to expire this remoting object
        }
    }
}