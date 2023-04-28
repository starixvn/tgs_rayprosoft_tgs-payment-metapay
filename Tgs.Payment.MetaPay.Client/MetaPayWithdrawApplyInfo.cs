using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.Payment.MetaPay;
public record MetaPayWithdrawApplyInfo(
    MetaPayWithdrawSettings Settings,
    MetaPayWithdrawResponse Response);
