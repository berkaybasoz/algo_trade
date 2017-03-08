using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class SecurityPropertySetter
    {
        public static void Set(Security security, KeyValuePair<string, string> pair)
        {
            string key = pair.Key;
            string value = pair.Value;

            decimal val;
            int i;
            switch (key)
            {
                case "1":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Open = val;
                    }
                    break;
                case "2":
                    if (decimal.TryParse(value, out val))
                    {
                        security.High = val;
                    }
                    break;
                case "3":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Low = val;
                    }
                    break;
                case "4":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Last = val;
                    }
                    break;
                case "5":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YesterdayClose = val;
                    }
                    break;
                case "6":
                    if (int.TryParse(value, out i))
                    {
                        security.TotalSize = i;
                    }
                    break;
                case "7":
                    if (decimal.TryParse(value, out val))
                    {
                        security.WeekHigh = val;
                    }
                    break;
                case "8":
                    if (decimal.TryParse(value, out val))
                    {
                        security.WeekLow = val;
                    }

                    break;
                case "9":
                    if (decimal.TryParse(value, out val))
                    {
                        security.MonthHigh = val;
                    }

                    break;
                case "10":

                    if (decimal.TryParse(value, out val))
                    {
                        security.MonthLow = val;
                    }

                    break;
                case "11":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YearHigh = val;
                    }

                    break;
                case "12":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YearLow = val;
                    }

                    break;
                case "13":
                    if (decimal.TryParse(value, out val))
                    {
                        security.WeekClose = val;
                    }

                    break;
                case "14":

                    if (decimal.TryParse(value, out val))
                    {
                        security.MonthClose = val;
                    }
                    break;
                case "15":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YearClose = val;
                    }
                    break;
                case "16":
                    if (int.TryParse(value, out i))
                    {
                        security.LastSize = i;
                    }

                    break;
                case "17":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Bid = val; //alis
                    }

                    break;
                case "18":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Ask = val;//satis
                    }

                    break;
                case "19":
                    if (int.TryParse(value, out i))
                    {
                        security.BidSize = i;
                    }

                    break;
                case "20":

                    if (int.TryParse(value, out i))
                    {
                        security.AskSize = i;
                    }
                    break;
                case "21":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BazFiyat = val;
                    }

                    break;
                case "22":
                    if (decimal.TryParse(value, out val))
                    {
                        security.WAwr = val;
                    }

                    break;
                case "23":
                    if (decimal.TryParse(value, out val))
                    {
                        security.WeekWAwr = val;
                    }
                    break;
                case "24":
                    if (decimal.TryParse(value, out val))
                    {
                        security.MonthWAwr = val;
                    }
                    break;
                case "25":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YearWAwr = val;
                    }

                    break;
                case "26":
                    if (decimal.TryParse(value, out val))
                    {
                        security.LimitUp = val;//Tavan
                    }
                    break;
                case "27":
                    if (decimal.TryParse(value, out val))
                    {
                        security.LimitDown = val;//Taban
                    }
                    break;
                case "30":
                    if (int.TryParse(value, out i))
                    {
                        security.Direction = i;
                    }
                    break;
                case "31":
                    if (decimal.TryParse(value, out val))
                    {
                        security.SessionClose = val;
                    }
                    break;
                case "32":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Session1Vol = val;
                    }
                    break;
                case "33":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Session1Size = val;
                    }
                    break;
                case "34":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Session1WAvr = val;
                    }
                    break;
                case "35":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Session1High = val;
                    }
                    break;
                case "36":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Session1Low = val;
                    }
                    break;
                case "37":
                    security.UpdateTime = value;
                    break;
                case "38":
                    security.TimeStamp = value;
                    break;
                case "41":
                    if (decimal.TryParse(value, out val))
                    {
                        security.Sermaye = val;
                    }
                    break;
                case "42":
                    if (decimal.TryParse(value, out val))
                    {
                        security.NetKar = val;
                    }
                    break;
                case "43":
                    security.Donem = value;
                    break;
                case "44":
                    security.Aciklik = value;

                    break;
                case "45":
                    if (decimal.TryParse(value, out val))
                    {
                        security.KayNetKar = val;
                    }
                    break;
                case "46":
                    if (decimal.TryParse(value, out val))
                    {
                        security.OpenInterest = val;
                    }
                    break;
                case "55":
                    if (decimal.TryParse(value, out val))
                    {
                        security.SeansFark = val;
                    }
                    break;
                case "56":
                    if (decimal.TryParse(value, out val))
                    {
                        security.SeansYuzdeDegisim = val;
                    }
                    break;
                case "57":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukIslemHacmi = val;
                    }
                    break;
                case "58":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukIslemAdedi = val;
                    }
                    break;
                case "59":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukAgirlikliOrtalama = val;
                    }
                    break;
                case "60":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukYuksek = val;
                    }
                    break;
                case "61":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukDusuk = val;
                    }
                    break;
                case "64":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukFark = val;
                    }
                    break;
                case "65":
                    if (decimal.TryParse(value, out val))
                    {
                        security.HaftalikFark = val;
                    }
                    break;
                case "66":
                    if (decimal.TryParse(value, out val))
                    {
                        security.AylikFark = val;
                    }
                    break;
                case "67":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YillikFark = val;
                    }
                    break;
                case "68":
                    if (decimal.TryParse(value, out val))
                    {
                        security.GunlukYuzdeDegisim = val;
                    }
                    break;
                case "69":
                    if (decimal.TryParse(value, out val))
                    {
                        security.HaftalikYuzdeDegisim = val;
                    }
                    break;
                case "70":
                    if (decimal.TryParse(value, out val))
                    {
                        security.AylikYuzdeDegisim = val;
                    }
                    break;
                case "71":
                    if (decimal.TryParse(value, out val))
                    {
                        security.YillikYuzdeDegisim = val;
                    }
                    break;
                case "97":
                    if (decimal.TryParse(value, out val))
                    {
                        security.VOBUzlasma = val;
                    }
                    break;
                case "98":
                    if (decimal.TryParse(value, out val))
                    {
                        security.VOBOncekiUzlasma = val;
                    }
                    break;
                case "116":
                    security.PiyasaYapiciUye = value;
                    break;
                case "119":
                    security.HisseSenediGrubu = value;
                    break;
                case "139":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumAlislarinOrtalamasi = val;
                    }
                    break;
                case "140":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumSatislarinOrtalamasi = val;
                    }

                    break;
                case "141":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumAlislarinMiktari = val;
                    }
                    break;
                case "142":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumSatislarinMiktari = val;
                    }
                    security.BekleyenTumSatislarinMiktari = decimal.Parse(value);
                    break;
                case "143":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumAlislarinYuzdesi = val;
                    }
                    break;
                case "144":
                    if (decimal.TryParse(value, out val))
                    {
                        security.BekleyenTumSatislarinYuzdesi = val;
                    }
                    break;
                case "155":
                    if (decimal.TryParse(value, out val))
                    {
                        security.KapanisSeansFiyati = val;
                    }
                    break;
                case "156":
                    security.TabanTavanKapaniSeansinaAitMi = value;
                    break;

            }
        }
    }
}
