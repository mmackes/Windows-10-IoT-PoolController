// Copyright (c) Microsoft. All rights reserved.

using System;
using Windows.ApplicationModel.AppService;
using Windows.Devices.Gpio;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PoolWebService
{
    public sealed partial class MainPage : Page
    {
        AppServiceConnection appServiceConnection;

        public MainPage()
        {
            InitializeComponent();
            InitializeAppSvc();
        }

        private async void InitializeAppSvc()
        {
            string WebServerStatus = "PoolWebServer failed to start. AppServiceConnectionStatus was not successful.";

            // Initialize the AppServiceConnection
            appServiceConnection = new AppServiceConnection();
            appServiceConnection.PackageFamilyName = "PoolWebServer_hz258y3tkez3a";
            appServiceConnection.AppServiceName = "App2AppComService";

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "Initialize");
                var response = await appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    WebServerStatus = "PoolWebServer failed to start.";
                    throw new Exception("Failed to send message");
                }
                appServiceConnection.RequestReceived += OnMessageReceived;
                WebServerStatus = "PoolWebServer started.";
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                txtWebServerStatus.Text = WebServerStatus;
            });
        }

        private async void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string msgRequest = message["Request"] as string;
            string msgResponse = message["Response"] as string;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                txtRequest.Text = msgRequest;
                txtResponse.Text = msgResponse;
            });
        }
    }
}
