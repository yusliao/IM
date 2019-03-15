using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    /// <summary>
    /// 群列表信息
    /// </summary>
    public class groupListDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string MD5 { get; set; }
       
        public string getGroupListPackage { get; set; }
    }
}
