namespace Qualificator.Kernel
{
    public interface IContainerWrapper<out T>
    {
        T GetContainer();
    }
}
