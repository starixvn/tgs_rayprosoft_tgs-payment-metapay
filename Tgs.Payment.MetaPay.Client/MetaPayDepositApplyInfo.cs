using Tgs.Payment.MetaPay.Client;

namespace Tgs.Payment.MetaPay;
public record MetaPayDepositApplyInfo(
    MetaPayDepositSettings Settings,
    MetaPayDepositResponse Response);