using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Tests.DomainFactories;

namespace Moneybox.App.Tests.Features
{
    [TestFixture]
    public abstract class BaseFeatureTest
    {
        protected Mock<IAccountRepository> AccountRepositoryMock;
        protected Mock<INotificationService> NotificationServiceMock;

        protected Dictionary<Guid, Account> UpdatedAccounts;

        [SetUp]
        public virtual void SetUp()
        {
            UpdatedAccounts = new Dictionary<Guid, Account>();

            AccountRepositoryMock = new Mock<IAccountRepository>();

            AccountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.DefaultFrom))
                .Returns(TestAccountFactory.NewDefaultFrom());
            AccountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.DefaultTo))
                .Returns(TestAccountFactory.NewDefaultTo());
            AccountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.FundsLow))
                .Returns(TestAccountFactory.NewFundsLow());
            AccountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.ApproachingPayInLimit))
                .Returns(TestAccountFactory.NewApproachingPayInLimit());
            AccountRepositoryMock
                .Setup(x => x.GetAccountById(TestAccountFactory.Ids.AtPayInLimit))
                .Returns(TestAccountFactory.NewAtPayInLimit());

            AccountRepositoryMock
                .Setup(x => x.Update(It.IsAny<Account>()))
                .Callback<Account>(x =>
                {
                    // Note: this is an assumption based on the fact that we want to be atomic with account updates
                    if (UpdatedAccounts.ContainsKey(x.Id))
                        throw new InvalidOperationException("Accounts should only be updated once");

                    UpdatedAccounts.Add(x.Id, x);
                });

            NotificationServiceMock = new Mock<INotificationService>();

            NotificationServiceMock.Setup(x => x.NotifyFundsLow(It.IsAny<string>()));
            NotificationServiceMock.Setup(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()));
        }
    }
}
