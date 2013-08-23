using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Qualificator.Kernel;

namespace Qualificator.Web.Unit
{
    public class BaseControllerTest<TInstance, TContainer> : BaseTest<TInstance, TContainer> where TInstance : Controller
    {
        protected Mock<HttpContextBase> ContextBase = new Mock<HttpContextBase>();
        protected Mock<HttpRequestBase> RequestBase = new Mock<HttpRequestBase>();
        protected Mock<HttpResponseBase> ResponseBase = new Mock<HttpResponseBase>();
        protected Mock<ControllerContext> ControllerContext = new Mock<ControllerContext>();
        protected Mock<HttpSessionStateBase> SessionState = new Mock<HttpSessionStateBase>();
        protected Mock<HttpPostedFileBase> PostedFileBase = new Mock<HttpPostedFileBase>();
        protected FakeSessionState FakeSessionState = new FakeSessionState();

        protected override void TestInitialize(params object[] args)
        {
            RequestInitialize();
            ResponseInitialize();
            ContextInitialize();
            ControllerContextInitialize();

            base.TestInitialize();
        }

        protected virtual void ControllerContextInitialize()
        {
            ControllerContext
                .Setup(x => x.HttpContext)
                .Returns(ContextBase.Object);

            ControllerContext
                .Setup(x => x.Controller)
                .Returns(TestObject);
        }

        protected virtual void ResponseInitialize()
        {
            ResponseBase
                .Setup(s => s.ApplyAppPathModifier(It.IsAny<string>()))
                .Returns<string>(s => s);
        }

        protected virtual void RequestInitialize()
        {
            RequestBase
                .SetupGet(x => x.Headers)
                .Returns(
                    new WebHeaderCollection
                        {
                            {"X-Requested-With", "XMLHttpRequest"}
                        });

            RequestBase
                .Setup(r => r.AppRelativeCurrentExecutionFilePath)
                .Returns("/");

            RequestBase
                .Setup(r => r.ApplicationPath)
                .Returns("/");
        }

        protected virtual void ContextInitialize()
        {
            ContextBase
                .SetupGet(x => x.Request)
                .Returns(RequestBase.Object);

            ContextBase
                .SetupGet(x => x.Response)
                .Returns(ResponseBase.Object);

            ContextBase
                .SetupGet(x => x.Session)
                .Returns(FakeSessionState);
        }

        protected virtual void SetAuthentificatedTestUser(
            string userName, 
            string[] roles)
        {
            ContextBase
                .Setup(x => x.User)
                .Returns(new GenericPrincipal(
                    new GenericIdentity(userName),
                    roles));
        }

        protected virtual void SetAnonimusTestUser()
        {
            ContextBase
                 .Setup(x => x.User)
                 .Returns(new GenericPrincipal(
                 new GenericIdentity(string.Empty),
                 new string[0]));
        }

        protected override void CreateTestObjectInstance(params object[] args)
        {
            base.CreateTestObjectInstance(ContainerWrapper.GetContainer());

            var routes = new RouteCollection();

            ControllerContext
                .Setup(x => x.HttpContext)
                .Returns(ContextBase.Object);

            TestObject.ControllerContext = ControllerContext.Object;
            TestObject.Url = new UrlHelper(new RequestContext(ContextBase.Object, new RouteData()), routes);
            TestObject.Session.Clear();
        }
       

        protected void CreateController(IEnumerable<KeyValuePair<string, object>> sessionDictionary)
        {
            if (sessionDictionary == null)
                return;

            foreach (var pair in sessionDictionary)
            {
                TestObject.Session.Add(pair.Key, pair.Value);
            }
        }

        protected Byte[] GenerateByteArray(int length)
        {
            var result = new byte[length];
            var random = new Random();
            random.NextBytes(result);
            return result;
        }
    }
}
