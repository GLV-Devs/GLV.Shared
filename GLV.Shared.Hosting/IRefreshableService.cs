namespace GLV.Shared.Hosting;

public interface IRefreshableService
{
    public Task Initialize(IServiceProvider services);
    public Task Refresh(IServiceProvider services);
}
