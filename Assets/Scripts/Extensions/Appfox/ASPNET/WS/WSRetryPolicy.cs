using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Appfox.Unity.AspNetCore.WS.Extensions
{
    public class WSRetryPolicy : IRetryPolicy
    {
        private readonly Func<RetryContext, TimeSpan?> retryAction;

        public WSRetryPolicy(Func<RetryContext, TimeSpan?> retryAction)
        {
            this.retryAction = retryAction;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return retryAction(retryContext);
        }

        public static WSRetryPolicy CreateStaticPolicy(TimeSpan waitTime)
        {
            return new WSRetryPolicy(_ => waitTime);
        }

        public static WSRetryPolicy CreateNone()
        {
            return new WSRetryPolicy(_ => null);
        }
    }
}
