using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SDKClient.DAL
{
    public class DALAccount
    {
        public static async Task<string> RegisterAccount(string account)
        {
            return await new Util.Webs.Clients.WebClient().Post(Protocol.ProtocolBase.registerUri)
                 .Data("userMobile", account)
                  .Data("verificationCode", 816316)
                 .ContentType(Util.Webs.Clients.HttpContentType.Json)
                 .ResultAsync();
            //入库


        }
        public static async Task<bool> AddAccount(DB.historyAccountDB account)
        {

            return await SDKProperty.SQLiteComConn.InsertAsync(account) > 0 ? true : false;

        }
        public static async Task<List<DB.historyAccountDB>> GetAccountListDESC()
        {
            try
            {
                var l = await SDKProperty.SQLiteComConn.Table<DB.historyAccountDB>().OrderByDescending(h => h.lastlastLoginTime).ToListAsync();
                var tempList = l.ToList();
                if (tempList != null && tempList.Any())
                {
#if CUSTOMSERVER
                    tempList.Where(m => !string.IsNullOrEmpty(m.password)).ToList().ForEach(h => h.password = Util.Helpers.Encrypt.DesDecrypt(h.password));
#endif
                    return tempList;
                }
                else
                    return new List<DB.historyAccountDB>();
            }
            catch (Exception ex)
            {
                SDKClient.logger.Error(ex.Message);
                return new List<DB.historyAccountDB>();
            }


        }
        public static async Task UpdateAccount(Model.Account account)
        {
            await SDKProperty.SQLiteComConn.GetAsync<DB.historyAccountDB>(h => h.loginId == account.loginId).ContinueWith(async t =>
            {
                if (!t.IsFaulted)
                {

                    if ((t.Result.loginModel & account.LoginMode) == Model.LoginMode.None)
                    {
                        if ((int)t.Result.loginModel + (int)account.LoginMode >= (int)Model.LoginMode.All)
                            t.Result.loginModel = Model.LoginMode.All;
                        else
                            t.Result.loginModel = account.LoginMode;
                    }
                    if (!string.IsNullOrEmpty(account.userPass))
                        t.Result.password = Util.Helpers.Encrypt.DesEncrypt(account.userPass);
                    t.Result.lastlastLoginTime = account.lastlastLoginTime;
                    if (account.token != t.Result.token ||t.Result.FirstLoginTime==null)
                    {
                        t.Result.FirstLoginTime = account.lastlastLoginTime;
                        t.Result.token = account.token;
                    }
                    t.Result.userName = account.userName;
                    t.Result.headPic = account.photo;
                    t.Result.UserId = account.userID;
                    account.GetOfflineMsgTime = t.Result.GetOffLineMsgTime;
                    await SDKProperty.SQLiteComConn.UpdateAsync(t.Result);
                }
                else
                {

                    var accountList = await SDKProperty.SQLiteComConn.Table<DB.historyAccountDB>().ToListAsync();


                    await SDKProperty.SQLiteComConn.InsertAsync(new DB.historyAccountDB()
                    {
                        lastlastLoginTime = account.lastlastLoginTime,
                        FirstLoginTime = account.lastlastLoginTime,
                        loginId = account.loginId,
                        loginModel = account.LoginMode,
                        headPic = account.photo,
                        token = account.token,
                        UserId = account.userID,
                        userName = account.userName,
                        password = Util.Helpers.Encrypt.DesEncrypt(account.userPass)

                    });
                }
            });
        }
        public static void UpdateAccountLoginModel(Model.LoginMode loginMode)
        {
            SDKProperty.SQLiteComConn.GetAsync<DB.historyAccountDB>(h => h.loginId == SDKClient.Instance.property.CurrentAccount.loginId).ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {

                    t.Result.loginModel = loginMode;

                    SDKProperty.SQLiteComConn.UpdateAsync(t.Result).Wait();
                }

            });
        }
        public static async void UpdateAccountOfflineMsgTime(DateTime? time)
        {
            await SDKProperty.SQLiteComConn.GetAsync<DB.historyAccountDB>(h => h.loginId == SDKClient.Instance.property.CurrentAccount.loginId).ContinueWith(async t =>
            {
                if (!t.IsFaulted)
                {

                    t.Result.GetOffLineMsgTime = time;

                    await SDKProperty.SQLiteComConn.UpdateAsync(t.Result);
                }

            });
        }
        public static void UpdateAccountTopMostTime(DateTime? time)
        {
            SDKProperty.SQLiteComConn.GetAsync<DB.historyAccountDB>(h => h.loginId == SDKClient.Instance.property.CurrentAccount.loginId).ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {

                    t.Result.TopMostTime = time;

                    SDKProperty.SQLiteComConn.UpdateAsync(t.Result).Wait();
                }

            });
        }
        public async static Task<DB.historyAccountDB> GetAccount()
        {
            return await SDKProperty.SQLiteComConn.FindAsync<DB.historyAccountDB>(h => h.loginId == SDKClient.Instance.property.CurrentAccount.loginId);

        }
        public static async Task<bool> DeleteAccount(string account)
        {
            var t = await SDKProperty.SQLiteComConn.Table<DB.historyAccountDB>().Where(h => h.loginId == account).ToListAsync();

            if (t.Any())
            {
                return await SDKProperty.SQLiteComConn.DeleteAsync(t.First()) > 0 ? true : false;

            }
            else
                return false;
        }


    }
}
