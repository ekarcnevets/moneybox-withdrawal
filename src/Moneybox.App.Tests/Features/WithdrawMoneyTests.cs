using System;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Moneybox.App.Features;
using Moneybox.App.Tests.DomainFactories;

namespace Moneybox.App.Tests.Features
{
    [TestFixture]
    public class WithdrawMoneyTests : BaseFeatureTest
    {
        private WithdrawMoney withdrawMoney;

        
        public override void SetUp()
        {
            base.SetUp();

            withdrawMoney = new WithdrawMoney(AccountRepositoryMock.Object, NotificationServiceMock.Object);
        }

        [Test]
        public void ExceptionThrownOnInsufficientFunds()
        {
            var amount = TestAccountFactory.DefaultBalance + 1m;

            Action withdrawalAction = () => withdrawMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                amount);

            withdrawalAction.Should().Throw<InvalidOperationException>();

            // Ensure that no notifications were sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that the account was not updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }

        [Test]
        public void ExceptionThrownOnNonPositiveAmount([Values(-1, 0)] decimal amount)
        {
            Action transferAction = () => withdrawMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                amount);

            transferAction.Should().Throw<InvalidOperationException>();

            // Ensure that no notifications were sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that the account was not updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }

        [Test]
        public void UserNotifiedOnFundsLow()
        {
            var amount = 1m;

            withdrawMoney.Execute(
                TestAccountFactory.Ids.FundsLow,
                amount);

            // TODO: Shouldn't be creating a new account here just to get its email
            var fromAccount = TestAccountFactory.NewFundsLow();

            // Ensure that only funds low notification sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(fromAccount.User.Email), Times.Once);
        }

        [Test]
        public void SuccessfulWithdrawUpdatesFromAccount([Values(1, 2000, 4000)] decimal amount)
        {
            withdrawMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                amount);

            // Ensure that the account is updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Exactly(1));

            UpdatedAccounts.TryGetValue(TestAccountFactory.Ids.DefaultFrom, out var fromAfter).Should().BeTrue();

            // Ensure that the from balance and withdrawn properties have correct values after the withdrawal
            fromAfter.Balance.Should().Be(TestAccountFactory.DefaultBalance - amount);
            fromAfter.Withdrawn.Should().Be(TestAccountFactory.DefaultWithdrawn - amount);
        }
    }
}
