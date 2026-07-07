using System;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

public record TokenValidationResult(bool IsValid, TokenCheckResponse? Response, Exception? Exception);
