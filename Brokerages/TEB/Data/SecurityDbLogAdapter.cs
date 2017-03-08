using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class SecurityDbLogAdapter : SecurityDbLog
    {
        public string streamMessage = "";

        public SecurityDbLogAdapter(Security sec, string streamMsg = "")
        {
            Query = "up_SecurityStream";
            IsStoreProcedure = true;
            streamMessage = streamMsg;
            Parameters = new Dictionary<string, object>();
            Parameters.Add("Kriter", 1);
            Parameters.Add("Id", sec.Id);
            Parameters.Add("OrgSecurity", sec.OrgSecurity);
            Parameters.Add("Symbol", sec.Symbol);
            Parameters.Add("SymbolSfx", sec.SymbolSfx);
            Parameters.Add("Description", sec.Description);
            Parameters.Add("ExchangeID", sec.ExchangeID);
            Parameters.Add("MarketCode", sec.MarketCode);
            Parameters.Add("Depth", sec.Depth);
            Parameters.Add("SectorID", sec.SectorID);
            Parameters.Add("DecimalPlace", sec.DecimalPlace);
            Parameters.Add("Endexes", sec.Endexes);
            Parameters.Add("IsBist30", sec.IsBist30);
            Parameters.Add("IsBist50", sec.IsBist50);
            Parameters.Add("IsBist100", sec.IsBist100);
            Parameters.Add("IsDeleted", sec.IsDeleted);
            Parameters.Add("LastUpdate", sec.LastUpdate);
            Parameters.Add("Session1WAvr", sec.Session1WAvr);
            Parameters.Add("Last", sec.Last);
            Parameters.Add("Open", sec.Open);
            Parameters.Add("High", sec.High);
            Parameters.Add("Low", sec.Low);
            Parameters.Add("YesterdayClose", sec.YesterdayClose);
            Parameters.Add("TotalSize", sec.TotalSize);
            Parameters.Add("WeekLow", sec.WeekLow);
            Parameters.Add("WeekHigh", sec.WeekHigh);
            Parameters.Add("MonthHigh", sec.MonthLow);
            Parameters.Add("MonthLow", sec.MonthLow);
            Parameters.Add("YearHigh", sec.YearHigh);
            Parameters.Add("YearLow", sec.YearLow);
            Parameters.Add("WeekClose", sec.WeekClose);
            Parameters.Add("MonthClose", sec.MonthClose);
            Parameters.Add("YearClose", sec.YearClose);
            Parameters.Add("LastSize", sec.LastSize);
            Parameters.Add("Ask", sec.Ask);
            Parameters.Add("Bid", sec.Bid);
            Parameters.Add("WAwr", sec.WAwr);
            Parameters.Add("Direction", sec.Direction);
            Parameters.Add("BazFiyat", sec.BazFiyat);
            Parameters.Add("WeekWAwr", sec.WeekWAwr);
            Parameters.Add("MonthWAwr", sec.MonthWAwr);
            Parameters.Add("YearWAwr", sec.YearWAwr);
            Parameters.Add("LimitUp", sec.LimitUp);
            Parameters.Add("LimitDown", sec.LimitDown);
            Parameters.Add("SessionClose", sec.SessionClose);
            Parameters.Add("Session1Vol", sec.Session1Vol);
            Parameters.Add("Session1Size", sec.Session1Size);
            Parameters.Add("Session1High", sec.Session1High);
            Parameters.Add("SessionLow", sec.Session1Low);
            Parameters.Add("Sermaye", sec.Sermaye);
            Parameters.Add("NetKar", sec.NetKar);
            Parameters.Add("Donem", sec.Donem);
            Parameters.Add("Aciklik", sec.Aciklik);
            Parameters.Add("KayNetKar", sec.KayNetKar);
            Parameters.Add("OpenInterest", sec.OpenInterest);
            Parameters.Add("SeansFark", sec.SeansFark);
            Parameters.Add("SeansYuzdeDegisim", sec.SeansYuzdeDegisim);
            Parameters.Add("GunlukIslemHacmi", sec.GunlukIslemHacmi);
            Parameters.Add("GunlukIslemAdedi", sec.GunlukIslemAdedi);
            Parameters.Add("GunlukAgirlikliOrtalama", sec.GunlukAgirlikliOrtalama);
            Parameters.Add("GunlukYuksek", sec.GunlukYuksek);
            Parameters.Add("GunlukDusuk", sec.GunlukDusuk);
            Parameters.Add("GunlukFark", sec.GunlukFark);
            Parameters.Add("HaftalikFark", sec.HaftalikFark);
            Parameters.Add("AylikFark", sec.AylikFark);
            Parameters.Add("YillikFark", sec.YillikFark);
            Parameters.Add("GunlukYuzdeDegisim", sec.GunlukYuzdeDegisim);
            Parameters.Add("HaftalikYuzdeDegisim", sec.HaftalikYuzdeDegisim);
            Parameters.Add("AylikYuzdeDegisim", sec.AylikYuzdeDegisim);
            Parameters.Add("YillikYuzdeDegisim", sec.YillikYuzdeDegisim);
            Parameters.Add("VOBUzlasma", sec.VOBUzlasma);
            Parameters.Add("VOBOncekiUzlasma", sec.VOBOncekiUzlasma);
            Parameters.Add("BidSize", sec.BidSize);
            Parameters.Add("AskSize", sec.AskSize);
            Parameters.Add("TimeStamp", sec.TimeStamp);
            Parameters.Add("UpdateTime", sec.UpdateTime);
            Parameters.Add("HisseSenediGrubu", sec.HisseSenediGrubu);
            Parameters.Add("KapanisSeansFiyati", sec.KapanisSeansFiyati);
            Parameters.Add("TabanTavanKapaniSeansinaAitMi", sec.TabanTavanKapaniSeansinaAitMi);
            Parameters.Add("PiyasaYapiciUye", sec.PiyasaYapiciUye);
            Parameters.Add("BekleyenTumAlislarinOrtalamasi", sec.BekleyenTumAlislarinOrtalamasi);
            Parameters.Add("BekleyenTumSatislarinOrtalamasi", sec.BekleyenTumSatislarinOrtalamasi);
            Parameters.Add("BekleyenTumAlislarinMiktari", sec.BekleyenTumAlislarinMiktari);
            Parameters.Add("BekleyenTumSatislarinMiktari", sec.BekleyenTumSatislarinMiktari);
            Parameters.Add("BekleyenTumAlislarinYuzdesi", sec.BekleyenTumAlislarinYuzdesi);
            Parameters.Add("BekleyenTumSatislarinYuzdesi", sec.BekleyenTumSatislarinYuzdesi);
            Parameters.Add("Source", sec.Source);
            Parameters.Add("StreamMessage", streamMsg);
        }

        public override string ToString()
        {
            return String.Format("Matriks verisi: {0}", this.streamMessage);
            //return base.ToString();
        }
    }
}
