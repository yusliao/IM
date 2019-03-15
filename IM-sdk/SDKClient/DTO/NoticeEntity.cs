using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DTO
{
    public class NoticeEntity
    {
      
        internal WebAPI.GetNoticeListResponse.NoticeInfo db;
        
        public int groupId => db.groupId;
      
        public string title => db.title;
        public string content => db.content;

        public int noticeId => db.noticeId;
        /// <summary>
        /// 公告类型 0：普通公告，1：入群须知
        /// </summary>
        public int type => db.type;
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime? publishTime => db.publishTime;
    }
}
