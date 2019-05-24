using System;

namespace Moneybox.App.Tests.DomainFactories
{
    // TODO: Cache?
    /// <summary>
    /// Factory class for creating Accounts with helpful state for testing against.
    /// </summary>
    public static class TestAccountFactory
    {
        /// <summary>
        /// Account Ids used for creating and recalling Accounts created by the <see cref="TestAccountFactory"/>.
        /// </summary>
        public static class Ids
        {
            public static readonly Guid DefaultFrom = new Guid("79B33415-4390-4BD2-AAC4-CD0A041F9191");
            public static readonly Guid DefaultTo = new Guid("70D8FD9A-29DB-49F4-AC2B-CFF3D5169423");
            public static readonly Guid FundsLow = new Guid("BFD9586B-87CB-44CA-A457-11925F19F5B2");
            public static readonly Guid ApproachingPayInLimit = new Guid("12D44BF4-43AC-4949-B84C-6E915E9032E1");
            public static readonly Guid AtPayInLimit = new Guid("FFCAE59D-43DA-495C-8CEC-8C006AB02752");
        }

        public const decimal DefaultBalance = 10000m;
        public const decimal DefaultPaidIn = 0m;
        public const decimal DefaultWithdrawn = 0m;

        // These thresholds from TransferMoney Execute method
        public const decimal FundsLowBalance = 500m;
        public const decimal ApproachingPayInLimitThresholdFrom = 500m;

        private static Account CreateAccount(Guid id, string userName, decimal balance = DefaultBalance, decimal paidIn = DefaultPaidIn)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = userName,
                Email = $"{userName}@email.com",
            };

            return new Account
            {
                Id = id,
                User = user,
                Balance = balance,
                Withdrawn = DefaultWithdrawn,
                PaidIn = paidIn,
            };
        }

        public static Account NewDefaultFrom()
        {
            return CreateAccount(Ids.DefaultFrom, nameof(Ids.DefaultFrom));
        }

        public static Account NewDefaultTo()
        {
            return CreateAccount(Ids.DefaultTo, nameof(Ids.DefaultTo));
        }

        public static Account NewFundsLow()
        {
            return CreateAccount(Ids.FundsLow, nameof(Ids.FundsLow), balance: FundsLowBalance);
        }

        public static Account NewApproachingPayInLimit()
        {
            return CreateAccount(Ids.ApproachingPayInLimit, nameof(Ids.ApproachingPayInLimit), paidIn: Account.PayInLimit - ApproachingPayInLimitThresholdFrom);
        }

        public static Account NewAtPayInLimit()
        {
            return CreateAccount(Ids.AtPayInLimit, nameof(Ids.AtPayInLimit), paidIn: Account.PayInLimit);
        }
    }
}
