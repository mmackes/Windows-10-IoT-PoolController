// Copyright (c) Microsoft. All rights reserved.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using System.IO;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Devices.Gpio;

namespace WebServerTask
{
    public sealed class WebServerBGTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;

            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null && appService.Name == "App2AppComService")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        //Processes message resquests sent from PoolWebService App
        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "Initialize":
                    {
                        Sensors.InitSensors();
                        Devices.InitDevices();

                        var messageDeferral = args.GetDeferral();
                        //Set a result to return to the caller
                        var returnMessage = new ValueSet();
                        //Define a new instance of our HTTPServer on Port 8888
                        HttpServer server = new HttpServer(8888, appServiceConnection);
                        IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                            (workItem) =>
                            {   //Start the Sever
                                server.StartServer();
                            });

                        //Respond back to PoolWebService with a Status of Success 
                        returnMessage.Add("Status", "Success");
                        var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                        messageDeferral.Complete();
                        break;
                    }

                case "Quit":
                    {
                        //Service was asked to quit. Give us service deferral
                        //so platform can terminate the background task
                        serviceDeferral.Complete();
                        break;
                    }
            }
        }
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //Clean up and get ready to exit
        }

        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection appServiceConnection;
    }

    //Class to define the HTTP WebServer
    public sealed class HttpServer : IDisposable
    {
        //Create a buffer to read HTTP data
        private const uint BufferSize = 8192;
        //Port to listen on
        private int port = 8888;
        //Listener to
        private readonly StreamSocketListener listener;
        //Connection to send status information back to PoolControllerWebService
        private AppServiceConnection appServiceConnection;

        public HttpServer(int serverPort, AppServiceConnection connection)
        {
            listener = new StreamSocketListener();
            port = serverPort; 
            appServiceConnection = connection;
            //Add event handler for HTTP connections
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        //Call to start the listner 
        public void StartServer()
        {
#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }


        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                StringBuilder request = new StringBuilder();
                //Get the incomming data
                using (IInputStream input = socket.InputStream)
                {
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    //Read all the incomming data
                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                //Got the data start processing a response
                using (IOutputStream output = socket.OutputStream)
                {
                    string requestMethod = request.ToString();
                    string[] requestParts = { "" };
                    if (requestMethod != null)
                    {
                        //Beakup the request into it parts
                        requestMethod = requestMethod.Split('\n')[0];
                        requestParts = requestMethod.Split(' ');
                    }
                    //We only respond HTTP GETS and POST methods
                    if (requestParts[0] == "GET")
                        await WriteGetResponseAsync(requestParts[1], output);
                    else if (requestParts[0] == "POST")
                        await WritePostResponseAsync(requestParts[1], output);
                    else
                        await WriteMethodNotSupportedResponseAsync(requestParts[1], output);
                }
            }
            catch (Exception) { }
        }

        //Handles all HTTP GET's
        private async Task WriteGetResponseAsync(string request, IOutputStream os)
        {
            bool urlFound = false;
            byte[] bodyArray = null;
            string responseMsg = "";
            //See if the request it matches any of the valid requests urls and create the response message
            switch (request.ToUpper())
            {
                case "/SENSORS/POOLTEMP":
                    responseMsg = Sensors.PoolTemperature;
                    urlFound = true;
                    break;
                case "/SENSORS/SOLARTEMP":
                    responseMsg = Sensors.SolarTemperature;
                    urlFound = true;
                    break;
                case "/SENSORS/OUTSIDETEMP":
                    responseMsg = Sensors.OutsideTemperature;
                    urlFound = true;
                    break;
                case "/DEVICES/POOLPUMP/STATE":
                    responseMsg = Devices.PoolPumpState;
                    urlFound = true;
                    break;
                case "/DEVICES/WATERFALLPUMP/STATE":
                    responseMsg = Devices.PoolWaterfallState;
                    urlFound = true;
                    break;
                case "/DEVICES/POOLLIGHTS/STATE":
                    responseMsg = Devices.PoolLightsState;
                    urlFound = true;
                    break;
                case "/DEVICES/YARDLIGHTS/STATE":
                    responseMsg = Devices.PoolLightsState;
                    urlFound = true;
                    break;
                case "/DEVICES/POOLSOLAR/STATE":
                    responseMsg = Devices.PoolSolarValveState;
                    urlFound = true;
                    break;
                default:
                    urlFound = false;
                    break;
            }

            bodyArray = Encoding.UTF8.GetBytes(responseMsg);
            await WriteResponseAsync(request.ToUpper(), responseMsg, urlFound, bodyArray, os);
        }

        //Handles all HTTP POST's
        private async Task WritePostResponseAsync(string request, IOutputStream os)
        {
            bool urlFound = false;
            byte[] bodyArray = null;
            string responseMsg = "";
            //See if the request it matches any of the valid requests urls and create the response message
            switch (request.ToUpper())
            {
                case "/DEVICES/POOLPUMP/OFF":
                    Devices.PoolPumpPinValue = GpioPinValue.Low;
                    bodyArray = Encoding.UTF8.GetBytes("OFF");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/POOLPUMP/ON":
                    Devices.PoolPumpPinValue = GpioPinValue.High;
                    bodyArray = Encoding.UTF8.GetBytes("ON");
                    responseMsg = "ON";
                    urlFound = true;
                    break;
                case "/DEVICES/WATERFALLPUMP/OFF":
                    Devices.PoolWaterfallPinValue = GpioPinValue.Low;
                    bodyArray = Encoding.UTF8.GetBytes("OFF");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/WATERFALLPUMP/ON":
                    Devices.PoolWaterfallPinValue = GpioPinValue.High;
                    bodyArray = Encoding.UTF8.GetBytes("ON");
                    responseMsg = "ON";
                    urlFound = true;
                    break;
                case "/DEVICES/POOLLIGHTS/OFF":
                    Devices.PoolLightsPinValue = GpioPinValue.Low;
                    bodyArray = Encoding.UTF8.GetBytes("OFF");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/POOLLIGHTS/ON":
                    Devices.PoolLightsPinValue = GpioPinValue.High;
                    bodyArray = Encoding.UTF8.GetBytes("ON");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/YARDLIGHTS/OFF":
                    Devices.YardLightsPinValue = GpioPinValue.Low;
                    bodyArray = Encoding.UTF8.GetBytes("OFF");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/YARDLIGHTS/ON":
                    Devices.YardLightsPinValue = GpioPinValue.High;
                    bodyArray = Encoding.UTF8.GetBytes("ON");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/POOLSOLAR/OFF":
                    Devices.PoolSolarValvePinValue = GpioPinValue.Low;
                    bodyArray = Encoding.UTF8.GetBytes("OFF");
                    responseMsg = "OFF";
                    urlFound = true;
                    break;
                case "/DEVICES/POOLSOLAR/ON":
                    Devices.PoolSolarValvePinValue = GpioPinValue.High;
                    bodyArray = Encoding.UTF8.GetBytes("ON");
                    responseMsg = "ON";
                    urlFound = true;
                    break;
                default:
                    bodyArray = Encoding.UTF8.GetBytes("");
                    urlFound = false;
                    break;
            }

            await WriteResponseAsync(request.ToUpper(), responseMsg, urlFound,bodyArray, os);
        }

        //Write the response for unsupported HTTP methods
        private async Task WriteMethodNotSupportedResponseAsync(string request, IOutputStream os)
        {
            bool urlFound = false;
            byte[] bodyArray = null;
            bodyArray = Encoding.UTF8.GetBytes("");
            await WriteResponseAsync(request.ToUpper(), "NOT SUPPORTED", urlFound, bodyArray, os);
        }

        //Write the response for HTTP GET's and POST's 
        private async Task WriteResponseAsync(string RequestMsg, string ResponseMsg, bool urlFound, byte[] bodyArray, IOutputStream os)
        {
            try  //The appService will die after a day or so. Let 's try catch it seperatly so the server will still return
            {
                var updateMessage = new ValueSet();
                updateMessage.Add("Request", RequestMsg);
                updateMessage.Add("Response", ResponseMsg);
                var responseStatus = await appServiceConnection.SendMessageAsync(updateMessage);
            }
            catch (Exception) { }

            try
            { 
            MemoryStream bodyStream = new MemoryStream(bodyArray);
                using (Stream response = os.AsStreamForWrite())
                {
                    string header = GetHeader(urlFound, bodyStream.Length.ToString());
                    byte[] headerArray = Encoding.UTF8.GetBytes(header);
                    await response.WriteAsync(headerArray, 0, headerArray.Length);
                    if (urlFound)
                        await bodyStream.CopyToAsync(response);
                    await response.FlushAsync();
                }
            }
            catch (Exception) { }
        }

        //Creates the HTTP header text for found and not found urls
        string GetHeader(bool urlFound, string bodyStreamLength)
        {
            string header;
            if (urlFound)
            {
                header = "HTTP/1.1 200 OK\r\n" +
                           "Access-Control-Allow-Origin: *\r\n" +
                           "Content-Type: text/plain\r\n" +
                           "Content-Length: " + bodyStreamLength + "\r\n" +
                           "Connection: close\r\n\r\n";
            }
            else
            {
                header = "HTTP/1.1 404 Not Found\r\n" +
                         "Access-Control-Allow-Origin: *\r\n" +
                         "Content-Type: text/plain\r\n" +
                         "Content-Length: 0\r\n" +
                         "Connection close\r\n\r\n";
            }
            return header;
        }
    }
}
