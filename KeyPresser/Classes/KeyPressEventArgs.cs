using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyPresser
{
    public class KeyPressEventArgs : EventArgs
    {
        public KeyPressInfo KeyPressed { get; private set; }

        public KeyPressEventArgs(KeyPressInfo KeyPressed)
        {
            this.KeyPressed = KeyPressed;
        }
    }
}
