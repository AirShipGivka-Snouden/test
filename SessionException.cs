using System;
using System.Collections.Generic;
using System.Text.Json;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.ApiClients;

namespace VpnHood.Core.Common.Exceptions;

public class SessionException : Exception
{
	public SessionResponse SessionResponse { get; }

	public SessionException(SessionResponse sessionResponse)
		: base(sessionResponse.ErrorMessage ?? MessageFromErrorCode(sessionResponse.ErrorCode))
	{
		SessionResponse = sessionResponse;
		SessionResponse sessionResponse2 = SessionResponse;
		if (sessionResponse2.ErrorMessage == null)
		{
			string text = (sessionResponse2.ErrorMessage = MessageFromErrorCode(sessionResponse.ErrorCode));
		}
		Data.Add("SessionResponse", JsonSerializer.Serialize(SessionResponse));
		Data.Add("ErrorCode", SessionResponse.ErrorCode.ToString());
		Data.Add("SuppressedBy", SessionResponse.SuppressedBy.ToString());
	}

	public SessionException(SessionErrorCode errorCode, string? message = null)
		: this(new SessionResponse
		{
			ErrorCode = errorCode,
			ErrorMessage = message
		})
	{
	}

	public SessionException(ApiError apiError)
		: base(apiError.Message)
	{
		if (!apiError.Is<SessionException>())
		{
			throw new ArgumentException("apiError is not a SessionException", "apiError");
		}
		string json = apiError.Data.GetValueOrDefault("SessionResponse") ?? throw new ArgumentException("apiError does not contain SessionResponse", "apiError");
		SessionResponse = JsonSerializer.Deserialize<SessionResponse>(json);
	}

	private static string? MessageFromErrorCode(SessionErrorCode errorCode)
	{
		return errorCode switch
		{
			SessionErrorCode.Ok => "Operation completed successfully.", 
			SessionErrorCode.AccessError => "An access error occurred.", 
			SessionErrorCode.PlanRejected => "The requested connection plan has been rejected.", 
			SessionErrorCode.GeneralError => "A general error occurred.", 
			SessionErrorCode.SessionClosed => "The session has been closed.", 
			SessionErrorCode.SessionSuppressedBy => "The session was suppressed by another session.", 
			SessionErrorCode.SessionError => "An error occurred in the session.", 
			SessionErrorCode.SessionExpired => "The session has expired.", 
			SessionErrorCode.AccessExpired => "Access has expired.", 
			SessionErrorCode.DailyLimitExceeded => "The daily limit has been exceeded.", 
			SessionErrorCode.AccessCodeRejected => "The access code was rejected.", 
			SessionErrorCode.AccessLocked => "Access is locked.", 
			SessionErrorCode.AccessTrafficOverflow => "Access traffic overflow occurred.", 
			SessionErrorCode.NoServerAvailable => "No server is available.", 
			SessionErrorCode.PremiumLocation => "The location is only available for premium accounts.", 
			SessionErrorCode.AdError => "An advertisement error occurred.", 
			SessionErrorCode.RewardedAdRejected => "The rewarded ad was rejected.", 
			SessionErrorCode.Maintenance => "The system is under maintenance.", 
			SessionErrorCode.RedirectHost => "The host is being redirected.", 
			SessionErrorCode.UnsupportedClient => "The client is unsupported.", 
			SessionErrorCode.UnsupportedServer => "The server is unsupported.", 
			_ => null, 
		};
	}
}
