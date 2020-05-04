using System.Data;
using Dapper;

namespace AleaGpuWork.Models
{
    public class MmtpNumberTypeHandler : SqlMapper.TypeHandler<MMTPNumber>
    {
        public override MMTPNumber Parse(object value)
        {
            return value.ToString().ToMmtpNumber();
        }

        public override void SetValue(IDbDataParameter parameter, MMTPNumber value)
        {
            parameter.Value = value.GetNumber();
        }
    }
}