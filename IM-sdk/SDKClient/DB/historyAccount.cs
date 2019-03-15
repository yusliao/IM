using SQLite;
using System;


namespace SDKClient.DB
{
    /// <summary>
    /// 历史账户
    /// </summary>
    public class historyAccountDB
    {
        /// <summary>
        /// 主键，自增
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [SQLite.Unique]
        /// <summary>
        /// 登录ID
        /// </summary>
        public string loginId { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// 登录模式
        /// </summary>
        public Model.LoginMode loginModel { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string headPic { get; set; }
        /// <summary>
        /// 上次登录时间
        /// </summary>
        public DateTime? lastlastLoginTime { get; set; }
        public DateTime? GetOffLineMsgTime { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// token首次有效时间
        /// </summary>
        public DateTime? FirstLoginTime { get; set; }
        public DateTime? TopMostTime { get; set; }
        public string userName { get; set; }
        public string token { get; set; }



    }
}
