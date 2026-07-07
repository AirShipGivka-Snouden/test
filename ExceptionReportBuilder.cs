using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services;

public static class ExceptionReportBuilder
{
	internal const int MaxReportsPerKey = 3;

	private static readonly ConcurrentDictionary<string, int> SentCounts = new ConcurrentDictionary<string, int>();

	public static ExceptionReport? TryBuild(Exception exception, string? source, bool fatal)
	{
		(Exception Root, string OuterType) tuple = Unwrap(exception);
		Exception item = tuple.Root;
		string item2 = tuple.OuterType;
		string key = $"{item.GetType().Name}|{item.Message}|{source}";
		int num = SentCounts.AddOrUpdate(key, 1, (string _, int count) => count + 1);
		if (num > 3 && !fatal)
		{
			return null;
		}
		return new ExceptionReport
		{
			Message = item.Message,
			StackTrace = (item.StackTrace ?? exception.StackTrace ?? string.Empty),
			TypeName = item.GetType().Name,
			Source = (item.Source ?? exception.Source ?? string.Empty),
			TargetSite = (GetTargetSiteName(item) ?? GetTargetSiteName(exception) ?? string.Empty),
			MethodName = (source ?? string.Empty),
			Fatal = fatal,
			InnerExceptionMessages = BuildInnerChain(exception),
			OuterType = item2,
			Occurrence = num
		};
	}

	private static (Exception Root, string OuterType) Unwrap(Exception exception)
	{
		Exception ex = exception;
		int num = 0;
		while (num < 8)
		{
			if (1 == 0)
			{
			}
			Exception ex3;
			if (!(ex is AggregateException aggregate))
			{
				if (ex is TargetInvocationException ex2)
				{
					Exception innerException = ex.InnerException;
					if (innerException == null)
					{
						goto IL_0064;
					}
					ex3 = ex2.InnerException;
				}
				else
				{
					if (!(ex is TaskSchedulerException ex4))
					{
						goto IL_0064;
					}
					ex3 = ex4.InnerException;
				}
			}
			else
			{
				ex3 = FirstInner(aggregate);
			}
			goto IL_0069;
			IL_0069:
			if (1 == 0)
			{
			}
			Exception ex5 = ex3;
			if (ex5 == null)
			{
				break;
			}
			ex = ex5;
			num++;
			continue;
			IL_0064:
			ex3 = null;
			goto IL_0069;
		}
		return (ex == exception) ? (Root: exception, OuterType: string.Empty) : (Root: ex, OuterType: exception.GetType().Name);
	}

	private static Exception? FirstInner(AggregateException aggregate)
	{
		AggregateException ex = aggregate.Flatten();
		return (ex.InnerExceptions.Count > 0) ? ex.InnerExceptions[0] : null;
	}

	private static string BuildInnerChain(Exception exception)
	{
		List<string> list = new List<string>();
		Exception innerException = exception.InnerException;
		while (innerException != null && list.Count < 16)
		{
			list.Add(innerException.Message);
			innerException = innerException.InnerException;
		}
		if (exception is AggregateException ex)
		{
			ReadOnlyCollection<Exception> innerExceptions = ex.Flatten().InnerExceptions;
			for (int i = 1; i < innerExceptions.Count && i <= 8; i++)
			{
				list.Add("[sibling] " + innerExceptions[i].GetType().Name + ": " + innerExceptions[i].Message);
			}
		}
		return string.Join(" -> ", list);
	}

	private static string? GetTargetSiteName(Exception exception)
	{
		try
		{
			return exception.TargetSite?.Name;
		}
		catch
		{
			return null;
		}
	}

	internal static void ResetForTests()
	{
		SentCounts.Clear();
	}
}
