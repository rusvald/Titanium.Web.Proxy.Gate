﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Titanium.Web.Proxy.Examples.Wpf.Annotations;
using Titanium.Web.Proxy.Http;

namespace Titanium.Web.Proxy.Examples.Wpf
{
    public class SessionListItem : INotifyPropertyChanged
    {
        private long? bodySize;
        private Exception exception;
        private string host;
        private string process;
        private string protocol;
        private long receivedDataCount;
        private long sentDataCount;
        private string statusCode;
        private string url;

        public int Number { get; set; }

        public HttpWebClient WebSession { get; set; }

        public bool IsTunnelConnect { get; set; }

        public string StatusCode
        {
            get { return statusCode; }
            set { SetField(ref statusCode, value); }
        }

        public string Protocol
        {
            get { return protocol; }
            set { SetField(ref protocol, value); }
        }

        public string Host
        {
            get { return host; }
            set { SetField(ref host, value); }
        }

        public string Url
        {
            get { return url; }
            set { SetField(ref url, value); }
        }

        public long? BodySize
        {
            get { return bodySize; }
            set { SetField(ref bodySize, value); }
        }

        public string Process
        {
            get { return process; }
            set { SetField(ref process, value); }
        }

        public long ReceivedDataCount
        {
            get { return receivedDataCount; }
            set { SetField(ref receivedDataCount, value); }
        }

        public long SentDataCount
        {
            get { return sentDataCount; }
            set { SetField(ref sentDataCount, value); }
        }

        public Exception Exception
        {
            get { return exception; }
            set { SetField(ref exception, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Update()
        {
            var request = WebSession.Request;
            var response = WebSession.Response;
            int statusCode = response?.StatusCode ?? 0;
            StatusCode = statusCode == 0 ? "-" : statusCode.ToString();
            Protocol = string.Format( "{0}:{1}", request.RequestUri.Scheme.ToUpper(), request.Method.ToUpper() );

            if (IsTunnelConnect)
            {
                Host = "Tunnel to";
                Url = request.RequestUri.Host + ":" + request.RequestUri.Port;
            }
            else
            {
                Host = request.RequestUri.Host;
                Url = request.RequestUri.AbsolutePath;
            }

            if (!IsTunnelConnect)
            {
                long responseSize = -1;
                if (response != null)
                {
                    if (response.ContentLength != -1)
                    {
                        responseSize = response.ContentLength;
                    }
                    else if (response.IsBodyRead && response.Body != null)
                    {
                        responseSize = response.Body.Length;
                    }
                }

                BodySize = responseSize;
            }

            Process = GetProcessDescription(WebSession.ProcessId.Value);
        }

        private string GetProcessDescription(int processId)
        {
            //return string.Empty;
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(processId);
                return process.ProcessName + ":" + processId;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
