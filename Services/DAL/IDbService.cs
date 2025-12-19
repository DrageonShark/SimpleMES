using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Services.DAL
{
    public interface IDbService
    {
        /// <summary>
        /// 异步查询列表，获取所有列表
        /// </summary>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);

        /// <summary>
        /// 异步查询单条数据
        /// </summary>
        Task<T?> QueryFirstOrDefault<T>(string sql, object param = null);

        /// <summary>
        /// 异步执行增删改后返回影响的行数
        /// </summary>
        Task<int> ExecuteAsync(string sql, object param = null);
        /// <summary>
        /// 异步执行指定的 SQL 查询，并返回结果集第一行第一列的值。
        /// </summary>
        /// <returns>表示异步操作的任务。任务结果包含结果集首行首列的值，该值转换为类型 T；若结果集为空则返回 </returns>
        Task<T?> ExecuteScalarAsync<T>(string sql, object param = null);
    }
}
