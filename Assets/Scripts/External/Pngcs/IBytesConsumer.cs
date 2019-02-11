using System;
using System.Collections.Generic;
using System.Text;

namespace Pngcs
{
    interface IBytesConsumer
    {
        int consume ( byte[] buf , int offset , int tofeed );//nie było tej linijki, dopisałem zgadując z zastosowania
    }
}
