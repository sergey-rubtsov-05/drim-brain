using System.Diagnostics;
using BlockchainService.Features.Withdrawals.Logging;
using Common.Telemetry;
using FluentValidation;
using MediatR;

namespace BlockchainService.Features.Withdrawals.Requests;

internal static class Withdraw
{
    internal class RequestValidator : AbstractValidator<WithdrawRequest>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Currency).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.CryptoAddress).NotEmpty();
        }
    }

    internal class RequestHandler(ILogger<RequestHandler> _logger) : IRequestHandler<WithdrawRequest, WithdrawReply>
    {
        public async Task<WithdrawReply> Handle(WithdrawRequest request, CancellationToken cancellationToken)
        {
            await EstimateFee(request.Currency, request.Amount, cancellationToken);

            var txId = await SendTransaction(request.Currency, request.Amount, request.CryptoAddress, cancellationToken);

            return new() { TxId = txId };
        }

        private async Task EstimateFee(string currency, double amount, CancellationToken cancellationToken)
        {
            using var activity = Tracing.ActivitySource.StartActivity();
            activity!.AddTag("currency", currency);
            activity.AddTag("amount", amount);

            await Task.Delay(Random.Shared.Next(5, 10), cancellationToken);

            WithdrawalsLogger.FeeEstimated(_logger);
        }

        private async Task<string> SendTransaction(string currency, double amount, string address,
            CancellationToken cancellationToken)
        {
            using var activity = Tracing.ActivitySource.StartActivity();
            activity!.AddTag("currency", currency);
            activity.AddTag("amount", amount);

            await Task.Delay(Random.Shared.Next(20, 50), cancellationToken);

            activity.AddEvent(new ActivityEvent("Transaction sent"));

            WithdrawalsLogger.TransactionSent(_logger);

            return Guid.NewGuid().ToString();
        }
    }
}
