using System;

namespace Qualificator.Kernel
{
    public abstract class BaseTest<TInstance, TContainer> : BaseTest<TContainer>
    {
        protected TInstance TestObject;

        protected virtual void CreateTestObjectInstance(params object[] args)
        {
            TestObject = (TInstance)Activator.CreateInstance(typeof(TInstance), args);
        }
        
        protected override void TestInitialize(params object[] args)
        {
            base.TestInitialize();

            CreateTestObjectInstance(args);
        }
    }

    public abstract class BaseTest<TContainer>: BaseTest
    {
        protected IContainerWrapper<TContainer> ContainerWrapper;

        protected TContainer Container;

        protected override void TestInitialize(params object[] args)
        {
            Container = ContainerInitialize();
        }

        protected virtual TContainer ContainerInitialize()
        {
            return ContainerWrapper.GetContainer();
        }
    }

    public abstract class BaseTest
    {
        protected virtual void FeatureInitilize() { }

        protected virtual void TestInitialize(params object[] args) { }
    }
}
