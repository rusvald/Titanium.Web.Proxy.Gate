using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Examples.Wpf
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ClientConnectionCountProperty = DependencyProperty.Register(
            nameof(ClientConnectionCount), typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ServerConnectionCountProperty = DependencyProperty.Register(
            nameof(ServerConnectionCount), typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty SaveTrafficDataToFileProperty = DependencyProperty.Register(
            nameof(SaveTrafficDataToFile), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty SaveTrafficDataPathProperty = DependencyProperty.Register(
            nameof(SaveTrafficDataPath), typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SaveByFilterProperty = DependencyProperty.Register(
            nameof(SaveByFilter), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty FilterTrafficBySettingsProperty = DependencyProperty.Register(
            nameof(FilterTrafficBySettings), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        private readonly ProxyServer proxyServer;

        private readonly Dictionary<HttpWebClient, SessionListItem> sessionDictionary =
            new Dictionary<HttpWebClient, SessionListItem>();

        private int lastSessionNumber;
        private SessionListItem selectedSession;

        private List<string> reservedFiles = new List<string>();
        bool _filterMatchInclusive = true;
        bool _nodecryptSSLMatchInclusive = true;
        bool _saveMatchInclusive = true;
        Models.FilterMatchFinder _filterMatchFinder;
        Models.FilterMatchFinder _nodecryptSSLMatchFinder;
        Models.FilterMatchFinder _saveFilterMatchFinder;
        Models.FilterMatchFinder _filterMatchFinderExclusive;
        Models.FilterMatchFinder _nodecryptSSLMatchFinderExclusive;
        Models.FilterMatchFinder _saveFilterMatchFinderExclusive;

        public MainWindow()
        {
#if DEBUG
            SaveTrafficDataPath = Path.Combine(Environment.CurrentDirectory, "Temp");
#else
            if (string.IsNullOrEmpty(Properties.Settings.Default.SaveDataPath))
            {
                SaveTrafficDataPath = Path.Combine("h:\\Temp", "TitaniumWebProxy");
            }
            else
            {
                SaveTrafficDataPath = Properties.Settings.Default.SaveDataPath;
            }
#endif
            _filterMatchInclusive = Properties.Settings.Default.filterMatchInclusive;
            _nodecryptSSLMatchInclusive = Properties.Settings.Default.nodecryptSSLMatchInclusive;
            _saveMatchInclusive = Properties.Settings.Default.saveMatchInclusive;

            FilterSettingsFile = "filter.config";
            NodecryptSSLSettingsFile = "NodecryptSSL.config";
            SaveFilterSettingsFile = "save_filter.config";

            FilterExclusiveSettingsFile = "filterExclusive.config";
            NodecryptSSLExclusiveSettingsFile = "NodecryptSSLExclusive.config";
            SaveFilterExclusiveSettingsFile = "save_filterExclusive.config";

            _filterMatchFinder = new Models.FilterMatchFinder(FilterSettingsFile);
            _nodecryptSSLMatchFinder = new Models.FilterMatchFinder(NodecryptSSLSettingsFile);
            _saveFilterMatchFinder = new Models.FilterMatchFinder(SaveFilterSettingsFile);

            _filterMatchFinderExclusive = new Models.FilterMatchFinder(FilterExclusiveSettingsFile);
            _nodecryptSSLMatchFinderExclusive = new Models.FilterMatchFinder(NodecryptSSLExclusiveSettingsFile);
            _saveFilterMatchFinderExclusive = new Models.FilterMatchFinder(SaveFilterExclusiveSettingsFile);


            #region TEST

            string testUrl = "http://www.youtube.com:443";
            var res = _nodecryptSSLMatchFinder.HasMatches(testUrl);

            testUrl = "http://www.youtube.com:443/";
            res = _nodecryptSSLMatchFinder.HasMatches(testUrl);

            testUrl = "https://www.youtube.com:443/";
            res = _nodecryptSSLMatchFinder.HasMatches(testUrl);

            testUrl = "www.youtube.com:443";
            res = _nodecryptSSLMatchFinder.HasMatches(testUrl);

            testUrl = "https://www.youtube.com/youtubei/v1/log_event?alt=json&key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
            res = _filterMatchFinder.HasMatches(testUrl);

            #endregion TEST

            _nodecryptSSLMatchFinder = new Models.FilterMatchFinder(NodecryptSSLSettingsFile);
            _filterMatchFinder = new Models.FilterMatchFinder(FilterSettingsFile);



            proxyServer = new ProxyServer();
            //proxyServer.CertificateManager.CertificateEngine = CertificateEngine.DefaultWindows;

            ////Set a password for the .pfx file
            //proxyServer.CertificateManager.PfxPassword = "PfxPassword";

            ////Set Name(path) of the Root certificate file
            //proxyServer.CertificateManager.PfxFilePath = @"C:\NameFolder\rootCert.pfx";

            ////do you want Replace an existing Root certificate file(.pfx) if password is incorrect(RootCertificate=null)?  yes====>true
            //proxyServer.CertificateManager.OverwritePfxFile = true;

            ////save all fake certificates in folder "crts"(will be created in proxy dll directory)
            ////if create new Root certificate file(.pfx) ====> delete folder "crts"
            //proxyServer.CertificateManager.SaveFakeCertificates = true;

            proxyServer.ForwardToUpstreamGateway = true;

            ////if you need Load or Create Certificate now. ////// "true" if you need Enable===> Trust the RootCertificate used by this proxy server
            //proxyServer.CertificateManager.EnsureRootCertificate(true);

            ////or load directly certificate(As Administrator if need this)
            ////and At the same time chose path and password
            ////if password is incorrect and (overwriteRootCert=true)(RootCertificate=null) ====> replace an existing .pfx file
            ////note : load now (if existed)
            //proxyServer.CertificateManager.LoadRootCertificate(@"C:\NameFolder\rootCert.pfx", "PfxPassword");

            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true);

            proxyServer.AddEndPoint(explicitEndPoint);
            //proxyServer.UpStreamHttpProxy = new ExternalProxy
            //{
            //    HostName = "158.69.115.45",
            //    Port = 3128,
            //    UserName = "Titanium",
            //    Password = "Titanium",
            //};

            proxyServer.BeforeRequest += ProxyServer_BeforeRequest;
            proxyServer.BeforeResponse += ProxyServer_BeforeResponse;
            proxyServer.AfterResponse += ProxyServer_AfterResponse;
            explicitEndPoint.BeforeTunnelConnectRequest += ProxyServer_BeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse += ProxyServer_BeforeTunnelConnectResponse;
            proxyServer.ClientConnectionCountChanged += delegate
            {
                Dispatcher.Invoke(() => { ClientConnectionCount = proxyServer.ClientConnectionCount; });
            };
            proxyServer.ServerConnectionCountChanged += delegate
            {
                Dispatcher.Invoke(() => { ServerConnectionCount = proxyServer.ServerConnectionCount; });
            };
            proxyServer.Start();

            proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);



            InitializeComponent();
        }

        public ObservableCollection<SessionListItem> Sessions { get; } = new ObservableCollection<SessionListItem>();
        public bool SaveTrafficDataToFile
        {
            get { return (bool)GetValue(SaveTrafficDataToFileProperty); }
            set { SetValue(SaveTrafficDataToFileProperty, value); }
        }
        public string SaveTrafficDataPath
        {
            get { return (string)GetValue(SaveTrafficDataPathProperty); }
            set { SetValue(SaveTrafficDataPathProperty, value); }
        }

        public bool SaveByFilter
        {
            get { return (bool)GetValue(SaveByFilterProperty); }
            set { SetValue(SaveByFilterProperty, value); }
        }
        public bool FilterTrafficBySettings
        {
            get { return (bool)GetValue(FilterTrafficBySettingsProperty); }
            set { SetValue(FilterTrafficBySettingsProperty, value); }
        }
        public string FilterSettingsFile { get; set; }
        public string NodecryptSSLSettingsFile { get; set; }
        public string SaveFilterSettingsFile { get; set; }

        public string FilterExclusiveSettingsFile { get; set; }
        public string NodecryptSSLExclusiveSettingsFile { get; set; }
        public string SaveFilterExclusiveSettingsFile { get; set; }

        public SessionListItem SelectedSession
        {
            get { return selectedSession; }
            set
            {
                if (value != selectedSession)
                {
                    selectedSession = value;
                    SelectedSessionChanged();
                }
            }
        }

        public int ClientConnectionCount
        {
            get { return (int)GetValue(ClientConnectionCountProperty); }
            set { SetValue(ClientConnectionCountProperty, value); }
        }

        public int ServerConnectionCount
        {
            get { return (int)GetValue(ServerConnectionCountProperty); }
            set { SetValue(ServerConnectionCountProperty, value); }
        }

        private async Task ProxyServer_BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            bool isSave = false;
            bool isSaveByFilter = false;
            string savePath = string.Empty;
            string hostname = e.WebSession.Request.RequestUri.Host;
            bool terminateSession = false;
            bool filterTraffic = false;

            var matchres = _nodecryptSSLMatchInclusive ?
                _nodecryptSSLMatchFinder.HasMatches(e.WebSession.Request.RequestUri.AbsoluteUri) :
                _nodecryptSSLMatchFinderExclusive.HasMatches(e.WebSession.Request.RequestUri.AbsoluteUri);
            if (_nodecryptSSLMatchInclusive)
            {
                //No decrypt if matched one of the wildcard in settings
                if (matchres.IsMatch)
                {
                    e.DecryptSsl = false;
                }
            }
            else
            {
                //No decrypt if no matched one of the wildcard in settings
                if (!matchres.IsMatch)
                {
                    e.DecryptSsl = false;
                }
            }
            

            await Dispatcher.InvokeAsync(() => {
                AddSession(e);

                isSave = SaveTrafficDataToFile;
                isSaveByFilter = SaveByFilter;
                savePath = SaveTrafficDataPath;
                filterTraffic = FilterTrafficBySettings;
            });

            Models.MatchResult mresult = _filterMatchInclusive ? _filterMatchFinder.HasMatches(e.WebSession.Request.Url) : _filterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            if (_filterMatchInclusive)
            {
                if (mresult.IsMatch && filterTraffic)
                {
                    e.TerminateSession();
                    e.DenyConnect = true;
                    terminateSession = true;
                }
            }
            else
            {
                if (!mresult.IsMatch && filterTraffic)
                {
                    e.TerminateSession();
                    e.DenyConnect = true;
                    terminateSession = true;
                }
            }
            //Is the same?
            //if ((mresult.IsMatch && _filterMatchInclusive || !mresult.IsMatch && !_filterMatchInclusive) && filterTraffic)
            //{
            //    e.TerminateSession();
            //    terminateSession = true;
            //}


            var smresult = _saveMatchInclusive ?
                _saveFilterMatchFinder.HasMatches(e.WebSession.Request.Url) :
                _saveFilterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            
            if (isSave)
            {
                if (isSaveByFilter && _saveMatchInclusive && !smresult.IsMatch)
                    return;
                if (isSaveByFilter && !_saveMatchInclusive && smresult.IsMatch)
                    return;

                string hostName = e.WebSession.Request.Host.Replace(":", "_Port").Replace(".", "_");
                string fileNameHeader = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_H_TREQ_{2}.log",
                    savePath, DateTime.Now, hostName);
                string fileNameBody = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_B_TREQ_{2}.log",
                    savePath, DateTime.Now, hostName);

                if (terminateSession)
                {
                    if(_filterMatchInclusive)
                        WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText, string.Format("Will be termineted by {0}", mresult.MatchedWildCard));
                    else
                        WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText, string.Format("Will be termineted by exclusive (no match any filter wildcards)"));
                }
                else
                {
                    WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText);
                }

                e.UserData = terminateSession ? (object)mresult : e.WebSession.Request.HeaderText;

                if (e.WebSession.Request.IsBodyRead && e.WebSession.Request.HasBody)
                    WriteToLog(fileNameBody, e.WebSession.Request.Body, e.WebSession.Request.Body.Length);
                else if (!e.WebSession.Request.HasBody)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not exist.");
                else if (!e.WebSession.Request.IsBodyRead)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not read yet.");
            }
        }

        private async Task ProxyServer_BeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
        {
            bool isSave = false;
            bool isSaveByFilter = false;
            string savePath = string.Empty;

            await Dispatcher.InvokeAsync(() =>
            {
                SessionListItem item;
                if (sessionDictionary.TryGetValue(e.WebSession, out item))
                {
                    item.Update();
                }


                isSave = SaveTrafficDataToFile;
                isSaveByFilter = SaveByFilter;
                savePath = SaveTrafficDataPath;
            });

            var smresult = _saveMatchInclusive ?
                _saveFilterMatchFinder.HasMatches(e.WebSession.Request.Url) :
                _saveFilterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            if (isSave)
            {
                if (isSaveByFilter && _saveMatchInclusive && !smresult.IsMatch)
                    return;
                if (isSaveByFilter && !_saveMatchInclusive && smresult.IsMatch)
                    return;

                string hostName = e.WebSession.Request.Host.Replace(":", "_Port").Replace(".", "_");
                string fileNameHeader = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_H_TRES_{2}.log",
                    savePath, DateTime.Now, hostName);
                string fileNameBody = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_B_TRES_{2}.log",
                    savePath, DateTime.Now, hostName);

                if (e.UserData is Models.MatchResult)
                {
                    Models.MatchResult res = e.UserData as Models.MatchResult;
                    if (_filterMatchInclusive)
                    {
                        WriteToLog(
                          fileNameHeader,
                          e.WebSession.Response.HeaderText,
                          string.Format("Session Terminated. By wildcard {0}. Url {1}", res.MatchedWildCard, res.ProcessingString));
                    }
                    else
                    {
                        WriteToLog(
                          fileNameHeader,
                          e.WebSession.Response.HeaderText,
                          string.Format("Session Terminated. By wildcard exclusive. Url {0}", res.ProcessingString));
                    }
                }
                else
                {
                    WriteToLog(fileNameHeader, e.WebSession.Response.HeaderText, e.UserData.ToString());
                }


                if (e.WebSession.Response.IsBodyRead && e.WebSession.Response.HasBody)
                    WriteToLog(fileNameBody, e.WebSession.Response.Body, e.WebSession.Response.Body.Length);
                else if (!e.WebSession.Response.HasBody)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not exist.");
                else if (!e.WebSession.Response.IsBodyRead)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not read yet.");
            }
        }

        private async Task ProxyServer_BeforeRequest(object sender, SessionEventArgs e)
        {
            bool isSave = false;
            bool isSaveByFilter = false;
            string savePath = string.Empty;
            bool terminateSession = false;
            bool filterTraffic = false;

            SessionListItem item = null;
            await Dispatcher.InvokeAsync(() => {
                item = AddSession(e);



                isSave = SaveTrafficDataToFile;
                isSaveByFilter = SaveByFilter;
                savePath = SaveTrafficDataPath;
                filterTraffic = FilterTrafficBySettings;
            });

            if (e.WebSession.Request.HasBody)
            {
                e.WebSession.Request.KeepBody = true;
                await e.GetRequestBody();
            }

            Models.MatchResult mresult = _filterMatchInclusive ?
                _filterMatchFinder.HasMatches(e.WebSession.Request.Url) :
                _filterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            if (_filterMatchInclusive)
            {
                if (mresult.IsMatch && filterTraffic)
                {
                    e.TerminateSession();
                    terminateSession = true;
                }
            }
            else
            {
                if (!mresult.IsMatch && filterTraffic)
                {
                    e.TerminateSession();
                    terminateSession = true;
                }
            }

            var smresult = _saveMatchInclusive ?
                _saveFilterMatchFinder.HasMatches(e.WebSession.Request.Url) :
                _saveFilterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            if (isSave)
            {
                if (isSaveByFilter && _saveMatchInclusive && !smresult.IsMatch)
                    return;
                if (isSaveByFilter && !_saveMatchInclusive && smresult.IsMatch)
                    return;


                string hostName = e.WebSession.Request.Host.Replace(":", "_Port").Replace(".", "_");
                string fileNameHeader = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_H_REQ_{2}.log",
                    savePath, DateTime.Now, hostName);
                string fileNameBody = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_B_REQ_{2}.log",
                    savePath, DateTime.Now, hostName);

                if (terminateSession)
                {
                    if(_filterMatchInclusive)
                        WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText, string.Format( "Willbe termineted by {0}", mresult.MatchedWildCard));
                    else
                        WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText, string.Format("Will be termineted by exclusive (no match any filter wildcards)"));
                }
                else
                {
                    WriteToLog(fileNameHeader, e.WebSession.Request.HeaderText);
                }
                e.UserData = terminateSession ? (object)mresult : e.WebSession.Request.HeaderText;

                if (e.WebSession.Request.IsBodyRead && e.WebSession.Request.HasBody)
                    WriteToLog(fileNameBody, e.WebSession.Request.Body, e.WebSession.Request.Body.Length);
                else if (!e.WebSession.Request.HasBody)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not exist.");
                else if (!e.WebSession.Request.IsBodyRead)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not read yet.");
            }
        }

        private async Task ProxyServer_BeforeResponse(object sender, SessionEventArgs e)
        {
            SessionListItem item = null;
            await Dispatcher.InvokeAsync(() =>
            {
                if (sessionDictionary.TryGetValue(e.WebSession, out item))
                {
                    item.Update();
                }
            });

            if (item != null)
            {
                if (e.WebSession.Response.HasBody)
                {
                    e.WebSession.Response.KeepBody = true;
                    await e.GetResponseBody();

                    await Dispatcher.InvokeAsync(() => { item.Update(); });
                }
            }
        }

        private async Task ProxyServer_AfterResponse(object sender, SessionEventArgs e)
        {
            bool isSave = false;
            bool isSaveByFilter = false;
            string savePath = string.Empty;
            await Dispatcher.InvokeAsync(() =>
            {
                SessionListItem item;
                if (sessionDictionary.TryGetValue(e.WebSession, out item))
                {
                    item.Exception = e.Exception;
                }
                isSave = SaveTrafficDataToFile;
                isSaveByFilter = SaveByFilter;
                savePath = SaveTrafficDataPath;
            });

            var smresult = _saveMatchInclusive ?
                _saveFilterMatchFinder.HasMatches(e.WebSession.Request.Url) :
                _saveFilterMatchFinderExclusive.HasMatches(e.WebSession.Request.Url);
            if (isSave)
            {
                if (isSaveByFilter && _saveMatchInclusive && !smresult.IsMatch)
                    return;
                if (isSaveByFilter && !_saveMatchInclusive && smresult.IsMatch)
                    return;


                string hostName = e.WebSession.Request.Host.Replace(":", "_Port").Replace(".", "_");
                string fileNameHeader = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_H_RES_{2}.log",
                    savePath, DateTime.Now, hostName);
                string fileNameBody = string.Format("{0}\\{1:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}_B_RES_{2}.log",
                    savePath, DateTime.Now, hostName);

                if(e.UserData is Models.MatchResult)
                {
                    Models.MatchResult res = e.UserData as Models.MatchResult;
                    if (_filterMatchInclusive)
                    {
                        WriteToLog(
                          fileNameHeader,
                          e.WebSession.Response.HeaderText,
                          string.Format("Session Terminated. By wildcard {0}. Url {1}", res.MatchedWildCard, res.ProcessingString));
                    }
                    else
                    {
                        WriteToLog(
                          fileNameHeader,
                          e.WebSession.Response.HeaderText,
                          string.Format("Session Terminated. By wildcard exclusive. Url {0}", res.ProcessingString));
                    }
                }
                else
                {
                    WriteToLog(fileNameHeader, e.WebSession.Response.HeaderText, e.UserData.ToString());
                }
                

                if (e.WebSession.Response.IsBodyRead && e.WebSession.Response.HasBody)
                    WriteToLog(fileNameBody, e.WebSession.Response.Body, e.WebSession.Response.Body.Length);
                else if (!e.WebSession.Response.HasBody)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not exist.");
                else if (!e.WebSession.Response.IsBodyRead)
                    WriteToLog(fileNameBody, "Titanium.Web.Proxy: Body not read yet.");
            }
        }

        private SessionListItem AddSession(SessionEventArgsBase e)
        {
            var item = CreateSessionListItem(e);
            Sessions.Add(item);
            sessionDictionary.Add(e.WebSession, item);
            return item;
        }

        private SessionListItem CreateSessionListItem(SessionEventArgsBase e)
        {
            lastSessionNumber++;
            bool isTunnelConnect = e is TunnelConnectSessionEventArgs;
            var item = new SessionListItem
            {
                Number = lastSessionNumber,
                WebSession = e.WebSession,
                IsTunnelConnect = isTunnelConnect
            };

            if (!isTunnelConnect || e.WebSession.Request.UpgradeToWebSocket)
            {
                e.DataReceived += (sender, args) =>
                {
                    var session = (SessionEventArgs)sender;
                    SessionListItem li;
                    if (sessionDictionary.TryGetValue(session.WebSession, out li))
                    {
                        li.ReceivedDataCount += args.Count;
                    }
                };

                e.DataSent += (sender, args) =>
                {
                    var session = (SessionEventArgs)sender;
                    SessionListItem li;
                    if (sessionDictionary.TryGetValue(session.WebSession, out li))
                    {
                        li.SentDataCount += args.Count;
                    }
                };
            }

            item.Update();
            return item;
        }

        private void ListViewSessions_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var selectedItems = ((ListView)sender).SelectedItems;
                foreach (var item in selectedItems.Cast<SessionListItem>().ToArray())
                {
                    Sessions.Remove(item);
                    sessionDictionary.Remove(item.WebSession);
                }
            }
        }

        private void SelectedSessionChanged()
        {
            if (SelectedSession == null)
            {
                return;
            }

            const int truncateLimit = 1024;

            var session = SelectedSession.WebSession;
            var request = session.Request;
            var data = (request.IsBodyRead ? request.Body : null) ?? new byte[0];
            bool truncated = data.Length > truncateLimit;
            if (truncated)
            {
                data = data.Take(truncateLimit).ToArray();
            }

            //string hexStr = string.Join(" ", data.Select(x => x.ToString("X2")));
            var sb = new StringBuilder();
            sb.Append(request.HeaderText);
            sb.Append(request.Encoding.GetString(data));
            sb.Append(truncated ? Environment.NewLine + $"Data is truncated after {truncateLimit} bytes" : null);
            sb.Append((request as ConnectRequest)?.ClientHelloInfo);
            TextBoxRequest.Text = sb.ToString();

            var response = session.Response;
            data = (response.IsBodyRead ? response.Body : null) ?? new byte[0];
            truncated = data.Length > truncateLimit;
            if (truncated)
            {
                data = data.Take(truncateLimit).ToArray();
            }

            //hexStr = string.Join(" ", data.Select(x => x.ToString("X2")));
            sb = new StringBuilder();
            sb.Append(response.HeaderText);
            sb.Append(response.Encoding.GetString(data));
            sb.Append(truncated ? Environment.NewLine + $"Data is truncated after {truncateLimit} bytes" : null);
            sb.Append((response as ConnectResponse)?.ServerHelloInfo);
            if (SelectedSession.Exception != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(SelectedSession.Exception);
            }

            TextBoxResponse.Text = sb.ToString();
        }

        private void WriteToLog(string Filename, string Comment, string url = "")
        {
            StreamWriter sw = null;
            try
            {
                string dirPath = Path.GetDirectoryName(Filename);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                int i = 1;
                string newFileName = Filename;
                
                lock (reservedFiles)
                {
                    while (File.Exists(newFileName) || reservedFiles.Contains(newFileName))
                    {
                        newFileName = string.Format("{0}\\{1}_{2}{3}", Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename), i++, Path.GetExtension(Filename));
                    }

                    reservedFiles.Add(newFileName);
                }

                sw = new StreamWriter(newFileName, true);
                if (string.IsNullOrEmpty(url))
                {
                    sw.WriteLine(DateTime.Now + "\r\n" + Comment);
                }
                else
                {
                    sw.WriteLine(DateTime.Now + "\r\n" + url + "\r\n" + Comment);
                }
                
                sw.Flush();
                lock (reservedFiles)
                    reservedFiles.Remove(newFileName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        private void WriteToLog(string Filename, byte[] data, int length)
        {
            FileStream sw = null;
            try
            {
                string dirPath = Path.GetDirectoryName(Filename);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                int i = 1;
                string newFileName = Filename;
                lock (reservedFiles)
                {
                    while (File.Exists(newFileName) || reservedFiles.Contains(newFileName))
                    {
                        newFileName = string.Format("{0}\\{1}_{2}{3}", Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename), i++, Path.GetExtension(Filename));
                    }
                    reservedFiles.Add(newFileName);
                }
                    

                sw = new System.IO.FileStream(newFileName, FileMode.Append);
                sw.Write(data, 0, length);
                sw.Flush();

                lock (reservedFiles)
                    reservedFiles.Remove(newFileName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        private void ButtonReloadFilters_Click(object sender, RoutedEventArgs e)
        {
            var oldCur = Cursor;
            Cursor = Cursors.Wait;

            _filterMatchFinder = new Models.FilterMatchFinder(FilterSettingsFile);
            _filterMatchFinderExclusive = new Models.FilterMatchFinder(FilterExclusiveSettingsFile);
            tbFilters.Text = _filterMatchInclusive ? _filterMatchFinder.FiltersInfo : _filterMatchFinderExclusive.FiltersInfo;

            _nodecryptSSLMatchFinder = new Models.FilterMatchFinder(NodecryptSSLSettingsFile);
            _nodecryptSSLMatchFinderExclusive = new Models.FilterMatchFinder(NodecryptSSLExclusiveSettingsFile);
            tbNodecryptSSL.Text = _nodecryptSSLMatchInclusive ? _nodecryptSSLMatchFinder.FiltersInfo : _nodecryptSSLMatchFinderExclusive.FiltersInfo;

            _saveFilterMatchFinder = new Models.FilterMatchFinder(SaveFilterSettingsFile);
            _saveFilterMatchFinderExclusive = new Models.FilterMatchFinder(SaveFilterExclusiveSettingsFile);
            tbSaveFilter.Text = _saveMatchInclusive ? _saveFilterMatchFinder.FiltersInfo : _saveFilterMatchFinderExclusive.FiltersInfo;

            
            

            Cursor = oldCur;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rbtFiltersInclusive.IsChecked = _filterMatchInclusive;
            rbtFiltersExclusive.IsChecked = !_filterMatchInclusive;
            rbtNoDecryptInclusive.IsChecked = _nodecryptSSLMatchInclusive;
            rbtNoDecryptExclusive.IsChecked = !_nodecryptSSLMatchInclusive;
            rbtSaveByFilterInclusive.IsChecked = _saveMatchInclusive;
            rbtSaveByFilterExclusive.IsChecked = !_saveMatchInclusive;

            tbFilters.Text = _filterMatchInclusive ? _filterMatchFinder.FiltersInfo : _filterMatchFinderExclusive.FiltersInfo;
            tbNodecryptSSL.Text = _nodecryptSSLMatchInclusive ? _nodecryptSSLMatchFinder.FiltersInfo : _nodecryptSSLMatchFinderExclusive.FiltersInfo;
            tbSaveFilter.Text = _saveMatchInclusive ? _saveFilterMatchFinder.FiltersInfo : _saveFilterMatchFinderExclusive.FiltersInfo;
            this.Title = string.Format("{0} (ver. {1})", System.Windows.Forms.Application.ProductName, System.Windows.Forms.Application.ProductVersion);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using(System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.SelectedPath = SaveTrafficDataPath;
                if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SaveTrafficDataPath = fbd.SelectedPath;
                    Properties.Settings.Default.SaveDataPath = SaveTrafficDataPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void btnFilterRefresh_Click(object sender, RoutedEventArgs e)
        {
            tbFilters.Text = _filterMatchInclusive ? _filterMatchFinder.FiltersInfo : _filterMatchFinderExclusive.FiltersInfo;
        }

        private void btnNodecryptSSLRefresh_Click(object sender, RoutedEventArgs e)
        {
            tbNodecryptSSL.Text = _nodecryptSSLMatchInclusive ? _nodecryptSSLMatchFinder.FiltersInfo : _nodecryptSSLMatchFinderExclusive.FiltersInfo;
        }

        private void btnSaveFilterRefresh_Click(object sender, RoutedEventArgs e)
        {
            tbSaveFilter.Text = _saveMatchInclusive ? _saveFilterMatchFinder.FiltersInfo : _saveFilterMatchFinderExclusive.FiltersInfo;
        }

        private void rbtFiltersInclusive_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null) return;
            switch (rb.Name)
            {
                case "rbtFiltersInclusive":
                    Properties.Settings.Default.filterMatchInclusive = _filterMatchInclusive = true;
                    Properties.Settings.Default.Save();
                    tbFilters.Text = _filterMatchFinder.FiltersInfo;
                    break;
                case "rbtFiltersExclusive":
                    Properties.Settings.Default.filterMatchInclusive = _filterMatchInclusive = false;
                    Properties.Settings.Default.Save();
                    tbFilters.Text = _filterMatchFinderExclusive.FiltersInfo;
                    break;
                case "rbtNoDecryptInclusive":
                    Properties.Settings.Default.nodecryptSSLMatchInclusive = _nodecryptSSLMatchInclusive = true;
                    Properties.Settings.Default.Save();
                    tbNodecryptSSL.Text = _nodecryptSSLMatchFinder.FiltersInfo;
                    break;
                case "rbtNoDecryptExclusive":
                    Properties.Settings.Default.nodecryptSSLMatchInclusive = _nodecryptSSLMatchInclusive = false;
                    Properties.Settings.Default.Save();
                    tbNodecryptSSL.Text = _nodecryptSSLMatchFinderExclusive.FiltersInfo;
                    break;
                case "rbtSaveByFilterInclusive":
                    Properties.Settings.Default.saveMatchInclusive = _saveMatchInclusive = true;
                    Properties.Settings.Default.Save();
                    tbSaveFilter.Text = _saveFilterMatchFinder.FiltersInfo;
                    break;
                case "rbtSaveByFilterExclusive":
                    Properties.Settings.Default.saveMatchInclusive = _saveMatchInclusive = false;
                    Properties.Settings.Default.Save();
                    tbSaveFilter.Text = _saveFilterMatchFinderExclusive.FiltersInfo;
                    break;
                default:
                    break;
            }
        }
    }
}
