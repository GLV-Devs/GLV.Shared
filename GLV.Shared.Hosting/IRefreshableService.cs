namespace GLV.Shared.Hosting;

public interface IRefreshableService
{
    public Task Initialize();
    public Task Refresh();
}
