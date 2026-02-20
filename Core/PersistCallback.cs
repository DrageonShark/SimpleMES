using System.Threading;
using SimpleMES.Services.DAL;

namespace SimpleMES.Core
{
    public delegate Task PersistCallback(IDataRepository repository, CancellationToken token);
}
