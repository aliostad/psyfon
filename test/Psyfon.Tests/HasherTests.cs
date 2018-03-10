using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Psyfon.Tests
{
    public class HasherTests
    {
        [Fact]
        public void Md5HasherHashersToBuckets()
        {
            int bucketCount = 4;
            var dic = new Dictionary<int, string>();
            var hasher = new Md5Hasher();

            for (int i = 0; i < 1000; i++)
            {
                dic[hasher.Hash(Guid.NewGuid().ToString(), bucketCount)] = null;
            }

            Assert.Equal(bucketCount, dic.Count);
        }
    }
}
