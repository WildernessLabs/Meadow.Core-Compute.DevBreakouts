﻿using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace CoreComputerDevDualEthernet_Demo
{
    // Change F7CoreComputeV2 to F7FeatherV2 (or F7FeatherV1) for Feather boards
    public class MeadowApp : App<F7CoreComputeV2>
    {
        public override async Task Run()
        {
            Resolver.Log.Info("Run...");

            Resolver.Log.Info("Hello, Meadow Core-Compute!");

            TestSDCard();

            await TestEthernet();

            Console.WriteLine("Testing complete");
        }

        private async Task TestEthernet()
        {
            var ethernet = Device.NetworkAdapters.Primary<IWiredNetworkAdapter>();

            if (ethernet.IsConnected)
            {
                DisplayNetworkInformation();

                await GetWebPageViaHttpClient("https://postman-echo.com/get?foo1=bar1&foo2=bar2");
            }
        }

        public void DisplayNetworkInformation()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            if (adapters.Length == 0)
            {
                Resolver.Log.Warn("No adapters available");
            }
            else
            {
                foreach (NetworkInterface adapter in adapters)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    Resolver.Log.Info("");
                    Resolver.Log.Info(adapter.Description);
                    Resolver.Log.Info(string.Empty.PadLeft(adapter.Description.Length, '='));
                    Resolver.Log.Info($"  Adapter name: {adapter.Name}");
                    Resolver.Log.Info($"  Interface type .......................... : {adapter.NetworkInterfaceType}");
                    Resolver.Log.Info($"  Physical Address ........................ : {adapter.GetPhysicalAddress()}");
                    Resolver.Log.Info($"  Operational status ...................... : {adapter.OperationalStatus}");

                    string versions = string.Empty;

                    if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        versions = "IPv4";
                    }

                    if (adapter.Supports(NetworkInterfaceComponent.IPv6))
                    {
                        if (versions.Length > 0)
                        {
                            versions += " ";
                        }
                        versions += "IPv6";
                    }

                    Resolver.Log.Info($"  IP version .............................. : {versions}");

                    if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        IPv4InterfaceProperties ipv4 = properties.GetIPv4Properties();
                        Resolver.Log.Info($"  MTU ..................................... : {ipv4.Mtu}");
                    }

                    if ((adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) || (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    {
                        foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                Resolver.Log.Info($"  IP address .............................. : {ip.Address}");
                                Resolver.Log.Info($"  Subnet mask ............................. : {ip.IPv4Mask}");
                            }
                        }
                    }
                }
            }
        }
        public async Task GetWebPageViaHttpClient(string uri)
        {
            Resolver.Log.Info($"Requesting {uri} - {DateTime.Now}");

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 5, 0);

                HttpResponseMessage response = await client.GetAsync(uri);

                try
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Resolver.Log.Info(responseBody);
                }
                catch (TaskCanceledException)
                {
                    Resolver.Log.Info("Request time out.");
                }
                catch (Exception e)
                {
                    Resolver.Log.Info($"Request went sideways: {e.Message}");
                }
            }
        }

        private void TestSDCard()
        {
            var sdCardStorage = Device.PlatformOS.ExternalStorage.FirstOrDefault();

            if (sdCardStorage != null)
            {
                using (var file = File.CreateText(Path.Combine(sdCardStorage.Directory.FullName, "hello_meadow.txt")))
                {
                    file.Write("Hello, Meadow!");
                }

                // check on that file.
                FileStatus(Path.Combine(sdCardStorage.Directory.FullName, "hello_meadow.txt"));
            }
            else
            {
                Console.WriteLine("Could not detect SD card, check that it's inserted and formatted to FAT32");
            }
        }

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");

            Device.PlatformOS.ExternalStorageEvent += PlatformOS_ExternalStorageEvent;

            return base.Initialize();
        }

        void PlatformOS_ExternalStorageEvent(IExternalStorage storage, ExternalStorageState state)
        {
            // The affected storage and what happened to it are available from the event arguments.
            Resolver.Log.Info($"Storage Event: {storage.Directory.FullName} is {state}");
        }

        void CreateFile(string path, string filename)
        {
            Console.WriteLine($"Creating '{path}/{filename}'...");

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Directory doesn't exist, creating.");
                Directory.CreateDirectory(path);
            }

            try
            {
                using (var fs = File.CreateText(Path.Combine(path, filename)))
                {
                    fs.WriteLine("Hello Meadow File!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void FileStatus(string path)
        {
            Console.Write($"FileStatus() File: {Path.GetFileName(path)} ");
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    Console.WriteLine($"Size: {stream.Length,-8}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}