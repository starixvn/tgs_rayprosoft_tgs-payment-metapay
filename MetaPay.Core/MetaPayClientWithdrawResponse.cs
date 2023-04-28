using System.Text.Json.Serialization;

namespace Tgs.Payment.MetaPay;
internal record MetaPayClientWithdrawResponse(
    string AppId,
    int PlatRespCode,
    string ThirdPartyOrderNo,
    Guid FormId,
    string PickupCenter,
    decimal Amount,
    string Msg,
    string Sign);
