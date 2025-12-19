using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Services.DAL
{
    /// <summary>
    /// DbService 工厂接口：屏蔽具体数据库实现，便于日后扩展。
    /// </summary>
    public interface IDbServiceFactory
    {
        IDbService CreateService();
    }
}
