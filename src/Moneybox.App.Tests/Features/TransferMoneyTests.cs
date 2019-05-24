using System;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Moneybox.App.Features;
using Moneybox.App.Tests.DomainFactories;

namespace Moneybox.App.Tests.Features
{
    public class TransferMoneyTests : BaseFeatureTest
    {
        private TransferMoney _transferMoney;

        public override void SetUp()
        {
            base.SetUp();

            _transferMoney = new TransferMoney(AccountRepositoryMock.Object, NotificationServiceMock.Object);
        }

        [Test]
        public void ExceptionThrownOnInsufficientFunds()
        {
            var amount = TestAccountFactory.DefaultBalance + 1m;

            Action transferAction = () => _transferMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                TestAccountFactory.Ids.DefaultTo,
                amount);

            transferAction.Should().Throw<InvalidOperationException>();

            // Ensure that no notifications were sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that neither account was updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }

        [Test]
        public void ExceptionThrownOnPayInLimitReached()
        {
            var amount = 1m;

            Action transferAction = () => _transferMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                TestAccountFactory.Ids.AtPayInLimit,
                amount);

            transferAction.Should().Throw<InvalidOperationException>();

            // Ensure that no notifications were sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that neither account was updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }

        [Test]
        public void FromUserNotifiedOnFundsLow()
        {
            var amount = 1m;

            _transferMoney.Execute(
                TestAccountFactory.Ids.FundsLow,
                TestAccountFactory.Ids.DefaultTo,
                amount);

            // TODO: Shouldn't be creating a new account here just to get its email
            var fromAccount = TestAccountFactory.NewFundsLow();

            // Ensure that only funds low notification sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(fromAccount.User.Email), Times.Once);
        }

        [Test]
        public void ToUserNotifiedOnApproachingPayInLimit()
        {
            var amount = 1m;

            _transferMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                TestAccountFactory.Ids.ApproachingPayInLimit,
                amount);

            // TODO: Shouldn't be creating a new account here just to get its email
            var fromAccount = TestAccountFactory.NewFundsLow();

            // Ensure that only approaching pay in limit notification sent
            NotificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);
            NotificationServiceMock.Verify(x => x.NotifyFundsLow(fromAccount.User.Email), Times.Never);
        }

        [Test]
        public void SuccessfulTransferUpdatesFromAndToAccounts([Values(1, 2000, 4000)] decimal amount)
        {
            _transferMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                TestAccountFactory.Ids.DefaultTo,
                amount);

            // Ensure that both accounts are updated
            AccountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Exactly(2));

            UpdatedAccounts.TryGetValue(TestAccountFactory.Ids.DefaultFrom, out var fromAfter).Should().BeTrue();
            UpdatedAccounts.TryGetValue(TestAccountFactory.Ids.DefaultTo, out var toAfter).Should().BeTrue();

            // Ensure that the balance and withdrawn properties have correct values after the transfer
            fromAfter.Balance.Should().Be(TestAccountFactory.DefaultBalance - amount);
            fromAfter.Withdrawn.Should().Be(TestAccountFactory.DefaultWithdrawn - amount);
            toAfter.Balance.Should().Be(TestAccountFactory.DefaultBalance + amount);
        }
    }
}
