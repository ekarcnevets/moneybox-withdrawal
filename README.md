# Moneybox Money Withdrawal

The solution contains a .NET core library (Moneybox.App) which is structured into the following 3 folders:

* Domain - this contains the domain models for a user and an account, and a notification service.
* Features - this contains two operations, one which is implemented (transfer money) and another which isn't (withdraw money)
* DataAccess - this contains a repository for retrieving and saving an account (and the nested user it belongs to)

## The task

The task is to implement a money withdrawal in the WithdrawMoney.Execute(...) method in the features folder. For consistency, the logic should be the same as the TransferMoney.Execute(...) method i.e. notifications for low funds and exceptions where the operation is not possible. 

As part of this process however, you should look to refactor some of the code in the TransferMoney.Execute(...) method into the domain models, and make these models less susceptible to misuse. We're looking to make our domain models rich in behaviour and much more than just plain old objects, however we don't want any data persistance operations (i.e. data access repositories) to bleed into our domain. This should simplify the task of implementing WithdrawMoney.Execute(...).

## Guidelines

* You should spend no more than 1 hour on this task, although there is no time limit
* You should fork or copy this repository into your own public repository (Github, BitBucket etc.) before you do your work
* Your solution must compile and run first time
* You should not alter the notification service or the the account repository interfaces
* You may add unit/integration tests using a test framework (and/or mocking framework) of your choice
* You may edit this README.md if you want to give more details around your work (e.g. why you have done something a particular way, or anything else you would look to do but didn't have time)

Once you have completed your work, send us a link to your public repository.

Good luck!

## Notes on changes made
### Account.cs
- Introduced constants for the thresholds to improve clarity of intent
- Made property setters private for security purposes preventing external changes. To change these we should be using methods on the model
- Added constructor to ensure that Accounts are complete on construction i.e. you can't only assign the Id and not the User
- Moved shared behaviour from TransferMoney and WithdrawMoney into this model (PayIn, Withdraw methods), taking the notification service as a parameter rather than a constructor injected property to keep the service layer out of the domain
- Added positive value check on amounts being paid in and withdrawn as negatives and zeroes don't make sense - zeroes would be okay except they might result in notifications even though the balance didn't change

### User.cs
- Made property setters private for security purposes preventing external changes. To change these we should be using methods on the model
- Added constructor to ensure that Users are complete on construction i.e. you can't only assign the Id and not the Name/Email

### TransferMoney.cs
- Added a check to prevent transfering money from and to the same account since it doesn't make sense

### TransferMoney.cs & WithdrawMoney.cs
- Made the IAccountRepository and the INotificationService readonly to prevent bugs from them being reassigned

### Possible improvements
- The transfer operation in TransferMoney() should be atomic i.e. transacted - this is especially important given we are transfering money. However it would need some refactoring to ensure the notifications are only sent out if both succeed.
- I don't like how the TestAccountFactory behaves - given time I would change it so that it keeps track of created accounts, and returns the same instance if it is requested again. This would also allow asserting directly against the model properties in tests, meaning I wouldn't need the UpdatedAccounts property in BaseFeatureTest.

## Testing libraries
I have used the following libraries from NuGet to help me test
- NUnit 3.10.1 as the testing framework
- Moq 4.10.1 as the mocking framework
- FluentAssertions 5.6.0 for making natural language assertions

