using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class NewsDbAdapter : NewsDbLog
    {
        public string streamMessage = "";

        public NewsDbAdapter(News n, string streamMsg = "")
        {
            Query = "up_SecurityStreamNews";
            IsStoreProcedure = true;
            streamMessage = streamMsg;
            Parameters = new Dictionary<string, object>();
            Parameters.Add("Kriter", 1);
            Parameters.Add("NewsId", n.NewsId);
            Parameters.Add("Header", n.Header);
            Parameters.Add("Content", n.Content);
            Parameters.Add("ContentLength", n.ContentLength);
            Parameters.Add("Agent", n.Agent);
            Parameters.Add("Category", n.Category);
            Parameters.Add("RelatedStockCount", n.RelatedStockCount);
            Parameters.Add("RelatedStocks", n.RelatedStocks);
            Parameters.Add("IsDeleted", n.IsDeleted);
            Parameters.Add("DateTime", n.DateTime);
            Parameters.Add("Source", n.Source);
            Parameters.Add("UpdateTime", n.UpdateTime);
            Parameters.Add("StreamMessage", streamMsg);
        }

        public override string ToString()
        {
            return String.Format("Matriks verisi: {0}", this.streamMessage);
            //return base.ToString();
        }
    }
}
