using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    /// <summary>
    /// 好友列表
    /// </summary>
    public class contactListDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
       
        /// <summary>
        /// 获取联系人列表
        /// </summary>
        public string getContactsListPackage { get; set; }
        public string MD5 { get; set; }

    }
}
