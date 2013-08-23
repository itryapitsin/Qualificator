using System.Reflection;
using System.Web.Mvc;

namespace Qualificator.Web.Integration.Interception
{
    /// <summary>
    /// A special ASP.NET MVC action descriptor used to attach InterceptionFilter to all loaded controllers
    /// </summary>
    internal class InterceptionFilterActionDescriptor : ReflectedActionDescriptor
    {
        public InterceptionFilterActionDescriptor(
            MethodInfo methodInfo, 
            string actionName, 
            ControllerDescriptor controllerDescriptor)
            : base(methodInfo, actionName, controllerDescriptor)
        {
        }
    }
}