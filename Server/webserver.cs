// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace TabloHRServer
{
    class webServer
    {
        public List<string> Clients = new List<string>(22);
        private readonly string[] _indexFiles = {
        "index.html",
        "index.htm",
        "default.html",
        "default.htm"
    };

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
    };
        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;

        public int Port
        {
            get { return _port; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public webServer(string path, int port)
        {
            this.Initialize(path, port);
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public webServer(string path)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(path, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            Console.WriteLine(filename);
            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            filename = Path.Combine(_rootDirectory, filename);

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

            }
            else
            {
                if (context.Request.RawUrl.Contains("/auto/stop")) {
                    System.Diagnostics.Process p = new Process();
                    p.StartInfo.FileName = "stop.exe";
                }
                else if (context.Request.RawUrl.Contains("/auto/v2.1") && context.Request.HttpMethod == "GET")
                {
                    string?[] qs;

                    if (context.Request.RawUrl.Contains('?')) {
                        qs = context.Request.RawUrl.Split('?');
                        qs = qs[1].Split('&');
                        if (qs[1] == "rec=y" && qs[2] == "dur=30")
                        {
                            bool beingrecorded = true;
                            int dur = 30 * 60;
                            while (dur > 0)
                            {
                                dur--;

                            }
                        }

                    }
                    if (File.Exists("/tune/2-1.exe"))
                    {
                        System.Diagnostics.Process p = new Process();
                        p.StartInfo.FileName = "tune/2-1.exe";
                        p.Start();
                        p.StartInfo.FileName = "deps/vlc/vlc.exe";
                        p.StartInfo.Arguments = "";


                    }
                    if ()
                        if (File.Exists("stream/2.1"))
                        {
                            context.Response.Redirect("/stream/2.1");
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            Program.tunedCh = "2-1";
                            Program.tunerbusy = true;
                            Clients.Add(context.Request.UserHostAddress);
                            HttpClient cli = new HttpClient();
                            CancellationTokenSource src = new CancellationTokenSource();



                        }


                } else if (context.Request.RawUrl.Contains("/auto/v2.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v2.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v2.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v2.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v3.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v3.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v3.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v3.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v3.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v4.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v4.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v4.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v4.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v4.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v5.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v5.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v5.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v5.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v5.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v6.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v6.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v6.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v6.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v6.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v7.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v7.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v7.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v7.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v7.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v8.8"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v9.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v9.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v9.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v9.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v9.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v10.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v10.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v10.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v10.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v10.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v11.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v11.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v11.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v11.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v11.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v12.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v12.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v12.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v12.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v12.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v13.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v13.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v13.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v13.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v13.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v14.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v14.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v14.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v14.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v14.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v15.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v15.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v15.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v15.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v15.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v16.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v16.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v16.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v16.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v16.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v17.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v17.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v17.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v17.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v17.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v18.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v18.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v18.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v18.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v18.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v19.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v19.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v19.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v19.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v19.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v20.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v20.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v20.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v20.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v20.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v21.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v21.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v21.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v21.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v21.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v22.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v22.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v22.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v22.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v22.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v23.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v23.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v23.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v23.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v23.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v24.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v24.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v24.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v24.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v24.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v25.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v25.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v25.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v25.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v25.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v26.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v26.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v26.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v26.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v26.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v27.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v27.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v27.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v27.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v27.6"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v28.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v28.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v28.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v28.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v28.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v29.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v29.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v29.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v29.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v29.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v30.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v30.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v30.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v30.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v30.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v31.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v31.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v31.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v31.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v31.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v32.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v32.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v32.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v32.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v32.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v33.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v33.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v33.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v33.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v33.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v34.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v34.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v35.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v35.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v35.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v35.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v35.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v36.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v36.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v36.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v36.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v36.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v37.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v37.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v37.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v37.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v37.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v38.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v38.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v38.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v38.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v38.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v39.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v39.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v39.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v39.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v39.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v40.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v40.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v40.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v40.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v40.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v41.1))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v41.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v41.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v41.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v41.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v42.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v42.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v42.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v42.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v42.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v43.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v43.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v43.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v43.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v43.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v44.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v44.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v44.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v44.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v44.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v45.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v45.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v45.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v45.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v45.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v46.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v46.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v46.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v46.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v46.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v47.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v47.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v47.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v47.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v47.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v48.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v48.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v48.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v48.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v48.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v49.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v49.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v49.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v49.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v49.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v50.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v50.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v50.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v50.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v50.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v51.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v51.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v51.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v51.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v51.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v52.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v52.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v52.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v52.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v52.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v53.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v53.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v53.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v53.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v53.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v54.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v54.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v54.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v54.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v54.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v55.6"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v56.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v56.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v56.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v56.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v56.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v57.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v57.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v57.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v57.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v57.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v58.6"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v59.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v59.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v59.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v59.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v59.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v60.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v60.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v60.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v60.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v60.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v61.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v61.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v61.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v61.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v61.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v62.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v62.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v62.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v62.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v62.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v63.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v63.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v63.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v63.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v63.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v64.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v64.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v64.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v64.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v64.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v65.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v65.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v65.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v65.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v65.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v66.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v66.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v66.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v66.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v66.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v67.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v67.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v67.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v67.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v67.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.1"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.2"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.3"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.4"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.5"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.6"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.7"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.8"))
                {

                } else if (context.Request.RawUrl.Contains("/auto/v68.9"))
                {

                }


                else {

                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
        }

            context.Response.OutputStream.Close();
        }
        private void tune(string ch) {
            switch (ch)
            {
                default:
                    {
                        //Launch Bluestaks
                        System.Diagnostics.Process p = new Process();
                        p.StartInfo.FileName = "C:\\Program Files\\Blustacks_nxt\\HD-Player.exe";
                        

                        break;
                }
            }
        }
        
        private void Initialize(string path, int port)
        {
            this._rootDirectory = path;
            this._port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }


    }
}