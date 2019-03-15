using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    public class LinkEntity
    {
        public string id { get; set; }
        /// <summary>
        ///1:商品链接
        /// </summary>
        public int type { get; set; }
        public string url { get; set; }
    }
}
