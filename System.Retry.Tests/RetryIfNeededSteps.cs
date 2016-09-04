using System.Diagnostics;
using TechTalk.SpecFlow;
using System.InternetTime;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using FakeItEasy;

namespace System.Retry.Tests
{
    [Binding]
    public class RetryIfNeededSteps
    {
        int _retryAttempts; // use with faking after IoC refactor of InternetTime
        public Action GetTimeSynchronous { get; private set; }
        public Func<Task<DateTime?>> GetTimeAsynchronous { get; private set; }
        public Action ResultDelegate { get; private set; }
        public Func<Task> ResultDelegateFunc { get; private set; }

        [Given(@"I have supplied a retry action")]
        public void GivenIHaveSuppliedARetryAction()
        {
             GetTimeSynchronous = () => new NistTime().Get();
        }

        [Given(@"I have defined a maximum of (.*) retry attempts")]
        public void GivenIHaveDefinedAMaximumOfRetryAttempts(int p0)
        {
            _retryAttempts = p0;
        }
        
        [Given(@"I have supplied an async request to retry")]
        public void GivenIHaveSuppliedAnAsyncRequestToRetry()
        {
            GetTimeAsynchronous = async () => await new NistTime().GetAsync();
        }
        
        [When(@"I retry and exceed my maximum")]
        public void WhenIRetryAndExceedMyMaximum()
        {
            if (GetTimeSynchronous != null)
                 ResultDelegate = () => Retry.IfNeeded(() => GetTimeSynchronous, TransientExceptionStrategy);
            else if (GetTimeAsynchronous != null)
                ResultDelegateFunc = async () => await Retry.IfNeededAsync(GetTimeAsynchronous, TransientExceptionStrategy);
        }


        [Then(@"I will catch an OutOfRetries Exception")]
        public async Task ThenIWillCatchAnOutOfRetriesException()
        {
            if (ResultDelegate != null)
                Assert.Throws<OutOfRetriesException>(() => ResultDelegate());
            else
            {
                //later versions of NUnit support Assert.ThrowsAsync<>, but the version specflow depends on does not. We'll mimic that assert below:
                try
                {
                    Debug.Assert(ResultDelegateFunc != null, "ResultDelegateFunc != null");
                    await ResultDelegateFunc();
                }
                catch (Exception e)
                {
                    Assert.That(e is OutOfRetriesException, "Expected an OutOfRetries exception but did not encounter one.");
                }
            }
        }

        static Func<Exception, bool> TransientExceptionStrategy => exception => exception is WebException || ((AggregateException)exception).InnerException is WebException;
    }
}
