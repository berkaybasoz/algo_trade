using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuantConnect.Views.Model.UI
{
    public class SafeBindingSource : BindingSource
    {
        readonly bool _sortable = true;
        public SafeBindingSource() : this(true) { }
        public SafeBindingSource(bool sortallowed) : base() { _sortable = sortallowed; }
        public override bool SupportsSorting
        {
            get { return _sortable; }
        }

        public override int Add(object value)
        {
            lock (SyncRoot)
                return base.Add(value);
        }

        public override void Clear()
        {
            lock (SyncRoot)
                base.Clear();
        }

        public override int IndexOf(object value)
        {
            lock (SyncRoot)
                return base.IndexOf(value);
        }

        public override void Insert(int index, object value)
        {
            lock (SyncRoot)
                base.Insert(index, value);
        }

        public override void Remove(object value)
        {
            lock (SyncRoot)
                base.Remove(value);
        }

        public override void RemoveAt(int index)
        {
            lock (SyncRoot)
                base.RemoveAt(index);
        }

        public override int Count
        {
            get { lock (SyncRoot) return base.Count; }
        }

        public override object this[int index]
        {
            get { lock (SyncRoot) return base[index]; }
            set { lock (SyncRoot) base[index] = value; }
        }


        public override IEnumerator GetEnumerator()
        {
            lock (SyncRoot)
            {
                // readonly copy
                return (DataSource as IEnumerable<object>).ToList().GetEnumerator();
            }
        }

    }
}
