using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App.Features
{
    public class TransferMoney
    {
        private readonly IAccountRepository accountRepository;
        private readonly INotificationService notificationService;

        public TransferMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this.accountRepository = accountRepository;
            this.notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            if (fromAccountId.Equals(toAccountId))
            {
                throw new InvalidOperationException("Cannot transfer from and to the same account");
            }

            var from = this.accountRepository.GetAccountById(fromAccountId);
            var to = this.accountRepository.GetAccountById(toAccountId);

            // This should be atomic, would need to consider only sending
            // notifications out if both succeed
            from.Withdraw(amount, notificationService);
            to.PayIn(amount, notificationService);

            this.accountRepository.Update(from);
            this.accountRepository.Update(to);
        }
    }
}
