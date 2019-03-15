using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class GroupInfo
    {

        [PrimaryKey]
        /// <summary>
        /// 群ID
        /// </summary>
        public int groupId { get; set; }
        public string MD5 { get; set; }

        public string getGroupPackage { get; set; }

    }
}
