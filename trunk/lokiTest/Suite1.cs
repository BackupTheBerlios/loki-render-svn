using System;
using NUnit.Framework;

namespace loki
{

	[TestFixture]
    public class Job_Test
    {
        [Test]
        public void dummyTest()
        {
            Job j = new Job("job1", "exe", "bFile", "blah", "blah", "blah", "blah", "blah", 3, 5, 3);
            Assert.That(true);

        }
    }
}