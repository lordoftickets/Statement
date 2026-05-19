using System.Threading.Tasks;
using Statement.State;

namespace Statement.Tests.TestStates.Statement;

public class AsyncStatement : IAsyncStatement
{
    public bool OnEntryAsyncCalled { get; set; }
    public bool OnExitAsyncCalled { get; set; }

    public Task OnEntryAsync()
    {
        OnEntryAsyncCalled = true;
        return Task.CompletedTask;
    }

    public Task OnExitAsync()
    {
        OnExitAsyncCalled = true;
        return Task.CompletedTask;
    }
}
