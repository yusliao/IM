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
    class DALResourceManifestHelper
    {
        public static async Task<List<ResourceManifest>> GetList()
        {
            return await SDKProperty.SQLiteConn.Table<ResourceManifest>().ToListAsync();
        }
        public static async Task<ResourceManifest> Get(string md5)
        {
            return await SDKProperty.SQLiteConn.FindAsync<DB.ResourceManifest>(md5);
        }
        public static async Task InsertOrUpdateItem(ResourceManifest resource)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ResourceManifest>(resource.MD5);
            if (item != null && !string.IsNullOrEmpty(item.MD5))
            {
                item.State = resource.State;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
            else
            {
                item = new DB.ResourceManifest();
                item.MD5 = resource.MD5;
                item.Size = resource.Size;
                item.State = resource.State;
             
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
        public static async Task UpdateResourceState(string id, SDKProperty.ResourceState state)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ResourceManifest>(id);
            if (item != null && !string.IsNullOrEmpty(item.MD5))
            {
                item.State = (int)state;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
            else
            {
                item = new DB.ResourceManifest();
                item.MD5 = id;
                item.State = (int)state;

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



    }
}
