using System;
using System.Collections.Generic;
using System.Text;

namespace Psyfon
{
    public interface IHasher
    {
        string Hash(string value, int numberOfBuckets);
    }
}
