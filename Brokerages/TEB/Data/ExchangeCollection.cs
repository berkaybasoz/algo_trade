using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    
   
    public class ExchangeCollection : ConcurrentDictionary<string, Exchange>
    {
        public ExchangeCollection()
        {

            TryAdd("1", new Exchange() { ExchId = "1", ExchangeCode = "TAKASPARA", Description = "Takasbank Para Piyasasi" });
            TryAdd("3", new Exchange() { ExchId = "3", ExchangeCode = "FOREX", Description = "Online Spot" });
            TryAdd("4", new Exchange() { ExchId = "4", ExchangeCode = "IMKBHISSE", Description = "IMKB Hisse Senedi" });
            TryAdd("5", new Exchange() { ExchId = "5", ExchangeCode = "IMKBBONO", Description = "IMKB Tahvil" });
            TryAdd("6", new Exchange() { ExchId = "6", ExchangeCode = "TCMB", Description = "T.C. Merkez Bankasi" });
            TryAdd("8", new Exchange() { ExchId = "8", ExchangeCode = "BANKALAR", Description = "Bankalar Arasi Piyasa" });
            TryAdd("9", new Exchange() { ExchId = "9", ExchangeCode = "VIOP", Description = "IMKB Vadeli İşlem ve Opsiyon Piyasasi" });
            TryAdd("15", new Exchange() { ExchId = "15", ExchangeCode = "IMKBDER", Description = "IMKB Hisse Derinlik" });
            TryAdd("16", new Exchange() { ExchId = "16", ExchangeCode = "BONODER", Description = "Tahvil Derinlik" });
            TryAdd("28", new Exchange() { ExchId = "28", ExchangeCode = "MATRIKS", Description = "Matriks Verileri" });
            TryAdd("17", new Exchange() { ExchId = "17", ExchangeCode = "YFONLARI", Description = "Yatirim Fonlari" });
            TryAdd("18", new Exchange() { ExchId = "18", ExchangeCode = "IMKBDDVI", Description = "IMKB Dovize Dayali Vadeli Islemler" });
            TryAdd("22", new Exchange() { ExchId = "22", ExchangeCode = "ISEGOZALTI", Description = "IMKB Gozalti Pazari" });
            TryAdd("23", new Exchange() { ExchId = "23", ExchangeCode = "DUNYAEX", Description = "Dunya Endeksleri" });
            TryAdd("24", new Exchange() { ExchId = "24", ExchangeCode = "FUTURES", Description = "Futures" });
            TryAdd("25", new Exchange() { ExchId = "25", ExchangeCode = "LIBORFIBOR", Description = "Libor / Fibor" });
            TryAdd("26", new Exchange() { ExchId = "26", ExchangeCode = "SERBEST", Description = "Takasbank Para Piyasasi" });
            TryAdd("27", new Exchange() { ExchId = "27", ExchangeCode = "IMKBEX", Description = "Takasbank Para Piyasasi" });
            TryAdd("29", new Exchange() { ExchId = "29", ExchangeCode = "VOB", Description = "Takasbank Para Piyasasi" });
        }
    }

    public class Exchange
    {
        private string exchId;
        private string exchangeCode;
        private string description;

        public string ExchId
        {
            get { return exchId; }
            set
            {
                exchId = value;
            }
        }

        public string ExchangeCode
        {
            get { return exchangeCode; }
            set
            {
                exchangeCode = value;
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
            }
        }
    }
}
