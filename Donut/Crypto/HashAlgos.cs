using System;
using System.Collections.Generic;
using System.Text;

namespace Donut.Crypto
{
    public static class HashAlgos
    {
        public static uint Adler32(string str)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (char c in str)
            {
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }
    }
}
