using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALStrangerOptionHelper
    {
        public static async Task<IList<DTO.StrangerEntity>> GetStrangerEntities()
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.StrangerInfoDB>().ToListAsync();
            if (lst != null && lst.Any())
            {
                return lst.Select(s => new DTO.StrangerEntity() { db = s }).ToList();
            }
            else
                return null;
        }
       
        public static async Task<DTO.StrangerEntity> GetStranger(int userId)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.StrangerInfoDB>(userId);
            if (item != null)
                return new DTO.StrangerEntity() { db = item };
            else return null;
        }
        public static async Task<bool> DeleteStranger(int userId)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.StrangerInfoDB>(userId);
            if (item != null)
            {
                int i = await SDKProperty.SQLiteConn.DeleteAsync(item);
                return i > 0 ? true : false;
            }
            else return true;
        }
        public static async Task<bool> InsertOrUpdateStrangerInfo(int userId,string photo,string name,int sex)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.StrangerInfoDB>(userId);
            if(item!=null)
            {
                item.HeadImgMD5 = photo;
                item.NickName = name;
                item.Sex = sex;
                try
                {
                    int i = await SDKProperty.SQLiteConn.UpdateAsync(item);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                DB.StrangerInfoDB strangerInfoDB = new DB.StrangerInfoDB()
                {
                    UserId = userId,
                    Sex = sex,
                    NickName = name,
                    HeadImgMD5 = photo
                };
                try
                {
                    int i = await SDKProperty.SQLiteConn.InsertAsync(strangerInfoDB);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public static async Task<bool> SetStrangerdoNotDisturb(int userId,int disturbOption)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.StrangerInfoDB>(userId);
            if (item != null)
            {
                item.doNotDisturb = disturbOption;
                try
                {
                    int i = await SDKProperty.SQLiteConn.UpdateAsync(item);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                DB.StrangerInfoDB strangerInfoDB = new DB.StrangerInfoDB()
                {
                    UserId = userId,
                    doNotDisturb = disturbOption
                };
                try
                {
                    int i = await SDKProperty.SQLiteConn.InsertAsync(strangerInfoDB);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public static async Task<bool> SetStrangerChatTopTime(int userId,DateTime? datetime)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.StrangerInfoDB>(userId);
            if (item != null)
            {
                item.ChatTopTime = datetime;
                try
                {
                    int i = await SDKProperty.SQLiteConn.UpdateAsync(item);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                DB.StrangerInfoDB strangerInfoDB = new DB.StrangerInfoDB()
                {
                    UserId = userId,
                    ChatTopTime = datetime
                };
                try
                {
                    int i = await SDKProperty.SQLiteConn.InsertAsync(strangerInfoDB);
                    return i > 0 ? true : false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
      
    }
}
