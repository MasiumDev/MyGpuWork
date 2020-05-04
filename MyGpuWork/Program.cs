using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ILGPU;
using ILGPU.Runtime;
using Microsoft.Data.SqlClient;
using MyGpuWork.Models;
using Index = ILGPU.Index;

namespace MyGpuWork
{
    class Program
    {
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

                //var gA3 = A3.Select(x =>new
                //{
                //    x.Id,
                //    x.Header.InstrumentID,
                //    x.AIdOm.DSaiOm,
                //    x.AIdOm.NSeqOm
                //}).ToColumn();

                var gA3 = A3.Select(x => new { instrumentId = x.Header.InstrumentID.ToHash(), x.AIdOm.NSeqOm }).To2DArray();

                var gpu = Accelerator.Create(new Context(), Accelerator.Accelerators.First(a => a.AcceleratorType == AcceleratorType.Cuda));
                var kernel = gpu.LoadAutoGroupedStreamKernel<Index2, ArrayView2D<int>>(ApplyKernel);

                var result = Run(gpu, gA3, kernel);

            }

        }

        private static int[,] Run(Accelerator gpu, int[,] gA3, Action<Index2, ArrayView2D<int>> kernel)
        {
            using (MemoryBuffer2D<int> buffer = gpu.Allocate<int>(gA3.GetLength(0), gA3.GetLength(1)))
            {
                buffer.CopyFrom(gA3, Index2.Zero, Index2.Zero, new Index2(gA3.GetLength(0), gA3.GetLength(1)));

                kernel(new Index2(gA3.GetLength(0), gA3.GetLength(1)), buffer.View);

                // Wait for the kernel to finish...
                gpu.Synchronize();

                return buffer.GetAs2DArray();
            }
        }

        private static void ApplyKernel(
            Index2 index, /* The global thread index (1D in this case) */
            ArrayView2D<int> array /* A view to a chunk of memory (1D in this case)*/)
        {
            array[index] = Process(array[index]);
        }

        private static int Process(int value)
        {
            for (long i = 0; i < 500000; i++)
            {
                var a = value != 664890191;
            }

            if (value != 664890191)
                return 0;

            return 1;
        }
    }
}
