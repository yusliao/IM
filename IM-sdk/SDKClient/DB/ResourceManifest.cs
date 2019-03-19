using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace SDKClient.DB
{
    class ResourceManifest
    {
        [PrimaryKey]
        public string MD5 { get; set; }
        public long Size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int State { get; set; }
    }
}
