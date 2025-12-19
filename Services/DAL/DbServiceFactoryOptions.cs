using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Services.DAL
{
    /// <summary>
    /// 用于描述工厂创建服务时所需的参数（连接字符串、数据库类型等）。
    /// </summary>
    public class DbServiceFactoryOptions
    {
        public DatabaseProviderType ProviderType { get; set; } = DatabaseProviderType.SqlServer;
        public string ConnectionString { get; set; } = string.Empty;
    }
}
