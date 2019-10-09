using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RssServer
{
    public class Counter
    {
        private long count = 0;

        public Counter(long count = 0)
        {
            this.count = count;
        }

        public long Increment()
        {
            return Interlocked.Increment(ref this.count);
        }

        public long Decrement()
        {
            return Interlocked.Decrement(ref this.count);
        }

        public long Get()
        {
            return this.count;
        }

        public void Reset(long count)
        {
            this.count = count;
        }
    }
}
