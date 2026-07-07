using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Services.Interfaces;
using Microsoft.Win32;
using Serilog;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace Ciphra.VPN.WinUI.Services;

public class WinUiDeviceIdService : IDeviceIdService
{
	private const string DeviceIdSettingKey = "CiphraDeviceId";

	private static readonly Lazy<string> _deviceId = new Lazy<string>(LoadOrCompute, LazyThreadSafetyMode.ExecutionAndPublication);

	public Task<string> GetDeviceId()
	{
		return Task.FromResult(_deviceId.Value);
	}

	public string GetPlatform()
	{
		return "windows";
	}

	private static string LoadOrCompute()
	{
		try
		{
			IPropertySet values = ApplicationData.Current.LocalSettings.Values;
			if (((IDictionary<string, object>)values).TryGetValue("CiphraDeviceId", out object value) && value is string text && !string.IsNullOrEmpty(text))
			{
				return text;
			}
			string text2 = ComputeLegacyDeviceId();
			try
			{
				((IDictionary<string, object>)values)["CiphraDeviceId"] = text2;
				Log.Information("WinUiDeviceIdService: persisted new device_id to LocalSettings");
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "WinUiDeviceIdService: failed to persist device_id; will recompute on next launch");
			}
			return text2;
		}
		catch (Exception ex2)
		{
			Log.Warning(ex2, "WinUiDeviceIdService: LocalSettings unavailable; using process-scoped device_id");
			return ComputeLegacyDeviceId();
		}
	}

	private static string ComputeLegacyDeviceId()
	{
		string machineName = Environment.MachineName;
		string name = CultureInfo.CurrentUICulture.Name;
		int processorCount = Environment.ProcessorCount;
		string path = ApplicationData.Current.LocalFolder.Path;
		string text = $"{"lkadsja;lkdjhgoaerhgopqruhg1039ghq3irvnlqevjh0349tj[8hg[913rhgq038vh[0hg13408hg0835"}:{machineName}-{name}-{processorCount}-{path}";
		string machineGuid = GetMachineGuid();
		if (!string.IsNullOrEmpty(machineGuid))
		{
			text = text + "-" + machineGuid;
		}
		SystemIdentificationInfo systemIdForPublisher = SystemIdentification.GetSystemIdForPublisher();
		if (systemIdForPublisher != (SystemIdentificationInfo)null)
		{
			byte[] array = new byte[systemIdForPublisher.Id.Length];
			DataReader.FromBuffer(systemIdForPublisher.Id).ReadBytes(array);
			text = text + "-" + Convert.ToBase64String(array);
		}
		using SHA256 sHA = SHA256.Create();
		byte[] array2 = sHA.ComputeHash(Encoding.UTF8.GetBytes(text));
		return BitConverter.ToString(array2).Replace("-", "");
	}

	private static string GetMachineGuid()
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
		return registryKey?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
	}
}
