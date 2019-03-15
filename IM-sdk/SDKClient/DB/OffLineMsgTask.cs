using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class OffLineMsgTask
    {
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }
        public int roomId { get; set; }
        public bool isGroup { get; set; }
     
        public double deadTime { get; set; }
        public double earlyTime { get; set; }
    }
}
