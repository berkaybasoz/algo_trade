using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class SecurityBuilder
    {
        public Security Build(System.Data.IDataReader queryResult)
        {
            Security sec = new Security();
            sec.OrgSecurity = queryResult["OrgSecurity"].ToString();
            sec.Symbol = queryResult["Symbol"].ToString();
            sec.SymbolSfx = queryResult["SymbolSfx"].ToString();
            sec.Id = queryResult["Id"].ToString();
            sec.Aciklik = queryResult["Aciklik"].ToString();
            sec.Ask = Convert.ToDecimal(queryResult["Ask"]);
            sec.AskSize = Convert.ToInt32(queryResult["AskSize"]);
            sec.AylikFark = Convert.ToDecimal(queryResult["AylikFark"]);
            sec.AylikYuzdeDegisim = Convert.ToDecimal(queryResult["AylikYuzdeDegisim"]);
            sec.BazFiyat = Convert.ToDecimal(queryResult["BazFiyat"]);
            sec.BekleyenTumAlislarinMiktari = Convert.ToDecimal(queryResult["BekleyenTumAlislarinMiktari"]);
            sec.BekleyenTumAlislarinOrtalamasi = Convert.ToDecimal(queryResult["BekleyenTumAlislarinOrtalamasi"]);
            sec.BekleyenTumAlislarinYuzdesi = Convert.ToDecimal(queryResult["BekleyenTumAlislarinYuzdesi"]);
            sec.BekleyenTumSatislarinMiktari = Convert.ToDecimal(queryResult["BekleyenTumSatislarinMiktari"]);
            sec.BekleyenTumSatislarinOrtalamasi = Convert.ToDecimal(queryResult["BekleyenTumSatislarinOrtalamasi"]);
            sec.BekleyenTumSatislarinYuzdesi = Convert.ToDecimal(queryResult["BekleyenTumSatislarinYuzdesi"]);
            sec.Bid = Convert.ToDecimal(queryResult["Bid"]);
            sec.BidSize = Convert.ToInt32(queryResult["BidSize"]);
            sec.DecimalPlace = Convert.ToInt16(queryResult["DecimalPlace"]);
            sec.Depth = Convert.ToInt16(queryResult["Depth"]);
            sec.Description = queryResult["Description"].ToString();
            sec.Direction = Convert.ToInt32(queryResult["Direction"]);
            sec.Donem = queryResult["Donem"].ToString();
            sec.Endexes = queryResult["Endexes"].ToString();
            sec.ExchangeID = queryResult["ExchangeID"].ToString();
            sec.GunlukAgirlikliOrtalama = Convert.ToDecimal(queryResult["GunlukAgirlikliOrtalama"]);
            sec.GunlukDusuk = Convert.ToDecimal(queryResult["GunlukDusuk"]);
            sec.GunlukFark = Convert.ToDecimal(queryResult["GunlukFark"]);
            sec.GunlukIslemAdedi = Convert.ToDecimal(queryResult["GunlukIslemAdedi"]);
            sec.GunlukIslemHacmi = Convert.ToDecimal(queryResult["GunlukIslemHacmi"]);
            sec.GunlukYuksek = Convert.ToDecimal(queryResult["GunlukYuksek"]);
            sec.GunlukYuzdeDegisim = Convert.ToDecimal(queryResult["GunlukYuzdeDegisim"]);
            sec.HaftalikFark = Convert.ToDecimal(queryResult["HaftalikFark"]);
            sec.HaftalikYuzdeDegisim = Convert.ToDecimal(queryResult["HaftalikYuzdeDegisim"]);
            sec.High = Convert.ToDecimal(queryResult["High"]);
            sec.HisseSenediGrubu = queryResult["HisseSenediGrubu"].ToString();
            sec.IsBist100 = Convert.ToBoolean(queryResult["IsBist100"]);
            sec.IsBist30 = Convert.ToBoolean(queryResult["IsBist30"]);
            sec.IsBist50 = Convert.ToBoolean(queryResult["IsBist50"]);
            sec.IsDeleted = Convert.ToBoolean(queryResult["IsDeleted"]);
            sec.KapanisSeansFiyati = Convert.ToDecimal(queryResult["KapanisSeansFiyati"]);
            sec.KayNetKar = Convert.ToDecimal(queryResult["KayNetKar"]);
            sec.Last = Convert.ToDecimal(queryResult["Last"]);
            sec.LastSize = Convert.ToInt32(queryResult["LastSize"]);
            sec.LastUpdate = Convert.ToDateTime(queryResult["LastUpdate"]);
            sec.LimitDown = Convert.ToDecimal(queryResult["LimitDown"]);
            sec.LimitUp = Convert.ToDecimal(queryResult["LimitUp"]);
            sec.Low = Convert.ToDecimal(queryResult["Low"]);
            sec.MarketCode = queryResult["MarketCode"].ToString();
            sec.MonthClose = Convert.ToDecimal(queryResult["MonthClose"]);
            sec.MonthHigh = Convert.ToDecimal(queryResult["MonthHigh"]);
            sec.MonthLow = Convert.ToDecimal(queryResult["MonthLow"]);
            sec.MonthWAwr = Convert.ToDecimal(queryResult["MonthWAwr"]);
            sec.NetKar = Convert.ToDecimal(queryResult["NetKar"]);
            sec.Open = Convert.ToDecimal(queryResult["Open"]);
            sec.OpenInterest = Convert.ToDecimal(queryResult["OpenInterest"]);
            sec.PiyasaYapiciUye = queryResult["PiyasaYapiciUye"].ToString();
            sec.SeansFark = Convert.ToDecimal(queryResult["SeansFark"]);
            sec.SeansYuzdeDegisim = Convert.ToDecimal(queryResult["SeansYuzdeDegisim"]);
            sec.SectorID = queryResult["SectorID"].ToString();
            sec.Sermaye = Convert.ToDecimal(queryResult["Sermaye"]);
            sec.Session1High = Convert.ToDecimal(queryResult["Session1High"]);
            sec.Session1Size = Convert.ToDecimal(queryResult["Session1Size"]);
            sec.Session1Vol = Convert.ToDecimal(queryResult["Session1Vol"]);
            sec.Session1WAvr = Convert.ToDecimal(queryResult["Session1WAvr"]);
            sec.SessionClose = Convert.ToDecimal(queryResult["SessionClose"]);
            sec.Session1Low = Convert.ToDecimal(queryResult["SessionLow"]);
            sec.TabanTavanKapaniSeansinaAitMi = queryResult["TabanTavanKapaniSeansinaAitMi"].ToString();
            sec.TimeStamp = queryResult["TimeStamp"].ToString();
            sec.TotalSize = Convert.ToInt32(queryResult["TotalSize"]);
            sec.UpdateTime = queryResult["UpdateTime"].ToString();
            sec.VOBOncekiUzlasma = Convert.ToDecimal(queryResult["VOBOncekiUzlasma"]);
            sec.VOBUzlasma = Convert.ToDecimal(queryResult["VOBUzlasma"]);
            sec.WAwr = Convert.ToDecimal(queryResult["WAwr"]);
            sec.WeekClose = Convert.ToDecimal(queryResult["WeekClose"]);
            sec.WeekHigh = Convert.ToDecimal(queryResult["WeekHigh"]);
            sec.WeekLow = Convert.ToDecimal(queryResult["WeekLow"]);
            sec.WeekWAwr = Convert.ToDecimal(queryResult["WeekWAwr"]);
            sec.YearClose = Convert.ToDecimal(queryResult["YearClose"]);
            sec.YearHigh = Convert.ToDecimal(queryResult["YearHigh"]);
            sec.YearLow = Convert.ToDecimal(queryResult["YearLow"]);
            sec.YearWAwr = Convert.ToDecimal(queryResult["YearWAwr"]);
            sec.YesterdayClose = Convert.ToDecimal(queryResult["YesterdayClose"]);
            sec.YillikFark = Convert.ToDecimal(queryResult["YillikFark"]);
            sec.YillikYuzdeDegisim = Convert.ToDecimal(queryResult["YillikYuzdeDegisim"]);
            return sec;
        }
    }
}
