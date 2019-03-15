using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DB
{
    class ChatRoomConfig
    {
        [SQLite.PrimaryKey]
        public int RoomId { get; set; }
        /// <summary>
        /// 0:chat;1:groupchat;
        /// </summary>
        public int RoomType { get; set; }
        /// <summary>
        /// 显示
        /// </summary>
        public bool Visibility { get; set; }
        public DateTime? TopTime { get; set; }
        public bool DoNotDisturb { get; set; }

    }
}
