using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ILGPU;
using ILGPU.Runtime;
using Microsoft.Data.SqlClient;
using MyGpuWork.Models;

namespace MyGpuWork
{
    class Program
    {
        static Accelerator gpu;

        static void Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new MmtpNumberTypeHandler());

            using (var connection = new SqlConnection("Server=.;Initial Catalog=IFSCache;User Id=sa;Password=sql@123;MultipleActiveResultSets=true"))
            {
                connection.Open();

                var A3 = connection.Query<RLCDiffHeader, RLCA3, AAIdOm, RLCA3>(
                   "SELECT Tech_head_Type, ItemCode, SessionNumber, ABSMessageNumber, MessageNumberForItemCode, BroadCastTimestamp, TransMitterSignature, InstumentCharacteristicHeaderType, MarketFeedCode, MarketPlaceCode, FinancialMarketCode, CIDGrc, InstrumentID, CValMNE, DEven, HEven, MessageCodeType, SEQbyINSTandType, ID, CActFdm, ISensOm, PLimSaiOm, QTitMtrOm, QTitRestOm, PAffOm, YCpteOm, YOm, Filler, YPLimSaiOm, DHPriOm, YAppaValMdv, CIdAdh, DSaiOm, NSeqOm FROM dbo.RLCA3 where DEven = '20200422'", map:
                   (header, a3, aIdOm) =>
                   {
                       a3.Header = header;
                       a3.AIdOm = aIdOm;
                       return a3;
                   }, splitOn: "Id,CIdAdh").ToList();

                var A4 = connection.Query<RLCDiffHeader, RLCA4, AAIdOm, RLCA4>(
                   "SELECT Tech_head_Type, ItemCode, SessionNumber, ABSMessageNumber, MessageNumberForItemCode, BroadCastTimestamp, TransMitterSignature, InstumentCharacteristicHeaderType, MarketFeedCode, MarketPlaceCode, FinancialMarketCode, CIDGrc, InstrumentID, CValMNE, DEven, HEven, MessageCodeType, SEQbyINSTandType, ID, YSupOm, ISensOm, CIdAdh, DSaiOm, NSeqOm FROM dbo.RLCA4 where DEven = '20200422'",
                   map:
                   (header, a4, aIdOm) =>
                   {
                       a4.Header = header;
                       a4.AIdOm = aIdOm;
                       return a4;
                   }, splitOn: "Id,CIdAdh").ToList();

                var gA3 = A3.Where(x => x.Header.InstrumentID == "IRO1FOLD0001").Select(x => new A3
                {
                    Id = x.Id,
                    InstrumentId = x.Header.InstrumentID.ToHash(),
                    DSaiOm = int.Parse(x.AIdOm.DSaiOm),
                    NSeqOm = int.Parse(x.AIdOm.NSeqOm),
                }).ToArray();
                for (int i = 0; i < gA3.Length; i++)
                {
                    gA3[i].Hash = long.Parse($"{gA3[i].DSaiOm}{gA3[i].NSeqOm}");
                }


                var gA4 = A4.Where(x => x.Header.InstrumentID == "IRO1FOLD0001").Select(x => new A4
                {
                    Id = x.Id,
                    InstrumentId = x.Header.InstrumentID.ToHash(),
                    DSaiOm = int.Parse(x.AIdOm.DSaiOm),
                    NSeqOm = int.Parse(x.AIdOm.NSeqOm)
                }).ToArray();
                for (int i = 0; i < gA4.Length; i++)
                {
                    gA4[i].Hash = long.Parse($"{gA4[i].DSaiOm}{gA4[i].NSeqOm}");
                }

                var watch = new Stopwatch();


                gpu = Accelerator.Create(new Context(), Accelerator.Accelerators.First(a => a.AcceleratorType == AcceleratorType.Cuda));
                var kernel = gpu.LoadAutoGroupedStreamKernel<Index1, ArrayView<A3>, ArrayView<A4>>(ApplyKernel);


                Console.WriteLine("Warming up GPU...");
                WarmUp();


                Console.WriteLine($"Run");
                watch.Start();

                var result = Run(gpu, gA3, gA4, kernel);

                watch.Stop();
                Console.WriteLine($"elapsed: {watch.ElapsedMilliseconds}");

                var final = result.Where(x => x.Id != 0).ToList();

                //watch.Restart();
                //var serial =  gA3.AsParallel().Where(a3 => !gA4.AsParallel().Any(a4 =>
                //    a4.InstrumentId == a3.InstrumentId && a4.DSaiOm == a3.DSaiOm && a4.NSeqOm == a3.NSeqOm &&
                //    a4.Id > a3.Id)).ToList();
                //watch.Stop();

                //Console.WriteLine($"cpu: {watch.ElapsedMilliseconds}");
            }

            Console.ReadLine();
        }

        private static A3[] Run(Accelerator gpu, A3[] gA3, A4[] gA4, Action<Index1, ArrayView<A3>, ArrayView<A4>> kernel)
        {
            Console.WriteLine($"A3: {gA3.Length}");
            Console.WriteLine($"A4: {gA4.Length}");

            Console.WriteLine($"Start Copy");

            var watch = new Stopwatch();
            watch.Start();
            using (MemoryBuffer<A3> a3buffer = gpu.Allocate<A3>(gA3.Length))
            using (MemoryBuffer<A4> a4buffer = gpu.Allocate<A4>(gA4.Length))
            {
                a3buffer.CopyFrom(gA3, 0, Index1.Zero, a3buffer.Extent);
                a4buffer.CopyFrom(gA4, 0, Index1.Zero, a4buffer.Extent);
                Console.WriteLine($"copy: {watch.ElapsedMilliseconds}");

                watch.Restart();
                kernel(a3buffer.Length, a3buffer.View, a4buffer.View);

                // Wait for the kernel to finish...
                gpu.Synchronize();


                var asArray = a3buffer.GetAsArray();
                watch.Stop();
                Console.WriteLine($"Process: {watch.ElapsedMilliseconds}");

                return asArray;
            }
        }

        private static void ApplyKernel(Index1 index, ArrayView<A3> a3, ArrayView<A4> a4)
        {
            a3[index] = Process(a3[index], a4);
        }

        private static A3 Process(A3 a3, ArrayView<A4> a4s)
        {
            for (int i = 0; i < a4s.Length; i++)
            {
                var a4 = a4s[i];

                if (a4.Hash == a3.Hash && a4.Id > a3.Id)
                    return new A3();
            }

            return a3;
        }

        public static void ApplyWarmUp(Index1 index, ArrayView<int> arrayView)
        {
            arrayView[index] = arrayView[index] * 2;
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
    }
}
