using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Teb.FIX.Infra.Context;
using Teb.FIX.Infra.Session;
using Teb.Infra;
using Teb.Infra.Model;
using Teb.Infra.Extension;
using Teb.FIX.Model;
using Teb.FIX.Infra.Builder;
using Teb.FIX.Infra.App;
using QuantConnect.Logging;
namespace QuantConnect.Brokerages.TEB.FIX
{
    public class FIXContext
    {
        public event Action<Exception> OnException;
        private StoreType appStoreType = StoreType.Sql;

        private BaseAppContext cashAppContext;
        private BaseAppContext viopAppContext;

        public StoreType AppStoreType
        {
            get { return appStoreType; }
            set { appStoreType = value; }
        }

        public BaseAppContext CashAppContext
        {
            get
            {
                return cashAppContext;
            }
            set
            {
                cashAppContext = value;
            }
        }

        public BaseAppContext VIOPAppContext
        {
            get
            {
                return viopAppContext;
            }
            set
            {
                viopAppContext = value;

            }
        }

        public void Disconnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 
           
            try
            {
                if (CashAppContext != null)
                {
                    CashAppContext.Logout();
                }
            }
            catch (Exception ex)
            {
                TBYAppContextFIXDisconnectException tex = new TBYAppContextFIXDisconnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "Hisse FIX bağlantısı sonlandırılırken hata oluştu", ex);
                Exception(tex);
            }

            try
            {
                if (VIOPAppContext != null)
                {
                    VIOPAppContext.Logout();
                }
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYAppContextFIXDisconnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "VIOP FIX bağlantısı sonlandırılırken hata oluştu", ex);
                Exception(tex);
            }

               Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void Connect(System.Collections.Generic.List<Teb.FIX.Model.Order> cashOrders, System.Collections.Generic.List<Teb.FIX.Model.Order> futureOrders)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); });

            Task.Factory.StartNew(() =>
            {
                //bool state = false;
                try
                {
                    if (CurrentUser.Instance.User != null)
                    {
                        if (CurrentUser.Instance.User.AutoConnectToCash && CashAppContext != null)
                        {
                            CashAppContext.LogonWithOrders(cashOrders);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "Hisse FIX bağlantısı kurulurken hata oluştu", ex);
                    Exception(tex);
                }

            });

            Task.Factory.StartNew(() =>
            {
                //bool state = false;
                try
                {

                    if (CurrentUser.Instance.User != null)
                    {
                        if (CurrentUser.Instance.User.AutoConnectToFuture && VIOPAppContext != null)
                        {

                            VIOPAppContext.LogonWithOrders(futureOrders);

                        }
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "VIOP FIX bağlantısı kurulurken hata oluştu", ex);
                    Exception(tex);
                }
            });

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void Connect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            Task.Factory.StartNew(() =>
            {
                //bool state = false;
                try
                {
                    if (CurrentUser.Instance.User != null)
                    {
                        if (CurrentUser.Instance.User.AutoConnectToCash && CashAppContext != null)
                        {
                            CashAppContext.Logon();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "Hisse FIX bağlantısı kurulurken hata oluştu", ex);
                    Exception(tex);
                }

            });

            Task.Factory.StartNew(() =>
            {
                //bool state = false;
                try
                {

                    if (CurrentUser.Instance.User != null)
                    {
                        if (CurrentUser.Instance.User.AutoConnectToFuture && VIOPAppContext != null)
                        {

                            VIOPAppContext.Logon();

                        }
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "VIOP FIX bağlantısı kurulurken hata oluştu", ex);
                    Exception(tex);
                }
            });

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void Clear()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (CashAppContext != null && CashAppContext.OrderHandler != null)
                    {
                        CashAppContext.Clear();
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "Hisse emirleri silinirken hata oluştu", ex);
                    Exception(tex);
                }

            });

            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (VIOPAppContext != null && VIOPAppContext.OrderHandler != null)
                    {
                        VIOPAppContext.Clear();
                    }
                }
                catch (Exception ex)
                {
                    TBYException tex = new TBYAppContextFIXConnectException("5029", MethodBase.GetCurrentMethod().GetFullName(), "VIOP emirleri silinirken hata oluştu", ex);
                    Exception(tex);
                }
            });

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void Dispose()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            try
            { 
                CashAppContext.Dispose();
                VIOPAppContext.Dispose();

                CashAppContext = null;
                VIOPAppContext = null;
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYAppContextStopException("5035", MethodBase.GetCurrentMethod().GetFullName(), "FIXContext Dispose hatası oluştu", ex);
                Exception(tex);

            }

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void ReceviedFIXMessage(FixMessage fm)
        {
            if (fm.SecurityType == CashDefinition.SECURITY_CASH)
            {
                QuickFix.Message msg = QMessageBuilder.BuildDbMessage(fm.Message, fm.DbID, fm.TEBID, CashAppContext.ValidateMessage);
                if (CashAppContext != null && CashAppContext.App != null)
                    CashAppContext.App.Recieve(msg);
            }
            else if (fm.SecurityType == CashDefinition.SECURITY_FUTURE || fm.SecurityType == CashDefinition.SECURITY_OPTION)
            {
                QuickFix.Message msg = QMessageBuilder.BuildDbMessage(fm.Message, fm.DbID, fm.TEBID, VIOPAppContext.ValidateMessage);
                if (VIOPAppContext != null && VIOPAppContext.App != null)
                    VIOPAppContext.App.Recieve(msg);

            }
        }

        public void CashConnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.Logon();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void CashDisconnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.Logout();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void CashConnectOrDisconnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started",MethodInfo.GetCurrentMethod().GetFullName() )); }); 
            
            if (CanLogout(CashAppContext))
            {
                CashAppContext.Logout();
            }
            else if (CanLogon(CashAppContext))
            {
                CashAppContext.Logon();
            }

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); }); 
        }

        public void CashLoadOrders()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.LoadOrders();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); }); 
        }

        public void VIOPConnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            VIOPAppContext.Logon();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); }); 
        }

        public void VIOPDisconnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            VIOPAppContext.Logout();
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); }); 
        }

        public void VIOPConnectOrDisconnect()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            if (CanLogout(VIOPAppContext))
            {
                VIOPAppContext.Logout();
            }
            else if (CanLogon(VIOPAppContext))
            {
                VIOPAppContext.Logon();
            }

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void VIOPLoadOrders()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            VIOPAppContext.LoadOrders();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void EndOfDay()
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            Clear();
            CashLoadOrders();
            VIOPLoadOrders();

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public bool CanLogout(BaseAppContext context)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            if (context.IsLoggedOn || (context.ConnectionStatus == ConnectStatus.Created
                                                            || context.ConnectionStatus == ConnectStatus.PendingLogon
                                                            || context.ConnectionStatus == ConnectStatus.Logon))
            {
                return true;
            }
            else
            {
                return false;
            }

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public bool CanLogon(BaseAppContext context)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            if (!context.IsLoggedOn)
            {
                return true;
            }
            else
            {
                return false;
            }

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendNewOrderToCash(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.SendNewOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendCancelOrderCash(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.SendCancelOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendReplaceOrderCash(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.SendReplaceOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendStatusOrderCash(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            CashAppContext.SendStatusOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendNewOrderToFuture(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            VIOPAppContext.SendNewOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendCancelOrderFuture(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 
            
            VIOPAppContext.SendCancelOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendReplaceOrderFuture(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 
            
            VIOPAppContext.SendReplaceOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        public void SendStatusOrderFuture(Teb.FIX.Contents.Content co, out bool isSend)
        {
            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} started", MethodInfo.GetCurrentMethod().GetFullName())); }); 

            VIOPAppContext.SendStatusOrder(co, out isSend);

            Debug.DebugHelper.Run(() => { Debug.DebugHelper.Log(String.Format("{0} end", MethodInfo.GetCurrentMethod().GetFullName())); });  
        }

        private void Exception(Exception ex)
        {
            if (OnException != null)
                OnException(ex);
        }

    }
}
