using System.Threading.Tasks;

namespace Statement.State;

public interface IAsyncStatement
{
    Task OnEntryAsync();
    Task OnExitAsync();
}
