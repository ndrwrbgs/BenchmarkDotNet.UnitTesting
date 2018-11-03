using System;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace LibraryV3
{
    public class A
    {
        private Random rand = new Random();

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(
                rand.Next(2.6, 3.6)));
        }

        [Benchmark]
        public void Treatment(){
            Thread.Sleep(TimeSpan.FromMilliseconds(
                rand.Next(2.5, 3.5)));

        }
    }
}