using System.Text.Json.Serialization;

namespace Tgs.Payment.MetaPay;
internal record MetaPayClientDepositResponse(
    string AppId,
    int PlatRespCode,
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount,
    Uri RedirectUrl,
    string Msg,
    string Sign);