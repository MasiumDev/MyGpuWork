using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Algorithms.RadixSortOperations;
using ILGPU.IR.Intrinsics;
using ILGPU.Runtime;

namespace PositionCalc
{
    class Program
    {
        private static Accelerator gpu;

        static void Main(string[] args)
        {
            gpu = Accelerator.Create(new Context(), Accelerator.Accelerators.First(a => a.AcceleratorType == AcceleratorType.Cuda));
            WarmUp();

            var dts = File.ReadAllLines($"{AppDomain.CurrentDomain.BaseDirectory}\\SampleData.dat").Select(x =>
             {
                 var split = x.Split(',');
                 return new A3(Convert.ToByte(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]), Convert.ToInt64(split[3]));
             }).ToList();

            var a3S = dts.Take(30000).ToArray();

            var samples = dts.Take(30000).Select(x => new Order
            {
                ISensOm = x.ISensOm,
                PLimSaiOm = x.PLimSaiOm,
                DHPriOm = x.DHPriOm
            }).ToArray();

            var sample = samples[100] /*samples.First(x => x.DHPriOm == 200422135924549659)*/;
            var sampleList = a3S.Where(x => x.ISensOm == sample.ISensOm &&
                                            (sample.ISensOm == 1 ? x.PLimSaiOm > sample.PLimSaiOm : x.PLimSaiOm < sample.PLimSaiOm ||
                                                                                                    x.PLimSaiOm == sample.PLimSaiOm && x.DHPriOm < sample.DHPriOm)).ToList();
            var test = sampleList.Sum(x => x.QTitMtrOm);

            Console.WriteLine($"Test: {sample.DHPriOm}: {sampleList.Count} - {test}");

            for (int j = 0; j < 100; j++)
            {
                var totalWatch = new Stopwatch();
                totalWatch.Start();
                var tasks = new List<Task>();
                for (int i = 0; i < 50; i++)
                {
                    //tasks.Add(Task.Run(() =>
                    //{
                        var result = new Order[samples.Length];

                        var kernel = gpu.LoadAutoGroupedStreamKernel<Index1, ArrayView<A3>, ArrayView<Order>>(ApplyKernel);

                        var watch = new Stopwatch();
                        watch.Start();

                        using (MemoryBuffer<A3> a3SBuffer = gpu.Allocate<A3>(a3S.Length))
                        {
                            using (MemoryBuffer<Order> samplesBuffer = gpu.Allocate<Order>(samples.Length))
                            {
                                a3SBuffer.CopyFrom(a3S, 0, Index1.Zero, a3SBuffer.Extent);
                                samplesBuffer.CopyFrom(samples, 0, Index1.Zero, samplesBuffer.Extent);

                                kernel(samplesBuffer.Length, a3SBuffer.View, samplesBuffer.View);

                                gpu.Synchronize();

                                result = samplesBuffer.GetAsArray();
                            }
                        }
                        watch.Stop();
                        Console.WriteLine($"Elapsed: {watch.ElapsedMilliseconds}");

                        var s = result.FirstOrDefault(x => x.DHPriOm == sample.DHPriOm);

                        //Console.WriteLine($"Real: {s.DHPriOm}: {s.Count} - {s.Quantity}");
                    //}));
                }

                Task.WaitAll(tasks.ToArray());

                totalWatch.Stop();
                Console.WriteLine($"Total: {totalWatch.ElapsedMilliseconds}");
            }

            Console.ReadLine();
        }

        private static void ApplyKernel(Index1 index, ArrayView<A3> a3s, ArrayView<Order> samples)
        {
            var sample = samples[index];

            for (int i = 0; i < a3s.Length; i++)
            {
                var a3 = a3s[i];
                if (a3.ISensOm == sample.ISensOm && (a3.ISensOm == 1 ? a3.PLimSaiOm > sample.PLimSaiOm : a3.PLimSaiOm < sample.PLimSaiOm || a3.PLimSaiOm == sample.PLimSaiOm && a3.DHPriOm < sample.DHPriOm))
                {
                    sample.Count += 1;
                    sample.Quantity += a3.QTitMtrOm;

                }
            }

            samples[index] = sample;

        }

        public static void WarmUp()
        {
            var pixelArray = new int[10];

            using (MemoryBuffer<int> buffer = gpu.Allocate<int>(pixelArray.Length))
            {
                buffer.CopyFrom(pixelArray, 0, Index1.Zero, pixelArray.Length);

                var ker = gpu.LoadAutoGroupedStreamKernel<Index1, ArrayView<int>>(ApplyWarmUp);

                ker(buffer.Length, buffer.View);

                // Wait for the kernel to finish...
                gpu.Synchronize();

            }
        }
        public static void ApplyWarmUp(Index1 index, ArrayView<int> arrayView)
        {
            arrayView[index] = arrayView[index] * 2;
        }
    }
}
