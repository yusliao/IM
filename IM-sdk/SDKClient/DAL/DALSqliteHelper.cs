using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALSqliteHelper
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static void CreatePersonDB(int userId)
        {
            try
            {
                var file = Path.Combine(SDKProperty.dbPath, $"{userId}.db");
                if (!Directory.Exists(Path.GetDirectoryName(file)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                }
                Util.Helpers.Async.Run(async () =>
                {

                    SDKProperty.SQLiteConn = new SQLiteAsyncConnection(file);
                    SDKProperty.SQLiteReader = new SQLiteAsyncConnection(file);
                    
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.messageDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.GroupInfo>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.contactListDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.ContactDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.friendApplyItem>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.groupListDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.BigTxtPackageDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.ChatRoomConfig>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.JoinGroupDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.groupMemberListDB>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.ResourceManifest>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.GroupMemberInfo>();
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.OffLineMsgTask>();
                   
                    await SDKProperty.SQLiteConn.CreateTableAsync<DB.StrangerInfoDB>();

                    logger.Info($"{userId}的数据库创建成功！");
                });
            }
            catch (Exception e)
            {
                logger.Error($"{userId}数据库创建失败:{e.Message}");
            }
        }
        public static async Task<SQLiteAsyncConnection>  CreateCommDB()
        {
            var file = Path.Combine(SDKProperty.dbPath, "common.db");
            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }
            //if (!File.Exists(file))
            //{
            //    File.Create(file);
            //}
           
            var comconn = new SQLite.SQLiteAsyncConnection(file);

            await comconn.CreateTableAsync<DB.historyAccountDB>();
            return comconn;
        }
    }
}
