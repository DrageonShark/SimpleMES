using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Services.DAL
{
    /// <summary>
    /// 支持的数据库类型。
    /// </summary>
    public enum DatabaseProviderType
    {
        SqlServer = 0,
        Sqlite = 1,
        InMemoryMock = 2
    }
}
