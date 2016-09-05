# System.Retry
A Retry Helper which adheres to MSDN's Retry Pattern guidelines
This allows you to safely retry an action; defining a Transient Exception Strategy to determine if it is safe to retry, or if it should throw the exception encountered.


* WARNING
 Any usages of a retry pattern should be idempotent: meaning, if the retryAction affects data, repeated retries need to have the same NET effect on the data. 
 If usages of this class modify data (e.g.;, executing commands to a service which creates orders in a database), then it's possible the retry pattern could result in multiple additions. Validation needs to be done on the Reciever-Side for these types of actions to prevent this and ensure Idempotency. 
 This is why it is only safe to use under indempotent conditions.
 
- Tested with NUnit

