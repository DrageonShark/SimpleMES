using System.Threading;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.Strategy
{
    public delegate Task PersistCallback(IDataRepository repository, CancellationToken token);
}
