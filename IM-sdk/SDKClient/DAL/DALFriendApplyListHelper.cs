using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALFriendApplyListHelper
    {
        private static readonly object obj_lock = new object();
        public static async Task<List<DB.friendApplyItem>> GetFriendApplyList()
        {
            List<DB.friendApplyItem> lst = new List<DB.friendApplyItem>();
            try
            {
                lst = await SDKProperty.SQLiteConn.Table<DB.friendApplyItem>().OrderByDescending(f => f.TopTime).ToListAsync();
            }
            catch (Exception)
            {

            }
            return lst;
        }
        public static async Task InsertOrUpdateItem(Model.AddFriendPackage package)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.friendApplyItem>().Where(f => f.userId == package.data.userId).ToListAsync();
            if (lst.Any())
            {
                lst[0].IsRead = false;
                lst[0].name = package.data.userName;
                lst[0].sex = package.data.sex;
                lst[0].province = package.data.province;
                lst[0].city = package.data.city;
                lst[0].photo = package.data.photo;
                lst[0].TopTime = package.time ?? DateTime.Now;
                lst[0].IsChecked = false;
                lst[0].friendApplyId = package.data.friendApplyId;
                lst[0].applyRemark = package.data.applyRemark;
                lst[0].msgId = package.id;
                lst[0].time = package.time;
                lst[0].friendSource = package.data.friendSource;
                lst[0].sourceGroup = package.data.sourceGroup;
                lst[0].sourceGroupName = package.data.sourceGroupName;
                await SDKProperty.SQLiteConn.UpdateAsync(lst[0]);
            }
            else
            {

                DB.friendApplyItem item = new DB.friendApplyItem();
                item.name = package.data.userName;
                item.IsRead = false;
                item.sex = package.data.sex;
                item.province = package.data.province;
                item.city = package.data.city;
                item.TopTime = package.time ?? DateTime.Now;
                item.photo = package.data.photo;
                item.userId = package.data.userId;
                item.IsChecked = false;
                item.friendApplyId = package.data.friendApplyId;
                item.applyRemark = package.data.applyRemark;
                item.friendSource = package.data.friendSource;
                item.sourceGroup = package.data.sourceGroup;
                item.msgId = package.id;
                item.time = package.time;
                item.sourceGroupName = package.data.sourceGroupName;
                await SDKProperty.SQLiteConn.InsertAsync(item);
            }




        }
        public static async Task UpdateTableIsRead()
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.friendApplyItem>().ToListAsync();
            if (lst != null && lst.Count > 0)
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    lst[i].IsRead = true;
                }
                int count = await SDKProperty.SQLiteConn.UpdateAllAsync(lst);
                count.ToString();
            }
        }
        public static async Task UpdateItemIsChecked(int userId)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.friendApplyItem>().Where(f => f.userId == userId).ToListAsync();
            if (lst != null && lst.Any())
            {
                lst[0].IsChecked = true;
                int i = await SDKProperty.SQLiteConn.UpdateAsync(lst[0]);

            }

        }
        public static async Task DeleteItem(int userId)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.friendApplyItem>().Where(f => f.userId == userId).ToListAsync();
            if (lst != null && lst.Any())
            {
                foreach (var item in lst)
                {
                    await SDKProperty.SQLiteConn.DeleteAsync(item);
                }
            }

        }
    }
}
