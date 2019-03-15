using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALContactListHelper
    {
        public static async Task<DB.contactListDB> GetCurrentContactListPackage()
        {
            try
            {
                return await SDKProperty.SQLiteConn.Table<DB.contactListDB>()?.FirstOrDefaultAsync();
            }
            catch (Exception)
            {
            }
            return null;
           
        }
        public static async Task DeleteCurrentContactListPackage()
        {
            await SDKProperty.SQLiteConn.DropTableAsync<DB.contactListDB>();
            await SDKProperty.SQLiteConn.CreateTableAsync<DB.contactListDB>();

        }
       
    }
}
