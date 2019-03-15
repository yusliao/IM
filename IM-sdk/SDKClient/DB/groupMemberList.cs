using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    /// <summary>
    /// 群成员列表
    /// </summary>
    public class groupMemberListDB
    {
        [PrimaryKey]
        public int groupId { get; set; } //群ID
        public string MD5 { get; set; }
        /// <summary>
        /// 群成员消息包
        /// </summary>
        public string getGroupMemberListPackage { get; set; }
       
    }
}
