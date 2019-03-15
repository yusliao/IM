using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    class DALGetUserHelper
    {
        public static async Task<DB.GetUserDB> GetUserPackage(int userId)
        {
            var lst = await SDKProperty.SQLiteConn.Table<DB.GetUserDB>().Where(u => u.UserId == userId).ToListAsync();
            if (lst != null && lst.Any())
                return lst[0];
            else
                return null;
        }
       
    }
}
