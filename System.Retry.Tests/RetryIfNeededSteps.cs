using System.Collections.Generic;
using System.Diagnostics;
using TechTalk.SpecFlow;
using System.InternetTime;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;

namespace System.Retry.Tests
{
    [Binding]
    public class RetryIfNeededSteps
    {
        Client _fakeClient;
        int _retryAttempts;
        static Func<Exception, bool> TransientExceptionStrategy => exception => exception is WebException || ((AggregateException)exception).InnerException is WebException;
        public Action GetTimeSynchronous { get; private set; }
        public Func<Task<DateTime?>> GetTimeAsynchronous { get; private set; }
        public Action ResultDelegate { get; private set; }
        public Func<Task> ResultDelegateFunc { get; private set; }

        [Given(@"I have supplied a retry action")]
        public void GivenIHaveSuppliedARetryAction()
        {
            GetTimeSynchronous = () => new NistTime(_fakeClient).Get();
        }

        [Given(@"I have defined a maximum of (.*) retry attempts")]
        public void GivenIHaveDefinedAMaximumOfRetryAttempts(int p0)
        {
            _retryAttempts = p0;
            Func<string, double> func = NistTime.NistReponseToMillisecondsFunction;
            _fakeClient =
                A.Fake<Client>(
                    options =>
                        options.WithArgumentsForConstructor(new object[]
                        {
                            NistTime.NistUrl,
                            NistTime.NistMediaTypeHeaderValue,
                            func
                        }));
            A.CallTo(()=> _fakeClient.Url).ReturnsNextFromSequence(GetFakeResponseSequence(_retryAttempts).ToArray());
        }

        static IEnumerable<string> GetFakeResponseSequence(int retryAttempts)
        {
            for (var i = 0; i < retryAttempts; i++)
                yield return null;
        }

        [Given(@"I have supplied an async request to retry")]
        public void GivenIHaveSuppliedAnAsyncRequestToRetry()
        {
            GetTimeAsynchronous = async () => await new NistTime(_fakeClient).GetAsync();
        }

        [When(@"I retry and exceed my maximum")]
        public void WhenIRetryAndExceedMyMaximum()
        {
            //todo: how to exceed maximum
            if (GetTimeSynchronous != null)
                ResultDelegate = () => Retry.IfNeeded(() => GetTimeSynchronous, TransientExceptionStrategy, maxRetryCount: _retryAttempts);
            else if (GetTimeAsynchronous != null)
                ResultDelegateFunc = async () => await Retry.IfNeededAsync(GetTimeAsynchronous, TransientExceptionStrategy, maxRetryCount: _retryAttempts);
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
    }
}
