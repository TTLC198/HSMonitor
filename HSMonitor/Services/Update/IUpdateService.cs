using System.Threading.Tasks;

namespace HSMonitor.Services.Update;

public interface IUpdateService
{
    public Task UpdateAsync();
}