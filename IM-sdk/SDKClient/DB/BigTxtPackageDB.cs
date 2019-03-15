using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class BigTxtPackageDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 消息ID
        /// </summary>
        [SQLite.Unique]
        public string msgId { get; set; }
       
        /// <summary>
        /// 包ID，可重用包ID
        /// </summary>
        public string partName { get; set; }
        /// <summary>
        /// 包序号，0开始
        /// </summary>
        public int partOrder { get; set; }
        public string text { get; set; }
        /// <summary>
        /// 总包数，200长度/包
        /// </summary>
        public int partTotal { get; set; }//200一包，总包数
      
        public string Source { get; set; }
        public DateTime? time { get; set; }
    }
}
