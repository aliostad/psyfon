using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Psyfon.Tests
{
    [TestFixture]
    public class HasherTests
    {
        [Test]
        public void Md5HasherHashersToBuckets()
        {
            int bucketCount = 4;
            var dic = new Dictionary<string, string>();
            var hasher = new Md5Hasher();

            for (int i = 0; i < 1000; i++)
            {
                dic[hasher.Hash(Guid.NewGuid().ToString(), bucketCount)] = null;
            }

            Assert.AreEqual(bucketCount, dic.Count);
        }
    }
}
