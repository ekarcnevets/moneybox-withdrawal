using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App
{
    public class Account
    {
        public const decimal PayInLimit = 4000m;
        public const decimal FundsLowThreshold = 500m;
        public const decimal ApproachingPayInLimitThreshold = 500m;

        public Guid Id { get; private set; }

        public User User { get; private set; }

        public decimal Balance { get; private set; }

        public decimal Withdrawn { get; private set; }

        public decimal PaidIn { get; private set; }

        public Account(Guid id, User user, decimal balance = 0m, decimal paidIn = 0m, decimal withdrawn = 0m)
        {
            Id = id;
            User = user;
            Balance = balance;
            PaidIn = paidIn;
            Withdrawn = withdrawn;
        }

        public void PayIn(decimal amount, INotificationService notificationService)
        {
            if (amount <= 0m)
            {
                throw new InvalidOperationException("Pay In amount must be positive and non-zero");
            }

            var newPaidIn = PaidIn + amount;
            if (newPaidIn > PayInLimit)
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            if (PayInLimit - newPaidIn < 500m)
            {
                notificationService.NotifyApproachingPayInLimit(User.Email);
            }

            Balance += amount;
            PaidIn = newPaidIn;
        }

        public void Withdraw(decimal amount, INotificationService notificationService)
        {
            if (amount <= 0m)
            {
                throw new InvalidOperationException("Withdraw amount must be positive and non-zero");
            }

            var newBalance = Balance - amount;
            if (newBalance < 0m)
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }

            if (newBalance < 500m)
            {
                notificationService.NotifyFundsLow(User.Email);
            }

            Balance = newBalance;
            Withdrawn -= amount;
        }
    }
}
