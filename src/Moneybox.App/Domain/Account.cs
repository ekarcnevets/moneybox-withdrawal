using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App
{
    public class Account
    {
        public const decimal PayInLimit = 4000m;
        public const decimal FundsLowThreshold = 500m;
        public const decimal ApproachingPayInLimitThreshold = 500m;

        public Guid Id { get; set; }

        public User User { get; set; }

        public decimal Balance { get; set; }

        public decimal Withdrawn { get; set; }

        public decimal PaidIn { get; set; }

        public void PayIn(decimal amount, INotificationService notificationService)
        {
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
