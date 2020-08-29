using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alea.Parallel;
using AleaGpuWork.Models;
using Dapper;

namespace AleaGpuWork
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new MmtpNumberTypeHandler());

            using (var connection =
                new SqlConnection(
                    "Server=.;Initial Catalog=IFSCache;User Id=sa;Password=sql@123;MultipleActiveResultSets=true"))
            {
                connection.Open();

                var A3 = connection.Query<RLCDiffHeader, RLCA3, AAIdOm, RLCA3>(
                    "SELECT Tech_head_Type, ItemCode, SessionNumber, ABSMessageNumber, MessageNumberForItemCode, BroadCastTimestamp, TransMitterSignature, InstumentCharacteristicHeaderType, MarketFeedCode, MarketPlaceCode, FinancialMarketCode, CIDGrc, InstrumentID, CValMNE, DEven, HEven, MessageCodeType, SEQbyINSTandType, ID, CActFdm, ISensOm, PLimSaiOm, QTitMtrOm, QTitRestOm, PAffOm, YCpteOm, YOm, Filler, YPLimSaiOm, DHPriOm, YAppaValMdv, CIdAdh, DSaiOm, NSeqOm FROM dbo.RLCA3 where DEven = '20200422'",
                    map:
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
                    NSeqOm = int.Parse(x.AIdOm.NSeqOm)
                }).ToArray();

                var gA4 = A4.Where(x => x.Header.InstrumentID == "IRO1FOLD0001").Select(x => new A4
                {
                    Id = x.Id,
                    InstrumentId = x.Header.InstrumentID.ToHash(),
                    DSaiOm = int.Parse(x.AIdOm.DSaiOm),
                    NSeqOm = int.Parse(x.AIdOm.NSeqOm)
                }).ToArray();

                var cpuResult = new A3[gA3.Length];

                Alea.Gpu.Default.For(0, 10, x =>
                {
                    
                });

                var watch = new Stopwatch();
                watch.Start();
                Alea.Gpu.Default.For(0, gA3.Length, x =>
                 {
                     var a3 = gA3[x];

                     cpuResult[x] = a3;

                     for (int i = 0; i < gA4.Length; i++)
                     {
                         var a4 = gA4[i];

                         if (a4.InstrumentId == a3.InstrumentId && a4.DSaiOm == a3.DSaiOm && a4.NSeqOm == a3.NSeqOm && a4.Id > a3.Id)
                         {
                             cpuResult[x] = new A3();
                             break;
                         }
                        
                     }

                 });
                watch.Stop();
                Console.WriteLine($"Elapsed: {watch.ElapsedMilliseconds}");

                var final = cpuResult.Where(x => x.Id != 0).ToList();

                Console.ReadLine();
            }
        }
    }
}
