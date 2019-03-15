using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALChatRoomConfigHelper
    {
        public static async Task<List<DB.ChatRoomConfig>> GetListAsync()
        {
            return await SDKProperty.SQLiteConn.Table<DB.ChatRoomConfig>().ToListAsync();
        }
        public static async Task SetRoomHiddenAsync(int roomId)
        {
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ChatRoomConfig>(roomId);
            if (item == null)
            {
                item = new DB.ChatRoomConfig();
                item.RoomId = roomId;
                item.Visibility = false;
                await SDKProperty.SQLiteConn.InsertAsync(item);
            }
            else
            {
              
                item.Visibility = false;
                await SDKProperty.SQLiteConn.UpdateAsync(item);
            }
        }
        public static async Task SetRoomVisiableAsync(int roomId)
        {
          
            var item = await SDKProperty.SQLiteConn.FindAsync<DB.ChatRoomConfig>(roomId);
            if (item != null)
                await SDKProperty.SQLiteConn.DeleteAsync(item);
           

           
        }
    }
}
