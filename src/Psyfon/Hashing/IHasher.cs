using System;
using System.Collections.Generic;
using System.Text;

namespace Psyfon
{
    public interface IHasher
    {
        int Hash(string value, int numberOfBuckets);
    }
}
