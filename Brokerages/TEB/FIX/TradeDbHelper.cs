using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.DAL;
using Teb.FIX.Model;

namespace QuantConnect.Brokerages.TEB.FIX
{
    public class TradeDbHelper
    {
        public string ProviderName { get; set; }
        public string ConnectionString { get; set; }

        public TradeDbHelper()
        {

        }

        public TradeDbHelper(string connString)
        {
            ConnectionString = connString;
        }

        

        public void UpdateOrder(Order order)
        {
            DBHelper dbHelper = new DBHelper();
            dbHelper.ConnectionString = ConnectionString;
            dbHelper.ProviderName = ProviderName;
            dbHelper.UpdateOrder(order);
        }

        public DataSet GetSessionSetting(string appName, string senderCompId, string targetCompId)
        {
            DBHelper dbHelper = new DBHelper();
            dbHelper.ConnectionString = ConnectionString;
            return dbHelper.GetSessionSettingDataSet(appName, senderCompId, targetCompId);
        }


        public DataSet GetCashFixMessages(long messageId, string userName, string applicationName)
        {
            DBHelper dbHelper = new DBHelper();
            dbHelper.ConnectionString = ConnectionString;
            return dbHelper.GetCashMessageDataSet(messageId, userName, applicationName);

        }

        public DataSet GetFutureFixMessages(long messageId, string userName, string applicationName)
        {
            DBHelper dbHelper = new DBHelper();
            dbHelper.ConnectionString = ConnectionString;
            return dbHelper.GetFutureMessageDataSet(messageId, userName, applicationName);
        }


        //public void RefreshUserSessionList(ConcurrentDictionary<string, ConcurrentBag<string>> userSessionList)
        //{
        //    DBHelper dbHelper = new DBHelper();
        //    dbHelper.ConnectionString = ConnectionString;
        //    DataSet dsUserSessions = dbHelper.GetUserSessionDataSet("", "", "");
        //    DataTable dtUserSessions = dsUserSessions.Tables[0];
        //    foreach (DataRow dr in dtUserSessions.Rows)
        //    {
        //        string userName = dr["USERNAME"].ToString();
        //        string connectionId = dr["ConnectionId"].ToString();
        //        userSessionList.AddOrUpdate(userName, new ConcurrentBag<string>() { connectionId }, (k, v) =>
        //        {
        //            v.Add(connectionId);
        //            return v;
        //        });
        //    }
        //}

    }

}
