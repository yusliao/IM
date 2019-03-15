using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    public  class JoinGroupDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 消息ID
        /// </summary>
        [Indexed]
        public string msgId { get; set; }
        public int groupId { get; set; }
        /// <summary>
        /// 申请人
        /// </summary>
        public int userId { get; set; }
        /// <summary>
        /// 入群申请
        /// </summary>
        public string JoinGroupPackage { get; set; }
    }
}
