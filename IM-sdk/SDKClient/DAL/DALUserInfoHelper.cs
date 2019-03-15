using SDKClient.DB;
using SDKClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace SDKClient.DAL
{
    class DALUserInfoHelper
    {
        public static async Task<List<ContactDB>> GetList()
        {
            return await SDKProperty.SQLiteConn.Table<ContactDB>().ToListAsync();
        }
        public static async Task<ContactDB> Get(int userId)
        {
            return await SDKProperty.SQLiteConn.FindAsync<DB.ContactDB>(userId);
        }
        public static async Task InsertOrUpdateItem(AddFriendPackage package)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ContactDB>(package.data.userId);
            if (item != null && item.UserId != 0)
            {
                item.State = 0;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
            else
            {
                item = new DB.ContactDB();
                item.NickName = package.data.userName;
                item.Sex = package.data.sex;
                item.Area = package.data.province + "," + package.data.city;


                item.HeadImgMD5 = package.data.photo;
                item.UserId = package.data.userId;
                item.State = 0;
                try
                {
                    await SDKProperty.SQLiteConn.InsertAsync(item);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

                }

            }
        }
        public static async Task InsertOrUpdateItem(CustomServicePackage package)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ContactDB>(package.from.ToInt());
            if (item != null && item.UserId != 0)
            {
                item.HeadImgMD5 = package.data.photo;
                item.Mobile = package.data.mobile;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
            else
            {
                item = new DB.ContactDB();
               

                item.HeadImgMD5 = package.data.photo;
                item.Mobile = package.data.mobile;
                item.UserId = package.from.ToInt();
                try
                {
                    await SDKProperty.SQLiteConn.InsertAsync(item);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

                }

            }
        }
        public static async Task UpdateItemIsChecked(int userId)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ContactDB>(userId);
            if (item != null && item.UserId != 0)
            {
                item.State = 1;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
           
        }

        public static async Task<bool> UpdateRecord(ContactDB user)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ContactDB>(user.UserId);
            if(item!=null&&item.UserId!=0)
                await SDKProperty.SQLiteConn.DeleteAsync(item);
            try
            {
                int i = await SDKProperty.SQLiteConn.InsertAsync(user);
                return i > 0 ? true : false;
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");
                return false;
            }

        }
        public static async Task DeleteRecords()
        {
            await SDKProperty.SQLiteConn.DropTableAsync<DB.ContactDB>();
            await SDKProperty.SQLiteConn.CreateTableAsync<DB.ContactDB>();
        }
        public static async Task DeleteItem(int userId)
        {
            var item = await DALUserInfoHelper.Get(userId);
            if (item != null)
            {
                item.State = 0;
                item.Remark = null;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
        }
        public static async Task InsertContactDB(Model.GetContactsListPackage package)
        {
            List<DB.ContactDB> lst = new List<DB.ContactDB>();
            var items = package.data.items;
            if (items != null && items.Any())
            {
                foreach (var contact in items)
                {
                    DB.ContactDB model = new DB.ContactDB();

                    model.UserId = contact.partnerUserId;

                    model.HeadImgMD5 = contact.photo;
                    model.NickName = contact.userName;
                    model.Remark = contact.partnerRemark;
                    model.KfId = contact.kfId;
                    model.Area = contact.province + " " + contact.city;
                    model.Sex = contact.sex;
                    model.Mobile = contact.mobile;
                    model.haveModifiedKfid = contact.haveModifiedKfid;
                    model.friendSource = contact.friendSource;
                    model.sourceGroup = contact.sourceGroup;
                    model.sourceGroupName = contact.sourceGroupName;
                    lst.Add(model);
                }
                try
                {
                    await SDKProperty.SQLiteConn.InsertOrReplaceAsync(lst);
                }
                catch (Exception ex)
                {
                    SDKClient.logger.Error($"消息处理异常：error:{ex.Message},stack:{ex.StackTrace};\r\n");

                }



            }
        }


    }
}
