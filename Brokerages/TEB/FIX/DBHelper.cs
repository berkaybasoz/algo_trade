using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.DAL;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.FIX
{
    public class DBHelper
    {
        public string ConnectionString { get; set; }

        public string ProviderName { get; set; }



        public static object ToDbNull(object o)
        {
            if (o == null)
            {
                return DBNull.Value;
            }
            else
            {
                return o;
            }
        }

        public bool IsEndOfDay()
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 2);
            bool isEndOfDay = dbManager.ToScalar<bool>("sel_FIXAppParameter", true, pList);
            return isEndOfDay;
        }

        public DataSet GetUserSessionDataSet(string username, string password, string ip)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 2);
            pList.Add("UserName", username);
            pList.Add("Password", password);
            pList.Add("IP", ip);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppUsers", true, pList);
            return ds;
        }

   

        public DataSet GetNasdaqUserConfigDataSet(int kriter, string userName, string key, string groupKey)
        {
            return ToNasdaqUserConfigsDataSet(kriter, userName, key, groupKey);
        }

        public DataSet GetCashMessageDataSet(long LastId)
        {
            return GetCashMessageDataSet(LastId, "", "", 4);
        }

        public DataSet GetCashMessageDataSet(long messageId, string userName, string applicationName, int kriter = 1)
        {
            return ToFixCashMessagesDataSet(messageId, userName, applicationName, kriter);
        }

        public DataSet GetFutureMessageDataSet(long LastId)
        {
            return GetFutureMessageDataSet(LastId, "", "", 4); ;
        }

        public DataSet GetFutureMessageDataSet(long messageId, string userName, string applicationName, int kriter = 1)
        {
            return ToFixFutureMessagesDataSet(messageId, userName, applicationName, kriter);
        }

        public DataSet GetSessionSettingDataSet(string appName, string senderCompId, string targetCompId)
        {
            return ToSessionSettingDataSet(appName, senderCompId, targetCompId);
        }

        public DataSet GetCashOrderDataSet(long LastId)
        {
            return GetCashOrdersDataSet(1, LastId);
        }

        public DataSet GetCashOrdersDataSet(int criteria, long messageId)
        {
            return ToFixCashOrdersDataSet(criteria, messageId);
        }

        public DataSet GetFutureOrderDataSet(long LastId)
        {
            return GetFutureOrdersDataSet(2, LastId);
        }

        public DataSet GetFutureOrdersDataSet(int criteria, long messageId)
        {
            return ToFixFutureOrdersDataSet(criteria, messageId);
        }

        public DataSet GetUserDataSet(string username, string password, string ip)
        {
            return ToUserDataSet(username, password, ip);
        }

        public DataSet GetParameters(string key)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 1);
            pList.Add("Key", key);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppParameter", true, pList);
            return ds;
        }

        public void UpdateOrder(Teb.FIX.Model.Order order)
        {
            DbQueryManager dbQueryManager = new DbQueryManager(ConnectionString, ProviderName);
            dbQueryManager.OpenConnection();

            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 1);
            pList.Add("State", order.State == null ? -1 : order.State.GetStateID());
            pList.Add("IsGlobal", order.IsGlobal);
            pList.Add("MessageId", order.DbID);
            pList.Add("TEBID", order.TEBID);
            pList.Add("UpdateDateTime", DateTime.Now);
            pList.Add("ClOrdIncrement", order.ClOrdIncrement);
            pList.Add("MarketClOrdID", order.MarketClOrdId);
            pList.Add("MarketOrdID", order.MarketOrdID);
            pList.Add("ConnectionID", order.ConnectionID);
            pList.Add("ConnectionClOrdID", order.ConnectionClOrdID);
            pList.Add("ConnectionOrigClOrdID", order.ConnectionOrigClOrdID);
            pList.Add("ClOrdID", order.ClOrdID);
            pList.Add("OrigClOrdID", order.OrigClOrdID);
            pList.Add("OrderID", order.OrderID);
            pList.Add("SecurityType", order.SecurityType);
            pList.Add("Afe", order.Afe);
            pList.Add("Kafe", order.Kafe);
            pList.Add("SellShort", order.IsSellShort);
            pList.Add("Account", order.Account);
            pList.Add("AccountSubNo", order.AccountSubNo);
            pList.Add("PartyID", order.PartyID);
            //pList.Add("AccountType", order.AccountType);
            pList.Add("ServerStatusID", (int)order.ServerStatus);
            pList.Add("ClientStatusID", (int)order.ClientStatus);
            pList.Add("HistoryStatusID", (int)order.HistoryStatus);
            pList.Add("IsUIActive", order.IsActive);

            pList.Add("TimeInForce", order.Core.TimeInForce);
            pList.Add("OrgTimeInForce", order.Core.OrgTimeInForce);
            pList.Add("ExpireDate", order.ExpireDate);
            pList.Add("Side", order.Core.Side);
            pList.Add("Symbol", order.Symbol);
            pList.Add("SymbolSfx", order.SymbolSfx);

            if (order.TransactTime.HasValue)
            {
                pList.Add("TransactTime", order.TransactTime.Value);
            }
            if (order.SendingTime.HasValue)
            {
                pList.Add("SendingTime", order.SendingTime.Value);
            }
            if (order.Price.HasValue)
            {
                pList.Add("Price", order.Price.Value);
            }
            if (order.Price2.HasValue)
            {
                pList.Add("Price2", order.Price2.Value);
            }
            if (order.TriggerPrice.HasValue)
            {
                pList.Add("TriggerPrice", order.TriggerPrice.Value);
            }
            if (order.OrderQty.HasValue)
            {
                pList.Add("OrderQty", order.OrderQty.Value);
            }
            if (order.OrderQty2.HasValue)
            {
                pList.Add("OrderQty2", order.OrderQty2.Value);
            }
            if (order.CumQty.HasValue)
            {
                pList.Add("CumQty", order.CumQty.Value);
            }
            if (order.AvgPx.HasValue)
            {
                pList.Add("AvgPx", order.AvgPx.Value);
            }
            if (order.LeavesQty.HasValue)
            {
                pList.Add("LeavesQty", order.LeavesQty.Value);
            }
            if (order.GrossTradeAmt.HasValue)
            {
                pList.Add("GrossTradeAmount", order.GrossTradeAmt.Value);
            }
            if (order.LastQty.HasValue)
            {
                pList.Add("LastQty", order.LastQty.Value);
            }
            if (order.LastPx.HasValue)
            {
                pList.Add("LastPx", order.LastPx.Value);
            }
            pList.Add("SenderCompID", order.SenderCompID);
            pList.Add("TargetCompID", order.TargetCompID);
            pList.Add("MarketSegmentID", order.Core.MarketSegmentID);
            pList.Add("PositionEffect", order.PositionEffect);
            pList.Add("Text", order.Text);
            pList.Add("EncodedText", order.EncodedText);
            pList.Add("OrdType", order.Core.OrdType);
            pList.Add("MarketSourceID", order.MarketSource == null ? "-1" : order.MarketSource.Id);
            pList.Add("InstructionSourceID", order.InstructionSource == null ? "-1" : order.InstructionSource.Id);

            pList.Add("ConnectionFirstClOrdID", order.ConnectionFirstClOrdID);
            pList.Add("MaxFloor", order.MaxFloor);
            pList.Add("PegPriceType", order.PegPriceType);
            pList.Add("FirstClOrdID", order.FirstClOrdID);
            dbQueryManager.Execute("up_FIXAppOrders", true, pList);
            dbQueryManager.CloseConnection();
        }

        public void WriteLog(Teb.Infra.Model.ServiceLog log)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("LogId", ToDbNull(log.LogId));
            pList.Add("AppName", ToDbNull(log.AppName));
            pList.Add("IP", ToDbNull(log.IP));
            pList.Add("UserName", ToDbNull(log.UserName));
            pList.Add("Category", ToDbNull(log.Category));
            pList.Add("Title", ToDbNull(log.Title));
            pList.Add("Message", ToDbNull(log.Message));
            pList.Add("Description", ToDbNull(log.Description));
            pList.Add("ExceptionMessage", ToDbNull(log.ExceptionMessage));
            pList.Add("ExceptionStackTrace ", ToDbNull(log.ExceptionStackTrace));
            pList.Add("LogTime", ToDbNull(log.Time));
            dbManager.Execute("up_FIXAppLogs", true, pList);
        }

        public bool IsDbAvailable()
        {
            DbManager dbManager = new DbManager(ConnectionString, ProviderName);
            return dbManager.TestConnection();

        }

        public void SaveUserConfig(Teb.Infra.Model.UserConfig uc)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 1);
            pList.Add("UserName", ToDbNull(uc.UserName));
            pList.Add("ConfigKey", ToDbNull(uc.Key));
            pList.Add("ConfigValue", ToDbNull(uc.Value));
            pList.Add("ConfigGroupKey", ToDbNull(uc.GroupKey));
            dbManager.Execute("up_FIXAppUserConfigs", true, pList);
        }

        public void SaveParameter(Parameter p)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 1);
            pList.Add("Key", ToDbNull(p.Key));
            pList.Add("Value", ToDbNull(p.Value));
            pList.Add("KeyGroup", ToDbNull(p.KeyGroup));
            pList.Add("Definition", ToDbNull(p.Definition));
            dbManager.Execute("up_FIXAppParameter", true, pList);
        }

        private void SaveNasdaqUserConfigsDataSet(int kriter, string userName, string key, string groupKey)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", kriter);
            pList.Add("UserName", userName);
            pList.Add("Key", key);
            pList.Add("GroupKey", groupKey);
            dbManager.Execute("up_FIXAppUserConfigs", true, pList);

        }




       
 

        private DataSet ToNasdaqUserConfigsDataSet(int kriter, string userName, string key, string groupKey)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", kriter);
            pList.Add("UserName", userName);
            pList.Add("Key", key);
            pList.Add("GroupKey", groupKey);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppUserConfigs", true, pList);
            return ds;
        }

       

        private DataSet ToSessionSettingDataSet(string appName, string senderCompId, string targetCompId)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();

            pList.Add("ApplicationName", appName);

            if (string.IsNullOrEmpty(senderCompId))
            {
                pList.Add("SenderCompID", DBNull.Value);
            }
            else
            {
                pList.Add("SenderCompID", senderCompId);
            }

            if (string.IsNullOrEmpty(targetCompId))
            {
                pList.Add("TargetCompID", DBNull.Value);
            }
            else
            {
                pList.Add("TargetCompID", targetCompId);
            }

            DataSet ds = dbManager.ToDataSet("sel_FIXAppConfigs", true, pList);
            return ds;


        }

        private DataSet ToFixCashOrdersDataSet(int criteria, long messageId)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", criteria);
            pList.Add("MessageId", messageId);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppOrders", true, pList);
            return ds;
        }

        private DataSet ToFixFutureOrdersDataSet(int criteria, long messageId)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", criteria);
            pList.Add("MessageId", messageId);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppOrders", true, pList);
            return ds;
        }

        private DataSet ToFixCashMessagesDataSet(long messageId = 0, string userName = null, string applicationName = null, int kriter = 1)
        {

            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", kriter);


            if (userName == null)
            {
                pList.Add("UserName", DBNull.Value);
            }
            else
            {
                pList.Add("UserName", userName);
            }


            if (applicationName == null)
            {
                pList.Add("ApplicationName", DBNull.Value);
            }
            else
            {
                pList.Add("ApplicationName", applicationName);
            }

            pList.Add("Id", messageId);
            DataSet ds = dbManager.ToDataSet("sel_FIXMessage", true, pList);
            return ds;

        }

        private DataSet ToFixFutureMessagesDataSet(long messageId = 0, string userName = null, string applicationName = null, int kriter = 1)
        {

            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", kriter);


            if (userName == null)
            {
                pList.Add("UserName", DBNull.Value);
            }
            else
            {
                pList.Add("UserName", userName);
            }


            if (applicationName == null)
            {
                pList.Add("ApplicationName", DBNull.Value);
            }
            else
            {
                pList.Add("ApplicationName", applicationName);
            }

            pList.Add("Id", messageId);
            DataSet ds = dbManager.ToDataSet("sel_FIXMessage", true, pList);
            return ds;

        }

        private DataSet ToUserDataSet(string username, string password, string ip)
        {
            DbManager dbManager = new DbManager(ConnectionString);
            Dictionary<string, object> pList = new Dictionary<string, object>();
            pList.Add("Kriter", 1);
            pList.Add("UserName", username);
            pList.Add("Password", password);
            pList.Add("IP", ip);
            DataSet ds = dbManager.ToDataSet("sel_FIXAppUsers", true, pList);
            return ds;
        }



        //public static DbManager DBdependencyNotification;
        //public void StartNotificationDependency(Action<SqlNotificationEventArgs> OnDataChange)
        //{ 
        //    SqlDependency.Start(ConnectionString);

        //    DBdependencyNotification = new DbManager(ConnectionString);
        //    DBdependencyNotification.EnableSqlDependency = true;
        //    DBdependencyNotification.OnDataChange += OnDataChange;
        //    //dbdependencyNotification.ToDataSet("sel_FIXReplicationData", true, new Dictionary<string, object> { { "Kriter", 2 } });
        //    DBdependencyNotification.RegisterDependency("select  [SourceName],[SourceIndex1],[SourceIndex2],[SourceIndex3],[Status],[UpdateNumber],[UpdateTime],[ReplicationData],[TimeStamp] from dbo.FIXReplicationData ", false);
        //    // dbdependencyNotification.RegisterDependency("sel_FIXReplicationData", true, new Dictionary<string, object> { { "Kriter", 2 } });


        //}

        //public   void StopNotificationDependency()
        //{
        //    SqlDependency.Stop(ConnectionString);

        //}

    }
}
