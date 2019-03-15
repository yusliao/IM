using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.P2P
{
    public class FileHead
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        public string MD5 { get; set; }
        public string MsgId { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public long FileSize { get; set; }
    }
    class InfoBody
    {
        public string FileName { get; set; }
        public string MD5 { get; set; }
        public int Index { get; set; }
        public int Total { get; set; }
        public int BodyLen { get; set; }
        public byte[] Body { get; set; }
    }
    
    public class P2PPackage
    {
        public int RoomId { get; set; }
        public string MsgId { get; set; }
        public string MD5 { get; set; }
        public string FileName { get; set; }
        public P2PPakcageState PackageCode { get; set; }
    }
    public enum P2PPakcageState
    {
        none=0,
        stop,
        @continue,
        complete,
        cancel
    }
    public class NotificatonPackage: FileHead
    {
        
        public bool IsSuccess { get; set; }
        public string Content { get; set; }
        public SDKProperty.ErrorState ErrorCode { get; set; }

        public string Error { get; set; }

    }
}
