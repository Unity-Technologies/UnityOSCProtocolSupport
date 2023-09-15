using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Media.Osc
{
    static class AsyncExtensions
    {
        public static Task WhenCancelled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}
