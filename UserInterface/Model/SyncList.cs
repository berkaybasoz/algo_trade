using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model
{
    public class SyncList<T> : System.ComponentModel.BindingList<T>
    {

        private System.ComponentModel.ISynchronizeInvoke _SyncObject;
        private System.Action<System.ComponentModel.ListChangedEventArgs> _FireEventAction;

        public SyncList()
            : this(null)
        {
        }

        public SyncList(System.ComponentModel.ISynchronizeInvoke syncObject)
        {

            _SyncObject = syncObject;
            _FireEventAction = FireEvent;
        }

        protected override void OnListChanged(System.ComponentModel.ListChangedEventArgs args)
        {
            if (_SyncObject == null)
            {
                FireEvent(args);
            }
            else
            {
                _SyncObject.Invoke(_FireEventAction, new object[] { args });
            }
        }

        private void FireEvent(System.ComponentModel.ListChangedEventArgs args)
        {
            base.OnListChanged(args);
        }
    }
}
