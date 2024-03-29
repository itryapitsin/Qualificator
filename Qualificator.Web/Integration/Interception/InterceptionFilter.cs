﻿using System;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace Qualificator.Web.Integration.Interception
{
    /// <summary>
    /// An ASP.NET MVC filter attached automatically to all controllers invoked within the test application
    /// This is used to capture action results and other output generated by each request
    /// </summary>
    internal class InterceptionFilter : ActionFilterAttribute
    {
        private static readonly object StaticDescriptorCacheInstance;
        private static readonly MethodInfo FetchOrCreateItemMethod;

        public static HttpContext LastHttpContext { get; private set; }

        static InterceptionFilter()
        {
            var staticDescriptorCache = typeof(ControllerActionInvoker).GetField("_staticDescriptorCache", BindingFlags.NonPublic | BindingFlags.Static);
            if (staticDescriptorCache == null)
                throw new NullReferenceException("staticDescriptorCache");

            StaticDescriptorCacheInstance = staticDescriptorCache.GetValue(null);

            FetchOrCreateItemMethod = StaticDescriptorCacheInstance
                .GetType()
                .GetMethod("FetchOrCreateItem", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void AssociateWithControllerType(Type controllerType)
        {
            Func<ControllerDescriptor> descriptorCreator = () => new InterceptionFilterControllerDescriptor(controllerType);
            FetchOrCreateItemMethod.Invoke(
                StaticDescriptorCacheInstance, 
                new object[] { controllerType,  descriptorCreator });
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (LastHttpContext == null)
                LastHttpContext = HttpContext.Current;

            // Clone to get a more stable snapshot
            if ((filterContext != null) && (LastRequestData.ActionExecutedContext == null))
                LastRequestData.ActionExecutedContext = new ActionExecutedContext {
                    ActionDescriptor = filterContext.ActionDescriptor,
                    Canceled = filterContext.Canceled,
                    Controller = filterContext.Controller,
                    Exception = filterContext.Exception,
                    ExceptionHandled = filterContext.ExceptionHandled,
                    HttpContext = filterContext.HttpContext,
                    RequestContext = filterContext.RequestContext,
                    Result = filterContext.Result,
                    RouteData = filterContext.RouteData
                };
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            // Clone to get a more stable snapshot
            if ((filterContext != null) && (LastRequestData.ResultExecutedContext == null))
                LastRequestData.ResultExecutedContext = new ResultExecutedContext {
                    Canceled = filterContext.Canceled,
                    Exception = filterContext.Exception,
                    Controller = filterContext.Controller,
                    ExceptionHandled = filterContext.ExceptionHandled,
                    HttpContext = filterContext.HttpContext,
                    RequestContext = filterContext.RequestContext,
                    Result = filterContext.Result,
                    RouteData = filterContext.RouteData
                };
        }
    }
}