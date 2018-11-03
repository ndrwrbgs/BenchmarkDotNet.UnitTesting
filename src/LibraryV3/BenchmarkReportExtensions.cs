using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Reports;

namespace LibraryV3
{
    internal static class BenchmarkReportExtensions
    {
        public static double[] GetAverageNanosecondsForResultRuns(this BenchmarkReport report)
        {
            return report.GetResultRuns().Select(run => run.GetAverageNanoseconds()).ToArray();
        }
    }
}
