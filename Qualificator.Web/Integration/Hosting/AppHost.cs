using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Qualificator.Web.Integration.Interception;
using Qualificator.Web.Integration.Request;

namespace Qualificator.Web.Integration.Hosting
{
    /// <summary>
    /// Hosts an ASP.NET application within an ASP.NET-enabled .NET appdomain
    /// and provides methods for executing test code within that appdomain
    /// </summary>
    public class AppHost
    {
        private readonly AppDomainProxy _appDomainProxy; // The gateway to the ASP.NET-enabled .NET appdomain
        private static readonly MethodInfo GetApplicationInstanceMethod;
        private static readonly MethodInfo RecycleApplicationInstanceMethod;

        static AppHost()
        {
            // Get references to some MethodInfos we'll need to use later to bypass nonpublic access restrictions
            var httpApplicationFactory = typeof(HttpContext).Assembly.GetType("System.Web.HttpApplicationFactory", true);
            GetApplicationInstanceMethod = httpApplicationFactory.GetMethod("GetApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
            RecycleApplicationInstanceMethod = httpApplicationFactory.GetMethod("RecycleApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public AppHost(
            string appPhysicalDirectory,
            string virtualDirectory = "/")
        {
            try 
            {
                _appDomainProxy = (AppDomainProxy) ApplicationHost.CreateApplicationHost(
                    typeof (AppDomainProxy), 
                    virtualDirectory,
                    appPhysicalDirectory);

            } 
            catch(FileNotFoundException ex) 
            {
                if (ex.Message.Contains("MvcIntegrationTestFramework"))
                {
                    var message =
                        string.Format(
                            "Could not load Qualificator.dll within a bin directory under {0}. Is this the path to your ASP.NET MVC application, and have you set up a post-build event to copy your test assemblies and their dependencies to this folder? See the demo project for an example.",
                            appPhysicalDirectory);
                    throw new InvalidOperationException(message);
                }

                throw;
            }

            _appDomainProxy.RunCodeInAppDomain(() => {
                InitializeApplication();
                AttachTestControllerDescriptorsForAllControllers();
                LastRequestData.Reset();
            });
        }

        public void Virtualize(Action<VirtualRequest> testScript)
        {
            var serializableDelegate = new SerializableDelegate<Action<VirtualRequest>>(testScript);
            _appDomainProxy.RunVirtualizationInAppDomain(serializableDelegate);
        }

        private static void InitializeApplication()
        {
            var appInstance = GetApplicationInstance();
            appInstance.PostRequestHandlerExecute += (sender, args) =>
                {
                    // Collect references to context objects that would otherwise be lost
                    // when the request is completed
                    if (LastRequestData.HttpSessionState == null)
                        LastRequestData.HttpSessionState = HttpContext.Current.Session;

                    if (LastRequestData.Response == null)
                        LastRequestData.Response = HttpContext.Current.Response;
                };
            RefreshEventsList(appInstance);

            RecycleApplicationInstance(appInstance);
        }

        private static void AttachTestControllerDescriptorsForAllControllers()
        {
            var allControllerTypes = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                     from type in assembly.GetTypes()
                                     where typeof (IController).IsAssignableFrom(type)
                                     select type;

            foreach (var controllerType in allControllerTypes)
                InterceptionFilter.AssociateWithControllerType(controllerType);
        }
        
        private static HttpApplication GetApplicationInstance()
        {
            var writer = new StringWriter();
            var workerRequest = new SimpleWorkerRequest("", "", writer);
            var httpContext = new HttpContext(workerRequest);
            return (HttpApplication)GetApplicationInstanceMethod.Invoke(null, new object[] { httpContext });
        }

        private static void RecycleApplicationInstance(HttpApplication appInstance)
        {
            RecycleApplicationInstanceMethod.Invoke(null, new object[] { appInstance });
        }

        private static void RefreshEventsList(HttpApplication appInstance)
        {
            var stepManager = typeof (HttpApplication).GetField("_stepManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(appInstance);
            var resumeStepsWaitCallback = typeof(HttpApplication).GetField("_resumeStepsWaitCallback", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(appInstance);
            var buildStepsMethod = stepManager.GetType().GetMethod("BuildSteps", BindingFlags.NonPublic | BindingFlags.Instance);
            buildStepsMethod.Invoke(stepManager, new[] { resumeStepsWaitCallback });
        }
    }
}