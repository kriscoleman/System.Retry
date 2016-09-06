# System.Retry
A Retry Helper which adheres to MSDN's Retry Pattern guidelines. 
This allows you to safely retry an action; defining a Transient Exception Strategy to determine if it is safe to retry, or if it should throw the exception encountered.

# Examples
```javascript

// you can use it on synchrnous actions when you don't need to await, 
// perhaps when a background thread command queue manages the posting of your data
Retry.IfNeeded(()=> CloudService.Post(credentials)); 

// you can use it asynchronously to await data
return await Retry.IfNeeded(async ()=> CloudService.GetMyMessages(credentials));
 
// you can use closures if you need to be more verbose or complex
return await Retry.IfNeeded(async ()=>
 {
    var mesesages = await CloudService.GetMyMessages(credentials); 
    return messages;
 });
  
 //it's best to specify your own Transient exception strategy
 Retry.IfNeeded(()=> CloudService.Post(credentials), exception => exception is WebException); // will only retry if WebException

```

This can be particularly useful when making asynchronous web calls to a web service, when you expect to deal with latency or timeout issues. But it can be applied to any problem where a safe number of retries is appropriate. 

For more info on this pattern, see: https://msdn.microsoft.com/en-us/library/dn589788.aspx

# WARNING
 Any usages of a retry pattern should be idempotent: meaning, if the retryAction affects data, repeated retries need to have the same NET effect on the data. 
 If usages of this class modify data (e.g.; executing commands to a service which creates orders in a database), then it's possible the retry pattern could result in multiple additions. Validation needs to be done on the Reciever-Side for these types of actions to prevent this and ensure Idempotency. 
 This is why it is only safe to use under indempotent conditions.

# Transient Exception Strategies
 When executing a Retry action, a Transient Exception Stragegy is required. This is a simple Func<Exception, bool> predicate which tells Retry when it is safe to retry. If an exception does not meet the criteria of your strategy, it will roll up all encountered exceptions (transient and non-transient) and throw. 

- for example, a strategy that would only allow retries on WebExceptions would be: 

- exception => exception is WebException

For more info on handling Transient Faults, see: https://msdn.microsoft.com/en-us/library/hh680901(v=pandp.50).aspx

# Out of Retries
 When out of retries (with no non-transient exceptions encountered), Retry will throw an OutOfRetriesException, which you can catch to either ignore, log, or display a friendly message to the user. 

# Encountering Non-Transient Exceptions 
 When Retry encounters an exception which your Transient Exception Strategy deems un-safe, it will respond in one of two ways. 
 If the unsafe exception is the first and only exception encountered, it will immediately throw without manipulating it. 
 if the unsafe exception is encountered after a few retry attemps, Retry will roll up all encountered exceptions (including the unsafe one) and throw them as an NonTransientEncounteredAfterRetriesException.
 
  - Tested with NUnit
  - Documentation with help of GhostDoc, see: System.Retry/System.Retry.ReferenceGuide/System.Retry.chm
