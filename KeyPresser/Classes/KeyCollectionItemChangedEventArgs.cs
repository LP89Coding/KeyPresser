using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyPresser
{
    public class KeyCollectionItemChangedEventArgs : EventArgs
    {
        public KeyPressInfo ChangedItem { get; private set; }
        public string ChangedProperty { get; private set; }

        public KeyCollectionItemChangedEventArgs(KeyPressInfo ChangedItem, string ChangedProperty)
        {
            this.ChangedItem = ChangedItem;
            this.ChangedProperty = ChangedProperty;
        }
    }
}
