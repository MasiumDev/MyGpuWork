using System;

namespace MyGpuWork.Models
{
    public class MMTPNumber
    {
        public string IFt { get; set; }
        public string QMt { get; set; }

        //TODO: int.TryParse
        public string GetNumber() =>
            double.TryParse(IFt, out var ift) && ift != 0.0 ? QMt.Insert((int)Math.Round(QMt.Length - ift), ".") : QMt;

    }
}

