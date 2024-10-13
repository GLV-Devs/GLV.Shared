using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Common;
public static class HttpExtensions
{
    public static TimeSpan GetRetryAfterTime(this RetryConditionHeaderValue? header)
    {
        if (header is not null)
        {
            TimeSpan delta;
            if (header.Delta is TimeSpan span)
                delta = span;
            else if (header.Date is DateTimeOffset date)
                delta = date - DateTimeOffset.Now;
            else
                return TimeSpan.FromMinutes(1);
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        return TimeSpan.FromMinutes(1);
    }
}
