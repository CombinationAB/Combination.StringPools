using BenchmarkDotNet.Running;
using Performance;

//var summary = BenchmarkRunner.Run<Deduplication>();
var summary = BenchmarkRunner.Run<Hashing>();

#if false
foreach (var sizeMultiple in Enumerable.Range(1, 11))
{
    var size = 100000 * sizeMultiple;

    var random = Deduplication.Random;
    var dataSet = Enumerable.Range(0, size).Select(_ => Deduplication.RandomString()).ToArray();
    using var pool = StringPool.DeduplicatedUtf8(4096, 1, 9);
    var sw = Stopwatch.StartNew();
    for (var j = 0; j < 10; ++j)
    {
        for (var i = dataSet.Length - 1; i >= 0; --i)
        {
            pool.Add(dataSet[i]);
        }
    }

    var e = sw.ElapsedMilliseconds;
    Console.WriteLine(size + ", " + (e / (double)(size*10) * 1000).ToString(CultureInfo.InvariantCulture) + " µs");
}
#endif
