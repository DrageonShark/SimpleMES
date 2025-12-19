using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public enum OrderStatus
    {
        Pending = 0,    // 待产
        Producing = 1,  // 生产中
        Paused = 2,     // 暂停
        Completed = 3   // 完工
    }
}
