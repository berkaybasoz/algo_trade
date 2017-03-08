using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra;
using Teb.Infra.Model;
using Teb.Infra.Extension;
using Teb.Infra.Collection;

namespace QuantConnect.Brokerages.TEB.Data
{
   /// <summary>
    /*   Tip 001: (Sembol Create) *
             001+ID+FF+Sembol+FF+Description+FF+Exchange+FF+PazarKodu+FF+Derinlik+FF+Sektor+FF+Decimal+FF+GrafikVarmı+FF+Endeksler+FF
         
         Tip 002: (Sembol Delete) *
             002+ID+FF+Sembol+FF
    
         Tip 011: ( IMKB Seans Saatleri)*
             011+Kalıcımı_Geçicimi(1)+FF+1.SeansBas(5)+FF+1.SeansBit(5)+FF+2.SeansBas(5)+FF+2.SeansBit(5)+FF
            
             1.SeansBas, 1.SeansBit, 2.SeansBas, 2.SeansBit : SS:DD (Örneğin 12:00 gibi)

         Tip 012: (Tarih Saat ve Seans Bilgisi) *
             012+2014-06-10+FF+12:00+FF+1+FF            Tarih: YYYY-AA-GG 
     
         Tip 020: ( Haber Başlığı ) *                     
             020+HaberID+FF+DateTime+FF+Baslik+FF+İçerikUzunlugu+FF+Ajans+FF+Kategori+FF+İlişkiliHisseSayısı+FF+HisseKodu+FF+HisseKodu+FF 
             DateTime : YYYY-AA-GG SS:MM:Sn
         Tip 021: (Haber İçeriği )*
             021+HaberID+FF+İçerikUzunlugu+FF+İçerik+FF

         Tip 022: (Haber Silme)*
             022+HaberID+FF
     
         Tip 060: (Genel Veri Update Paketi) 
         Tip 061: (Genel Veri Refresh Paketi) *
             060+Hisse Kodu+FF+Field Kod+FF+Deger+FF+Field Kod+FF+Deger+FF

         Tip 062 : (Derinlik kademesi insert) *
         Tip 063 : (Derinlik kademesi refresh) *
         Tip 064 : (Derinlik kademesi update) *
             062+Hisse Kodu+FF+Alis Satis+FF+Satır No+FF+Fiyat+FF+Miktar+FF+Emir sayısı+FF+Time+FF+Derinlik Kademe Sayısı+FF
           AlisSatis: 0-Alis, 1-Satis	
           Derinlik Kademe Sayısı: IMKB – 5, VOB - 19


         Tip 065 : (Derinlik kademesi delete) *
    */
    /// </summary>
    public class MatriksTcpDataProvider : AbstractSecurityDataProvider
    {

        public MatriksTcpDataProvider(string ip, int port)
        {
            this.matriksIP = ip;
            this.matriksPort = port;
        }

        #region Fields

        private readonly int ENV_NEWLINE_LENGTH = Environment.NewLine.Length;
        private const char MATRIKS_SEPERATOR = '�';
        private string matriksIP;
        private string missingMessage;
        private string maxTimeStampStr;
        private string seanceInfo;
        //private IPAddress NotIPAdress = IPAddress.Any;
        //private int NotPort = 1001;

        private int matriksPort;

        private bool startEnabled = true;
        private bool stopEnabled;
        private bool watchEnabled;

        //private DbQueryManager dbManager;
        //private DbLogQueue dbLogQueue;
        private MatriksListener listener;
        private StreamMessageQueue streamMessageQueue;
        private ParserQueue parser001Queue;
        private ParserQueue parser002Queue;
        private ParserQueue parser012Queue;
        private ParserQueue parser020Queue;
        private ParserQueue parser060Queue;
        private ParserQueue parser064Queue;
        private ParserQueue parserUnkownQueue;

        private MarketCollection marketDefinitions;
        private ExchangeCollection exchangeDefinitions;

        private DateTime maxTimeStamp = DateTime.Now;

        #endregion

        #region Properties

        public string MaxTimeStampStr
        {
            get { return maxTimeStampStr; }
            set
            {
                maxTimeStampStr = value;
                MaxTimeStampChange(maxTimeStampStr);
            }
        }

        public DateTime MaxTimeStamp
        {
            get { return maxTimeStamp; }
            set
            {
                maxTimeStamp = value;
                MaxTimeStampStr = maxTimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

            }
        }

        public StreamMessageQueue StreamMessageQueue
        {
            get { return streamMessageQueue; }
            set
            {
                streamMessageQueue = value;
            }
        }

        public ParserQueue Parser001Queue
        {
            get { return parser001Queue; }
            set
            {
                parser001Queue = value;
            }
        }

        public ParserQueue Parser002Queue
        {
            get { return parser002Queue; }
            set
            {
                parser002Queue = value;
            }
        }

        public ParserQueue Parser012Queue
        {
            get { return parser012Queue; }
            set
            {
                parser012Queue = value;
            }
        }
        public ParserQueue Parser020Queue
        {
            get { return parser020Queue; }
            set
            {
                parser020Queue = value;
            }
        }
        public ParserQueue Parser060Queue
        {
            get { return parser060Queue; }
            set
            {
                parser060Queue = value;
            }
        }

        public ParserQueue Parser064Queue
        {
            get { return parser064Queue; }
            set
            {
                parser064Queue = value;
            }
        }

        public ParserQueue ParserUnkownQueue
        {
            get { return parserUnkownQueue; }
            set
            {
                parserUnkownQueue = value;
            }
        }

        public MarketCollection MarketDefinitions
        {
            get
            {
                if (marketDefinitions == null)
                {
                    marketDefinitions = new MarketCollection();
                }
                return marketDefinitions;
            }
            set
            {
                marketDefinitions = value;
            }
        }

        public ExchangeCollection ExchangeDefinitions
        {
            get
            {
                if (exchangeDefinitions == null)
                {
                    exchangeDefinitions = new ExchangeCollection();
                }
                return exchangeDefinitions;
            }
            set
            {
                exchangeDefinitions = value;
            }
        }

        public MatriksListener Listener
        {
            get
            {
                return listener;
            }
            set { listener = value; }
        }

        public bool StartEnabled
        {
            get { return startEnabled; }
            set
            {
                startEnabled = value;
            }
        }

        public bool StopEnabled
        {
            get { return stopEnabled; }
            set
            {
                stopEnabled = value;
            }
        }

        public bool WatchEnabled
        {
            get { return watchEnabled; }
            set
            {
                watchEnabled = value;
            }
        }

        public string MissingMessage
        {
            get { return missingMessage; }
            set { missingMessage = value; }
        }

        public string SeanceInfo
        {
            get { return seanceInfo; }
            set
            {
                seanceInfo = value;
            }
        }

        #endregion

        #region Parsers

        private void UnkownParser(string dataFeedMsg)
        {
            HandleUknownFeed(new FeedEventArgs(dataFeedMsg));
        }

        private void NullParser(string dataFeedMsg)
        {

        }

        private void TryParse001(string dataFeedMsg)
        {

            try
            {
                string source = dataFeedMsg.Substring(0, 3);
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);

                if (svalues.Length != 12)
                {
                    HandleLog("Wrong 001 Message : " + dataFeedMsg);
                    return;
                }
                string origSymbol = svalues[1];
                if (string.IsNullOrEmpty(origSymbol))
                {
                    HandleLog("Missing 001 Symbol Field 001 Message : " + dataFeedMsg);
                    return;
                }

                int symbolCount = origSymbol.Length;
                if (symbolCount < 3)
                {
                    HandleLog("Wrong 001 Symbol Count " + symbolCount + " 001 Message : " + dataFeedMsg);
                    return;
                }

                string id = svalues[0];
                string description = svalues[2];
                string exchangeID = svalues[3];
                string marketCode = svalues[4];
                string depth = svalues[5];
                string sectorID = svalues[6];
                string decimalPlace = svalues[7];
                string endexes = svalues[8];
                string symbol = svalues[9];
                string sfx = svalues[10];

                Security sec;
                if (!SecurityValues.TryGetValue(origSymbol, out sec))
                {
                    sec = new Security();
                    sec.OrgSecurity = origSymbol;
                }

                sec.Source = source;
                sec.LastUpdate = DateTime.Now;
                sec.Id = id;
                sec.OrgSecurity = origSymbol;
                sec.Description = description;
                sec.ExchangeID = exchangeID;


                SetSecuritySembol(sec, symbol, sfx);

                Exchange exch;
                if (ExchangeDefinitions.TryGetValue(exchangeID, out  exch))
                {
                    sec.ExchangeDescription = exch.Description;
                }

                sec.MarketCode = marketCode;

                string mrktDef;
                if (MarketDefinitions.TryGetValue(marketCode, out  mrktDef))
                {
                    sec.MarketDescription = mrktDef;
                }
                sec.Depth = short.Parse(depth);
                sec.SectorID = sectorID;
                sec.DecimalPlace = short.Parse(decimalPlace);
                sec.Endexes = endexes;

                char[] eList = endexes.ToCharArray();
                if (eList[0] == '0')
                {
                    sec.IsBist30 = false;
                }
                else
                {
                    sec.IsBist30 = true;
                }
                if (eList[1] == '0')
                {
                    sec.IsBist50 = false;
                }
                else
                {
                    sec.IsBist50 = true;
                }
                if (eList[2] == '0')
                {
                    sec.IsBist100 = false;
                }
                else
                {
                    sec.IsBist100 = true;
                }
                sec.LastUpdate = DateTime.Now;
                SecurityValues.AddOrUpdate(origSymbol, sec, (k, v) =>
                {
                    sec.IsDeleted = v.IsDeleted;
                    return sec;
                });


                HandleSecurityChanged(new SecurityEventArgs(sec, dataFeedMsg));

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks symbol create mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse002(string dataFeedMsg)
        {

            try
            {
                string source = dataFeedMsg.Substring(0, 3);
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);



                if (svalues.Length != 3)
                {
                    HandleLog("Wrong 002 Message : " + dataFeedMsg);
                    return;
                }


                string id = svalues[0];
                string origSymbol = svalues[1];


                Security sec;
                if (!SecurityValues.TryGetValue(origSymbol, out sec))
                {
                    sec = new Security();
                    sec.OrgSecurity = origSymbol;
                }
                sec.Source = source;

                sec.Id = id;
                sec.OrgSecurity = origSymbol;
                sec.IsDeleted = true;
                sec.LastUpdate = DateTime.Now;

                SecurityValues.AddOrUpdate(origSymbol, sec, (k, v) =>
                {
                    v.IsDeleted = true;
                    return v;
                });

                HandleSecurityChanged(new SecurityEventArgs(sec, dataFeedMsg));
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks symbol delete mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse012(string dataFeedMsg)
        {

            try
            {
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);



                if (svalues.Length != 4)
                {
                    HandleLog("Wrong 012 Message : " + dataFeedMsg);
                    return;

                }

                string date = svalues[0];

                if (date.StartsWith(DateTime.Now.Year.ToString()) == false)
                {
                    HandleLog("Wrong 012 Message : " + dataFeedMsg);
                    return;
                }

                string time = svalues[1];
                string seanceInfoCode = svalues[2]; /* 0 : IMKB hisse senetleri piyasası seansı kapalı.
                                                       1 : IMKB hisse senetleri piyasası seansı açık.
                                                       2 : Tatil (IMKB hisse senetleri piyasasında işlem yapılmıyor
                                                     */


                string seansInfo = "";
                switch (seanceInfoCode)
                {
                    case "0":
                        seansInfo = "IMKB hisse senetleri piyasası seansı kapalı";
                        break;
                    case "1":
                        seansInfo = "IMKB hisse senetleri piyasası seansı açık";
                        break;
                    case "2":
                        seansInfo = "Tatil (IMKB hisse senetleri piyasasında işlem yapılmıyor";
                        break;
                    default:
                        HandleLog("Unkownn SeanceInfo Field 012 : " + dataFeedMsg);
                        return;
                }
                SeanceInfo = seansInfo;

                BistTime bistTime = new BistTime();
                bistTime.Date = date;
                bistTime.Time = time;
                bistTime.SeanceInfoCode = seanceInfoCode;
                bistTime.SeanceInfo = seansInfo;

                HandleBistTimeChanged(new BistTimeEventArgs(bistTime));
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks seans zaman bilgisi mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse020(string dataFeedMsg)//Haber basligi
        {
            try
            {
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);
                int len = svalues.Length;
                if (len < 8)
                {
                    HandleLog("Wrong 020 Message : " + dataFeedMsg);
                    return;

                }
                else if (len == 8 || (len == 9 && svalues[8] == ""))
                {
                    News news = new News();
                    news.NewsId = svalues[0];
                    news.DateTime = svalues[1];
                    news.Header = svalues[2];
                    news.ContentLength = svalues[3];
                    int cl;
                    if (int.TryParse(svalues[3], out cl) == false)
                    {

                    }
                    news.Agent = svalues[4];
                    news.Category = svalues[5];
                    news.RelatedStockCount = svalues[6];
                    news.RelatedStocks = svalues[7];
                    news.UpdateTime = DateTime.Now;
                    news.Source = "020";
                    news.StreamMessage = dataFeedMsg;
                    HandleNewsChanged(new NewsEventArgs(news, dataFeedMsg));
                }
                else
                {
                    News news = new News();
                    news.NewsId = svalues[0];
                    news.DateTime = svalues[1];
                    news.Header = svalues[2];
                    news.ContentLength = svalues[3];
                    news.Agent = svalues[4];
                    news.Category = svalues[5];
                    news.RelatedStockCount = svalues[6];


                    for (int i = 7; i < len; i++)
                    {
                        if (string.IsNullOrEmpty(svalues[i]) == false)
                            news.RelatedStocks += svalues[i] + ",";
                    }
                    if (news.RelatedStocks.Length > 1)
                        news.RelatedStocks = news.RelatedStocks.Remove(news.RelatedStocks.Length - 1, 1);
                    news.UpdateTime = DateTime.Now;
                    news.Source = "020";
                    news.StreamMessage = dataFeedMsg;
                    HandleNewsChanged(new NewsEventArgs(news, dataFeedMsg));

                }


            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks haber bilgisi mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse021(string dataFeedMsg)//Haber icerigi
        {

            try
            {
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);

                News news = new News();
                news.NewsId = svalues[0];
                news.ContentLength = svalues[1];
                int len = svalues.Length;

                for (int i = 2; i < len; i++)
                {
                    news.Content += svalues[i];
                }

                news.UpdateTime = DateTime.Now;
                news.Source = "021";
                news.StreamMessage = dataFeedMsg;
                HandleNewsChanged(new NewsEventArgs(news, dataFeedMsg));


            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks haber bilgisi mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse022(string dataFeedMsg)//Haber silme
        {

            try
            {
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);


                //News news = new News();
                //news.IsDeleted=true;
                //HandleNewsChanged(new NewsArgsEventArgs(news));

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks haber bilgisi mesajı işlenirken hatası oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse060(string dataFeedMsg)//Genel veri update paketi
        {

            try
            {

                string source = dataFeedMsg.Substring(0, 3);
                string tmp1 = dataFeedMsg.Substring(3, 1);
                int tmpInt1;
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);

                List<string> vals = new List<string>(notSymbolData.Split(MATRIKS_SEPERATOR));
                string origSymbol = vals[0];
                vals.RemoveAt(0);


                if (int.TryParse(tmp1, out tmpInt1))
                {
                    HandleLog("Wrong 060 Message : " + dataFeedMsg);
                    return;
                }

                if (string.IsNullOrEmpty(origSymbol))
                {
                    HandleLog("Missing Symbol Field 060 Message : " + dataFeedMsg);
                    return;
                }


                Security sec;
                if (!SecurityValues.TryGetValue(origSymbol, out sec))//if (sec == null) 
                {
                    sec = new Security();
                    sec.OrgSecurity = origSymbol;
                }

                SetSecuritySembol(sec, null, null);

                sec.LastUpdate = DateTime.Now;
                sec.Source = source;

                for (int i = 0; i < vals.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        string key = vals[i];
                        if (!string.IsNullOrEmpty(key))
                        {
                            string val = vals[i + 1];

                            SecurityPropertySetter.Set(sec, new KeyValuePair<string, string>(key, val));
                            sec.Dictionary.AddOrUpdate(key, val, (k, v) => { return val; });

                        }

                    }

                }
                SecurityValues.AddOrUpdate(origSymbol, sec, (k, v) => { return sec; });

                HandleSecurityChanged(new SecurityEventArgs(sec, dataFeedMsg));



            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks sembol verisi işlenirken hata oluştu", ex);
                HandleException(tex);
            }
        }

        private void TryParse064(string dataFeedMsg)//Derinlik kademesi update
        {

            try
            {

                string source = dataFeedMsg.Substring(0, 3);
                string tmp1 = dataFeedMsg.Substring(3, 1);
                int tmpInt1;
                string notSymbolData = dataFeedMsg.Substring(3, dataFeedMsg.Length - 3);
                string[] svalues = Split(notSymbolData);
                string origSymbol = svalues[0];

                int l = svalues.Length;

                if (l != 9 || (l != 9 && int.TryParse(tmp1, out tmpInt1)))
                {
                    HandleLog("Wrong 064 Message : " + dataFeedMsg);
                    return;
                }
                if (string.IsNullOrEmpty(origSymbol))
                {
                    HandleLog("Missing Symbol Field 064 Message : " + dataFeedMsg);
                    return;
                }


                string tmpSide = svalues[1];
                string tmpRow = svalues[2];
                string tmpPrice = svalues[3];
                string tmpLots = svalues[4];
                string tmporders = svalues[5];
                string time = svalues[6];
                string tmpDepth = svalues[7];

                decimal price;
                int lots;
                int orders;
                int side;
                int depth;
                int row;



                if (int.TryParse(tmpRow, out row) == false)
                {
                    HandleLog("Missing RowIndex Field 064 Message : " + dataFeedMsg);
                    return;
                }

                int listIndex = row - 1;
                decimal.TryParse(tmpPrice, out  price);
                int.TryParse(tmpLots, out lots);
                int.TryParse(tmporders, out orders);
                int.TryParse(tmpSide, out side);//0=> Buy 1=> Sell
                int.TryParse(tmpDepth, out depth);
                Quote q = null;
                SecurityDepth secDepth = null;

                // DispatchService.BeginInvoke(new Action(() =>
                //{
                QuoteValues.AddOrUpdate(origSymbol, (q = new Quote() { Name = origSymbol, DepthCollection = GetQouteCollection(origSymbol, depth) }), (k, v) =>
                {
                    q = v;
                    return q;
                });
                //}));
                // DispatchService.BeginInvoke(new Action(() =>
                // {
                q.DepthCollection.AddOrUpdate(listIndex, (secDepth = new SecurityDepth() { Name = origSymbol }), (k, v) =>
                {
                    secDepth = v;
                    return secDepth;
                });
                //}));
                secDepth.RowIndex = listIndex;
                secDepth.Source = source;
                MaxTimeStamp = DateTime.Now;
                q.LastUpdateTime = MaxTimeStamp;
                secDepth.LastUpdateTime = MaxTimeStamp;
                secDepth.LastStreamTime = time;
                if (side == 0)
                {

                    secDepth.BuyPrice = price;
                    secDepth.BuyLots = lots;
                    secDepth.BuyOrders = orders;
                }
                else
                {
                    secDepth.SellPrice = price;
                    secDepth.SellLots = lots;
                    secDepth.SellOrders = orders;
                }

                HandleSecurityDepthChanged(new SecurityDepthEventArgs(secDepth));

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matrkiks sembol derinlik verisi işlenirken hata oluştu", ex);
                HandleException(tex);
            }
        }
        #endregion


        private void SetSecuritySembol(Security sec, string symbol, string sfx)
        {
            if (sec.ExchangeID == "4")
            {
                if (symbol != null)
                {
                    sec.Symbol = symbol;
                    sec.SymbolSfx = sfx;
                }
                else
                {
                    if (sec.OrgSecurity.Length == 6)
                    {
                        sec.Symbol = sec.OrgSecurity.Substring(0, 5) + '.' + sec.OrgSecurity.Substring(5, 1);
                        sec.SymbolSfx = sec.OrgSecurity.Substring(5, 1);
                    }
                    else if (sec.OrgSecurity.Length == 5 || sec.OrgSecurity.Length == 4)
                    {
                        sec.Symbol = sec.OrgSecurity + ".E";
                        sec.SymbolSfx = "E";
                    }
                }
            }
            else
            {
                sec.Symbol = sec.OrgSecurity;
            }
            #region MyRegion
            //if (string.IsNullOrEmpty(sec.SecurityCode))
            //{
            //    if (sec.ExchangeID == "4")
            //    {
            //        if (sec.OrgSecurity.Length == 6)
            //        {
            //            sec.SecurityCode = sec.OrgSecurity.Substring(0, 5) + '.' + sec.OrgSecurity.Substring(5, 1);
            //            sec.SecurityGroupCode = sec.OrgSecurity.Substring(5, 1);
            //        }
            //        else if (sec.OrgSecurity.Length == 5 || sec.OrgSecurity.Length == 4)
            //        {
            //            sec.SecurityCode = sec.OrgSecurity + ".E";
            //            sec.SecurityGroupCode = "E";
            //        }
            //    }
            //    else
            //    {
            //        sec.SecurityCode = sec.OrgSecurity;
            //    }
            //} 
            #endregion
        }


        #region Dequeues
        private void quoteQueue_OnDequeue(object arg1, QueueEventArgs<string> arg)
        {
            ProcessSocketMessage(arg.Entry);
        }
        private void Parser001Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        private void Parser002Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        private void Parser012Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        private void Parser020Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        private void Parser060Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);

        }
        private void Parser064Queue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        private void ParserUnkownQueue_OnDequeue(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            arg2.Entry.Func(arg2.Entry.Parameter);
        }
        //private   void DbLogQueue_OnDequeue(object arg1, QueueEventArgs<DbLog> arg)
        //{ 
        //     AddDbLog(arg);
        //}
        //private void DbLogQueue_OnBulkDequeue(object arg1, QueueEventArgs<IEnumerable<DbLog>> arg2)
        //{

        //}
        private void LogQueue_OnDequeue(object arg1, QueueEventArgs<string> arg2)
        {
            HandleLog(arg2.Entry);
        }

        #endregion

        #region OnExceptions
        private void quoteQueue_OnException(object arg1, QueueEventArgs<string> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "QuoteQueue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);

        }
        private void Parser001Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser001Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void Parser002Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser002Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void Parser012Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser012Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void Parser020Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser020Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void Parser060Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser060Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void Parser064Queue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Parser064Queue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        private void ParserUnkownQueue_OnException(object arg1, QueueEventArgs<ParserArg> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "ParserUnkownQueue_OnException hatası oluştu", arg2.Exception);
            HandleException(tex);
        }
        //private void DbLogQueue_OnException(object arg1, QueueEventArgs<DbLog> arg2)
        //{
        //    TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "DbLogQueue_OnException hatası oluştu", arg2.Exception);
        //    HandleException(tex);
        //}
        private void LogQueue_OnException(object arg1, QueueEventArgs<string> arg)
        {
            HandleLog("CRITICAL ERROR!! LogQueue_OnException Message :" + arg.Exception.Message);
        }
        //private void DbLogQueue_OnBulkException(object arg1, QueueEventArgs<IEnumerable<DbLog>> arg2)
        //{
        //    TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "DbLogQueue_OnBulkDequeue hatası oluştu", arg2.Exception);
        //    HandleException(tex);
        //}



        #endregion

        public override void Start()
        {
            try
            {
                ParserUnkownQueue = new ParserQueue();
                ParserUnkownQueue.OnDequeue += ParserUnkownQueue_OnDequeue;
                ParserUnkownQueue.OnException += ParserUnkownQueue_OnException;


                Parser001Queue = new ParserQueue();
                Parser001Queue.OnDequeue += Parser001Queue_OnDequeue;
                Parser001Queue.OnException += Parser001Queue_OnException;


                Parser002Queue = new ParserQueue();
                Parser002Queue.OnDequeue += Parser002Queue_OnDequeue;
                Parser002Queue.OnException += Parser002Queue_OnException;


                Parser012Queue = new ParserQueue();
                Parser012Queue.OnDequeue += Parser012Queue_OnDequeue;
                Parser012Queue.OnException += Parser012Queue_OnException;

                Parser020Queue = new ParserQueue();
                Parser020Queue.OnDequeue += Parser020Queue_OnDequeue;
                Parser020Queue.OnException += Parser020Queue_OnException;

                Parser060Queue = new ParserQueue();
                Parser060Queue.OnDequeue += Parser060Queue_OnDequeue;
                Parser060Queue.OnException += Parser060Queue_OnException;


                Parser064Queue = new ParserQueue();
                Parser064Queue.OnDequeue += Parser064Queue_OnDequeue;
                Parser064Queue.OnException += Parser064Queue_OnException;


                StreamMessageQueue = new StreamMessageQueue();
                StreamMessageQueue.OnDequeue += quoteQueue_OnDequeue;
                StreamMessageQueue.OnException += quoteQueue_OnException;



                //SetSecurities();

                Listener = new MatriksListener(matriksIP, matriksPort);
                Listener.StartMatriksPriceSocket(OnSocketMessage);

                //Broadcaster = new DataBroadcaster(NotIPAdress, NotPort);

                //Broadcaster.OnLog += NotSocketServer_OnLog;
                //Broadcaster.OnException += NotSocketServer_OnException;
                //Broadcaster.Start();



                StartEnabled = false;
                StopEnabled = true;
                WatchEnabled = true;
                MissingMessage = string.Empty;

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Açılırken hata oluştu", ex);
                HandleException(tex);
            }

        }

        public override void Stop()
        {
            if (Listener != null)
                Listener.StopMatriksPriceSocket();


            StreamMessageQueue.Stop();
            StreamMessageQueue.Dispose();


            Parser001Queue.Stop();
            Parser001Queue.Dispose();
            Parser002Queue.Stop();
            Parser002Queue.Dispose();
            Parser012Queue.Stop();
            Parser012Queue.Dispose();
            Parser020Queue.Stop();
            Parser020Queue.Dispose();
            Parser060Queue.Stop();
            Parser060Queue.Dispose();
            Parser064Queue.Stop();
            Parser064Queue.Dispose();
            ParserUnkownQueue.Stop();
            ParserUnkownQueue.Dispose();

            //DbLogQueue.Stop();
            //DbLogQueue.Dispose();


            StartEnabled = true;
            StopEnabled = false;
            WatchEnabled = false;
        }

        public void OnSocketMessage(SocketBase socket, Int32 numBytes)
        {
            try
            {
                String strMessage = ASCIIEncoding.UTF8.GetString(socket.RawBuffer, 0, numBytes);

                StreamMessageQueue.Enqueue(strMessage);

                socket.RawBuffer = null;
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matriks socketinden veri gelirken hata oluştu", ex);
                HandleException(tex);

            }
        }

        private void ProcessSocketMessage(string recievedMessage)
        {
            try
            {
                string totalMessage = MissingMessage + recievedMessage;

                int l = totalMessage.Length;
                int ind = totalMessage.LastIndexOf(Environment.NewLine);
                if (ind != -1)
                {
                    int lastInd = (ind + ENV_NEWLINE_LENGTH);
                    if (l > lastInd)
                    {
                        MissingMessage = totalMessage.Substring(lastInd, totalMessage.Length - lastInd);
                        totalMessage = totalMessage.Remove(lastInd, totalMessage.Length - lastInd);
                    }
                    else
                    {
                        MissingMessage = string.Empty;
                    }
                    // string[] asdasdas=   recievedMessage.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    // int aasdas=recievedMessage.IndexOf(Environment.NewLine);
                    string[] lines = totalMessage.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string tmpLine = lines[i].Replace("\n", "");

                        CrackParser(tmpLine);

                    }
                }
                else
                {
                    MissingMessage = totalMessage;
                }
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Matriks socket verisi işlenirken hata oluştu", ex);
                HandleException(tex);
            }
        }

        private void CrackParser(string tmpLine)
        {
            if (string.IsNullOrEmpty(tmpLine) || tmpLine.Length < 10)
            {
                ParserUnkownQueue.Enqueue(new ParserArg() { Func = UnkownParser, Parameter = tmpLine });
            }
            else
            {
                string head = tmpLine.Substring(0, 3);

                switch (head)
                {
                    case "001":
                        Parser001Queue.Enqueue(new ParserArg() { Func = TryParse001, Parameter = tmpLine });
                        break;
                    case "002":
                        Parser002Queue.Enqueue(new ParserArg() { Func = TryParse002, Parameter = tmpLine });
                        break;
                    case "012":
                        Parser012Queue.Enqueue(new ParserArg() { Func = TryParse012, Parameter = tmpLine });
                        break;
                    case "020":
                        Parser020Queue.Enqueue(new ParserArg() { Func = TryParse020, Parameter = tmpLine });
                        break;
                    case "021":
                        Parser020Queue.Enqueue(new ParserArg() { Func = TryParse021, Parameter = tmpLine });
                        break;
                    case "022":
                        Parser020Queue.Enqueue(new ParserArg() { Func = TryParse022, Parameter = tmpLine });
                        break;
                    case "060":
                    case "061":
                        Parser060Queue.Enqueue(new ParserArg() { Func = TryParse060, Parameter = tmpLine });
                        break;
                    case "062":
                    case "063":
                    case "064":
                        Parser064Queue.Enqueue(new ParserArg() { Func = TryParse064, Parameter = tmpLine });
                        break;
                    case "065":
                        ParserUnkownQueue.Enqueue(new ParserArg() { Func = NullParser, Parameter = tmpLine });
                        break;


                }
            }
        }

        private SecurityDepthCollection GetQouteCollection(string name, int depth)
        {

            SecurityDepthCollection symbolPrices = new SecurityDepthCollection();

            for (int j = 0; j < depth; j++)
            {
                symbolPrices.AddOrUpdate(j, new SecurityDepth() { Name = name, RowIndex = j }, (k, v) => { return v; });
            }

            return symbolPrices;

        }

        private SecurityDepthCollection GetQouteCollection(string name, out int depth)
        {
            depth = (name.IndexOf("F_") >= 0 || name.IndexOf("O_") >= 0) ? 10 : 5;//Opsiyon yada Future ise
            return GetQouteCollection(name, depth);

        }

        public string[] Split(string text)
        {
            //return text.Split(new string[] { MATRIKS_SEPERATOR.ToString() }, StringSplitOptions.RemoveEmptyEntries);
            return text.Split(MATRIKS_SEPERATOR);
        }
    }
}
