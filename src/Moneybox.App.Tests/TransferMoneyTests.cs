using System;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moneybox.App.Tests.DomainFactories;
using System.Collections.Generic;

namespace Moneybox.App.Tests
{
    [TestFixture]
    public class TransferMoneyTests
    {
        private Mock<IAccountRepository> _accountRepositoryMock;
        private Mock<INotificationService> _notificationServiceMock;

        private Dictionary<Guid, Account> _updatedAccounts;

        private TransferMoney _transferMoney;

        [SetUp]
        public void SetUp()
        {
            _updatedAccounts = new Dictionary<Guid, Account>();

            _accountRepositoryMock = new Mock<IAccountRepository>();

            _accountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.DefaultFrom))
                .Returns(TestAccountFactory.NewDefaultFrom());
            _accountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.DefaultTo))
                .Returns(TestAccountFactory.NewDefaultTo());
            _accountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.FundsLow))
                .Returns(TestAccountFactory.NewFundsLow());
            _accountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.ApproachingPayInLimit))
                .Returns(TestAccountFactory.NewApproachingPayInLimit());
            _accountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.AtPayInLimit))
                .Returns(TestAccountFactory.NewAtPayInLimit());

            _accountRepositoryMock
                .Setup(x => x.Update(It.IsAny<Account>()))
                .Callback<Account>(x =>
                {
                    // Note: this is an assumption based on the fact that we want to be atomic with account updates
                    if (_updatedAccounts.ContainsKey(x.Id))
                        throw new InvalidOperationException("Accounts should only be updated once");

                    _updatedAccounts.Add(x.Id, x);
                });

            _notificationServiceMock = new Mock<INotificationService>();

            _notificationServiceMock.Setup(x => x.NotifyFundsLow(It.IsAny<string>()));
            _notificationServiceMock.Setup(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()));

            _transferMoney = new TransferMoney(_accountRepositoryMock.Object, _notificationServiceMock.Object);
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
            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            _notificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that neither account was updated
            _accountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
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
            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            _notificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);

            // Ensure that neither account was updated
            _accountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }

        [Test]
        public void FromUserNotifiedOnFundsLow()
        {
            var amount = 1m;

            _transferMoney.Execute(
                TestAccountFactory.Ids.FundsLow,
                TestAccountFactory.Ids.DefaultTo,
                amount);

            // TODO: Shouldn't be creating a new account here instead of using the same one that the service will create
            var fromUser = TestAccountFactory.NewFundsLow();

            // Ensure that only funds low notification sent
            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
            _notificationServiceMock.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Once);
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
            var fromUser = TestAccountFactory.NewFundsLow();

            // Ensure that only approaching pay in limit notification sent
            _notificationServiceMock.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);
            _notificationServiceMock.Verify(x => x.NotifyFundsLow(fromUser.User.Email), Times.Never);
        }

        [Test]
        public void SuccessfulTransferUpdatesFromAndToAccounts([Values(1, 2000, 4000)] decimal amount)
        {
            _transferMoney.Execute(
                TestAccountFactory.Ids.DefaultFrom,
                TestAccountFactory.Ids.DefaultTo,
                amount);

            // Ensure that both accounts are updated
            _accountRepositoryMock.Verify(x => x.Update(It.IsAny<Account>()), Times.Exactly(2));

            _updatedAccounts.TryGetValue(TestAccountFactory.Ids.DefaultFrom, out var fromAfter).Should().BeTrue();
            _updatedAccounts.TryGetValue(TestAccountFactory.Ids.DefaultTo, out var toAfter).Should().BeTrue();

            // Ensure that the from balance and withdrawn properties have correct values after the transfer
            fromAfter.Balance.Should().Be(TestAccountFactory.DefaultBalance - amount);
            toAfter.Balance.Should().Be(TestAccountFactory.DefaultBalance + amount);
        }
    }
}
