using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public  class UserModel
    {
        public int UserId;
        public string UserName;
        public int Role = 1; //1:admin，2:leader，3:employee
        public string Account;
        public string PasswordHash; //密码哈希值
        public string Salt; //密码盐值
        public string? Email;
        public byte IsActive = 1;
    }
}
