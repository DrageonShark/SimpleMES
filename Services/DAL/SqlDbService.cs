using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace SimpleMES.Services.DAL
{
    public class SqlDbService:IDbService
    {
        private readonly string _connectionString = DbConfig.ConnectionString;

        //获取数据库连接对象（工厂模式的体现：按需生产连接）
        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.QueryAsync<T>(sql, param);
            }
        }

        public async Task<T?> QueryFirstOrDefault<T>(string sql, object param = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
            }
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.ExecuteAsync(sql, param);
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object param = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.ExecuteScalarAsync<T>(sql, param);
            }
        }
    }
}
