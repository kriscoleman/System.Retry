using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Retry
{

    /// <summary>
    /// A Retry Helper which adheres to MSDN's Retry Pattern guidelines
    /// This allows you to safely retry an action; defining a Transient Exception Strategy to determine if it is safe to retry, or if it should throw the exception encountered.
    /// </summary>
    /// 
    /// <Warning>
    /// Any usages of a retry pattern should be idempotent: meaning, if the retryAction affects data, repeated retries need to have the same NET effect on the data. 
    /// If usages of this class modify data (e.g.;, executing commands to a service which creates orders in a database), then it's possible the retry pattern could result in multiple additions. Validation needs to be done on the Reciever-Side for these types of actions to prevent this and ensure Idempotency. 
    /// This is why it is only safe to use under indempotent conditions.
    /// </Warning>
    /// 
    /// <seealso cref="https://msdn.microsoft.com/en-us/library/dn589788.aspx"/>
    public static class Retry
    {
        const string OutOfRetriesMessage = "Retry ran out of retry attempts.";
        const string NonTransientExceptionEncounteredMessage =
            "Retry encountered a non-transient exception, which the provided transient exception strategy determined was not safe for retry attempts.";

        /// <summary>
        /// Will attempt an Action a given number of times, with a given interval wait between attempts, 
        /// which implements a Transient Exception Strategy Predicate and an optional Logging Delegate
        /// </summary>
        /// <param name="retryAction">The Action you wish to attempt to try</param>
        /// <param name="transientExceptionStrategy">The Transcient Exception Strategy you wish to use to determine if a caught exception is safe for retry</param>
        /// <param name="retryInterval">The interval you would like to wait between attempts, DEFAULT is 2 seconds</param>
        /// <param name="maxRetryCount">The maximum number of times you wish to attempt execution of the provided action, DEFAULT is 3 attempts</param>
        /// <param name="optionalLogDelegate">An optional delegate which can be used for logging retry attempts</param>
        public static void IfNeeded(Action retryAction, Func<Exception, bool> transientExceptionStrategy,
            TimeSpan? retryInterval = null, int maxRetryCount = 3, Action<int, Exception> optionalLogDelegate = null)
        {
            IfNeeded<object>(() =>
            {
                retryAction();
                return null;
            }, transientExceptionStrategy, retryInterval, maxRetryCount, optionalLogDelegate);
        }

        /// <summary>
        /// Will attempt an Func a given number of times, with a given interval wait between attempts, 
        /// which implements a Transient Exception Strategy Predicate and an optional Logging Delegate
        /// This is the Async version
        /// </summary>
        /// <param name="retryFunc">The Action you wish to attempt to try</param>
        /// <param name="transientExceptionStrategy">The Transcient Exception Strategy you wish to use to determine if a caught exception is safe for retry</param>
        /// <param name="retryInterval">The interval you would like to wait between attempts, DEFAULT is 2 seconds</param>
        /// <param name="maxRetryCount">The maximum number of times you wish to attempt execution of the provided action, DEFAULT is 3 attempts</param>
        /// <param name="optionalLogDelegate">An optional delegate which can be used for logging retry attempts</param>
        public static async Task<T> IfNeededAsync<T>(Func<Task<T>> retryFunc, Func<Exception, bool> transientExceptionStrategy,
            TimeSpan? retryInterval = null, int maxRetryCount = 3, Action<int, Exception> optionalLogDelegate = null)
        {
            if (transientExceptionStrategy == null)
                throw new ArgumentNullException(nameof(transientExceptionStrategy));
            if (retryFunc == null)
                throw new ArgumentNullException(nameof(retryFunc));

            var encounteredExceptions = new List<Exception>();
            for (var currentRetryCount = 1; currentRetryCount <= maxRetryCount; currentRetryCount++)
            {
                try
                {
                    return await retryFunc();
                }
                catch (Exception exception)
                {
                    encounteredExceptions.Add(exception);
                    if (!ExceptionIsTransient(transientExceptionStrategy, encounteredExceptions)) continue;
                    optionalLogDelegate?.Invoke(currentRetryCount, exception); //an optional action can be used to pass in a logging delegate, for insance. optionalDelegate should implement it's own exception handling probably, if needed.
                    Thread.Sleep(retryInterval ?? TimeSpan.FromSeconds(2));
                }
            }

            throw new AggregateException(OutOfRetriesMessage, encounteredExceptions); //out of retries
        }

        /// <summary>
        /// Will attempt an Func a given number of times, with a given interval wait between attempts, 
        /// which implements a Transient Exception Strategy Predicate and an optional Logging Delegate
        /// </summary>
        /// <param name="retryFunc">The Func you wish to attempt to try</param>
        /// <param name="transientExceptionStrategy">The Transcient Exception Strategy you wish to use to determine if a caught exception is safe for retry</param>
        /// <param name="retryInterval">The interval you would like to wait between attempts, DEFAULT is 2 seconds</param>
        /// <param name="maxRetryCount">The maximum number of times you wish to attempt execution of the provided action, DEFAULT is 3 attempts</param>
        /// <param name="optionalLogDelegate">An optional delegate which can be used for logging retry attempts</param>
        public static T IfNeeded<T>(Func<T> retryFunc, Func<Exception, bool> transientExceptionStrategy,
            TimeSpan? retryInterval = null, int maxRetryCount = 3, Action<int, Exception> optionalLogDelegate = null)
        {
            if (transientExceptionStrategy == null)
                throw new ArgumentNullException(nameof(transientExceptionStrategy));
            if (retryFunc == null)
                throw new ArgumentNullException(nameof(retryFunc));

            var encounteredExceptions = new List<Exception>();
            for (var currentRetryCount = 1; currentRetryCount <= maxRetryCount; currentRetryCount++)
            {
                try
                {
                    return retryFunc();
                }
                catch (Exception exception)
                {
                    encounteredExceptions.Add(exception);
                    if (!ExceptionIsTransient(transientExceptionStrategy, encounteredExceptions)) continue;
                    optionalLogDelegate?.Invoke(currentRetryCount, exception); //an optional action can be used to pass in a logging delegate, for insance. optionalDelegate should implement it's own exception handling probably, if needed.
                    Thread.Sleep(retryInterval ?? TimeSpan.FromSeconds(2));
                }
            }

            throw new AggregateException(OutOfRetriesMessage, encounteredExceptions); //out of retries
        }


        /// <summary>
        /// Executes the provided Transient Exception Strategy
        /// </summary>
        static bool ExceptionIsTransient(Func<Exception, bool> trainsientExceptionStrategy,
            IReadOnlyCollection<Exception> encounteredExceptions)
        {
            if (trainsientExceptionStrategy(encounteredExceptions.Last())) // the exception at the bottom of the stack is the one we want to check
                return true;

            if (encounteredExceptions.Count == 1)
                throw encounteredExceptions.Single(); //if there is only one exception encountered, better to throw it unmanipulated.

            throw new AggregateException(NonTransientExceptionEncounteredMessage, encounteredExceptions);
        }
    }
}
