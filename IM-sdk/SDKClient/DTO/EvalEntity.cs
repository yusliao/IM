using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    public class EvalEntity
    {
        public string id { get; set; }
        /// <summary>
        /// 1-问，2-答
        /// </summary>
        public int type { get; set; } 
        /// <summary>
        /// [1-很满意 2-满意 3-一般 4-不满意]
        /// </summary>
        public int subType { get; set; }

        /// <summary>
        /// 评价结果
        /// </summary>
        public string result { get; set; }
    }
}
