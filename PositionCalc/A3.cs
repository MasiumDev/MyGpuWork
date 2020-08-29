using System;

namespace PositionCalc
{
    [Serializable]
    public struct A3
    {
        public byte ISensOm { get; set; }
        public int PLimSaiOm { get; set; }
        public int QTitMtrOm { get; set; }
        public long DHPriOm { get; set; }

        public A3(byte iSensOm, int pLimSaiOm, int qTitMtrOm, long dhPriOm)
        {
            PLimSaiOm = pLimSaiOm;
            DHPriOm = dhPriOm;
            QTitMtrOm = qTitMtrOm;
            ISensOm = iSensOm;
        }
    }

    public struct OrderPosition
    {
        public long DHPriOm { get; set; }
        public long Quantity { get; set; }
        public int Count { get; set; }
    }

    public struct Order
    {
        public byte ISensOm { get; set; }
        public int PLimSaiOm { get; set; }
        public long DHPriOm { get; set; }
        public long Quantity { get; set; }
        public int Count { get; set; }
    }
}