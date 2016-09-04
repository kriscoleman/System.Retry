Feature: RetryIfNeeded
	In order to avoid silly mistakes
	I want to make sure System.Retry works as intended

@synchronous
Scenario: I have an action I want to retry a certain number of times, and it exceeds the allowable number of retries
	Given I have supplied a retry action
	And I have defined a maximum of 5 retry attempts
	When I retry and exceed my maximum
	Then I will catch an OutOfRetries Exception

#@asynchronous
#Scenario: I have a async request I want to retry a certain number of times, and it exceeds the allowable number of retries
#	Given I have supplied an async request to retry
#	And I have defined a maximum of 5 retry attempts
#	When I retry and exceed my maximum
#	Then I will catch an OutOfRetries Exception

