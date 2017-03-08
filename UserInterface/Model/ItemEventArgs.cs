using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model
{
    public class ItemEventArgs<T> : EventArgs
    {
        public ItemEventArgs(T item)
        {
            Item = item;
        }

        public T Item { get; protected set; }

        public static implicit operator ItemEventArgs<T>(T item)
        {
            return new ItemEventArgs<T>(item);
        }

    }

}
