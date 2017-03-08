using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class MarketCollection : ConcurrentDictionary<string, string>
    {
        public MarketCollection()
        {
            TryAdd("A", "Bos");
            TryAdd("B", "Birincil Piyasa");
            TryAdd("E", "Eski Pazar");
            TryAdd("G", "Gelisen isletmeler Pazari");
            TryAdd("J", "IMKB Dısı Endeksler");
            TryAdd("K", "Kurumsal Urunler Pazari");
            TryAdd("L", "2. Ulusal Pazar");
            TryAdd("N", "Ulusal/Kotici Pazar");
            TryAdd("O", "Opsiyon Pazari");
            TryAdd("R", "Ruchan Hakki Pazari");
            TryAdd("S", "Serbest islem Platformu");
            TryAdd("U", "Uluslararasi Pazar");
            TryAdd("V", "Vadeli İşlem Pazari");
            TryAdd("W", "Gozalti Pazari");
            TryAdd("X", "IMKB Endeksleri");
            TryAdd("Y", "Yeni Sirketler Pazari");
        }
    }
}
