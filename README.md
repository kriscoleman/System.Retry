# System.Retry
A Retry Helper which adheres to MSDN's Retry Pattern guidelines. 
This allows you to safely retry an action; defining a Transient Exception Strategy to determine if it is safe to retry, or if it should throw the exception encountered.

For more info on this pattern, see: https://msdn.microsoft.com/en-us/library/dn589788.aspx

* WARNING
 Any usages of a retry pattern should be idempotent: meaning, if the retryAction affects data, repeated retries need to have the same NET effect on the data. 
 If usages of this class modify data (e.g.;, executing commands to a service which creates orders in a database), then it's possible the retry pattern could result in multiple additions. Validation needs to be done on the Reciever-Side for these types of actions to prevent this and ensure Idempotency. 
 This is why it is only safe to use under indempotent conditions.
 
- Tested with NUnit

# Transient Exception Strategies
When executing a Retry action, a Transient Exception Stragegy is required. This is a simple Func<Exception, bool> predicate which tells Retry when it is safe to retry. If an exception does not meet the criteria of your strategy, it will roll up all encountered exceptions (transient and non-transient) and throw. 

- for example, a strategy that would only allow retries on WebExceptions would be: exception => exception is WebException

For more info on handling Transient Faults, see: https://msdn.microsoft.com/en-us/library/hh680901(v=pandp.50).aspx

# Out of Retries
When out of retries (with no non-transient exceptions encountered), Retry will throw an OutOfRetriesException, which you can catch to either ignore, log, or display a friendly message to the user. 

# Encountering Non-Transient Exceptions 
When Retry encounters an exception which your Transient Exception Strategy deems un-safe, it will respond in one of two ways. 
If the unsafe exception is the first and only exception encountered, it will immediately throw without manipulating it. 
if the unsafe exception is encountered after a few retry attemps, Retry will roll up all encountered exceptions (including the unsafe one) and throw them as an NonTransientEncounteredAfterRetriesException.

