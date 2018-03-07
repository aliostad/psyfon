using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Psyfon
{
    public class Md5Hasher : IHasher
    {
        private MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider();

        public string Hash(string value, int numberOfBuckets)
        {
            var bytes = _md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            var firstChunk = BitConverter.ToInt64(bytes, 0);
            return (firstChunk % numberOfBuckets).ToString();
        }
    }
}
