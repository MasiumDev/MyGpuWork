using System;
using System.Text;

namespace AleaGpuWork.Models
{
    public class RLCDiffHeader 
    {
        public long Tech_head_Type ;
        public long ItemCode ;
        public long SessionNumber ;
        public long ABSMessageNumber ;
        public long MessageNumberForItemCode ;
        public long BroadCastTimestamp ;
        public long TransMitterSignature ;
        public string InstumentCharacteristicHeaderType ;
        public string MarketFeedCode ;
        public string MarketPlaceCode ;
        public string FinancialMarketCode ;
        public string CIDGrc ;

        public string InstrumentID ;
        public string CValMNE ;

        public string DEven ;
        public string HEven ;
        public string MessageCodeType ;
        public string SEQbyINSTandType { get; set; }

        public void Parse(byte[] Buffer)
        {
            byte[] bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 0, bytes, 0, 1);
            this.Tech_head_Type = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 1, bytes, 0, 2);
            this.ItemCode = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 3, bytes, 0, 2);
            this.SessionNumber = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 5, bytes, 0, 4);
            this.ABSMessageNumber = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 9, bytes, 0, 4);
            this.MessageNumberForItemCode = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 13, bytes, 0, 4);
            this.BroadCastTimestamp = BitConverter.ToInt64(bytes, 0);
            bytes = BitConverter.GetBytes((long)0L);
            Array.Copy(Buffer, 17, bytes, 0, 8);
            this.TransMitterSignature = BitConverter.ToInt64(bytes, 0);
            this.InstumentCharacteristicHeaderType = Encoding.GetEncoding(1256).GetString(Buffer, 25, 1);
            this.MarketFeedCode = Encoding.GetEncoding(1256).GetString(Buffer, 26, 2);
            this.MarketPlaceCode = Encoding.GetEncoding(1256).GetString(Buffer, 28, 3);
            this.FinancialMarketCode = Encoding.GetEncoding(1256).GetString(Buffer, 31, 3);
            this.CIDGrc = Encoding.GetEncoding(1256).GetString(Buffer, 34, 2);
            this.InstrumentID = Encoding.GetEncoding(1256).GetString(Buffer, 36, 12);
            this.CValMNE = Encoding.GetEncoding(1256).GetString(Buffer, 48, 5);
            this.DEven = Encoding.GetEncoding(1256).GetString(Buffer, 53, 8);
            this.HEven = Encoding.GetEncoding(1256).GetString(Buffer, 61, 6);
            this.MessageCodeType = Encoding.GetEncoding(1256).GetString(Buffer, 67, 4);
            this.SEQbyINSTandType = Encoding.GetEncoding(1256).GetString(Buffer, 71, 6);
        }

    }
}

