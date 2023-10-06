// SimpleServer based on code by Can Güney Aksakalli
// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html
// modifications by Jaime Spacco

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Web;
using System.Text.Json;


/// <summary>
/// Interface for simple servlets.
/// 
/// </summary>
interface IServlet {
    void ProcessRequest(HttpListenerContext context);
}
/// <summary>
/// BookHandler: Servlet that reads a JSON file and returns a random book
/// as an HTML table with one row.
/// TODO: search for specific books by author or title or whatever
/// </summary>
class BookHandler : IServlet {



    private List<Book> books;
    public BookHandler()
    {

        var options = new JsonSerializerOptions{
            PropertyNameCaseInsensitive = true
        };
    }
    public void ProcessRequest(HttpListenerContext context) {

             if(!context.Request.QueryString.AllKeys.Contains("cmd"))
            {
               context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
               return;
            }

                string cmd = context.Request.QueryString["cmd"];
                if(cmd.Equals("list")){
                        int start = Int32.Parse(context.Request.QueryString["s"]);
                        int end = Int32.Parse(context.Request.QueryString["e"]);
                        List<Book>sublist = books.GetRange(start, end - start +1);


                                string response = $@"
        <table border=1>
        <tr>
            <th>Title</th>
            <th>Author</th>
            <th>Short Description</th>
            <th>Thumbnail</th>
        </tr>";

        foreach (Book book in sublist ){

            string authors = String.Join(",<br>", book.Authors);
        

            response += @"
        <tr>
            <td>{book.Title}</td>
            <td>{authors}</td>
            <td>{book.ShortDescription}</td>
            <td><img src='{book.ThumbnailUrl}'/></td>
        </tr> ";
                }
                response += "</table>";

        // write HTTP response to the output stream
        // all of the context.response stuff is setting the headers for the HTTP response
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.OutputStream.Flush();

                }
                else if (cmd.Equals("random")){
                    //return a ran book
                    Random rand = new Random ();
                    int index= rand.Next(books.Count);
                    Book book = books[index];
                    string authors = String.Join(", <br>", book.Authors);
                    string response = $@"
                    <table border =1>
                    <tr>
                        <th>Title</th>
                        <th>Author</th>
                        <th>Short Description</th>
                        <th>Long Description</th>

                    </tr>
                    <tr>
                        <td>{book.Title}</td>
                        <td>{authors}</td>
                        <td>{book.ShortDescription}</td>
                        <td>{book.LongDescription}</td>

                </tr>
                </table>
                ";

        // write HTTP response to the output stream
        // all of the context.response stuff is setting the headers for the HTTP response
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.OutputStream.Flush();

                }
                else {
                    // unknown command
                }

                }
    }

/// <summary>
/// FooHandler: Servlet that returns a simple HTML page.
/// </summary>
class FooHandler : IServlet {

    public void ProcessRequest(HttpListenerContext context) {
        string response = $@"
            <H1>This is a Servlet Test.</H1>
            <h2>Servlets are a Java thing; there is probably a .NET equivlanet but I don't know it</h2>
            <h3>I am but a humble Java programmer who wrote some Servlets in the 2000s</h3>
            <p>Request path: {context.Request.Url.AbsolutePath}</p>
";
        foreach ( String s in context.Request.QueryString.AllKeys )
            response += $"<p>{s} -> {context.Request.QueryString[s]}</p>\n";

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);

        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        context.Response.OutputStream.Write(bytes, 0, bytes.Length);

        context.Response.OutputStream.Flush();
    }
}


class SimpleHTTPServer
{
    // bind servlets to a path
    // for example, this means that /foo will be handled by an instance of FooHandler
    // TODO: put these mappings into a configuration file
    private static IDictionary<string, IServlet> _servlets = new Dictionary<string, IServlet>() {
        {"foo", new FooHandler()},
        {"books", new BookHandler()},
    };

    // list of default index files
    // if the client requests a directory (e.g. http://localhost:8080/), 
    // we will look for one of these files
    private string[] _indexFiles;
    
    // map extensions to MIME types
    // TODO: put this into a configuration file
    private IDictionary<string, string> _mimeTypeMappings ;

    // instance variables
    private Thread _serverThread;
    private string _rootDirectory;
    private HttpListener _listener;
    private int _port;
    private bool _done = false;
    private int _numRequests =0;
    private Dictionary<string, int> pathsRequested= new Dictionary<string, int>();
    private Dictionary<string, int> noPageExistsCount = new Dictionary<string, int>();



    public int Port
    {
        get { return _port; }
        private set { _port = value;}
    }

    public int NumRequests
    {

        get {return _numRequests; }
        private set { _numRequests = value; }

    }
    public Dictionary<string, int> PathsRequested
    {
        get { return pathsRequested;}

    }
    public Dictionary<string, int> NoPageExistsCount
    {
        get { return noPageExistsCount;}

    }


    /// <summary>
    /// Construct server with given port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    /// <param name="port">Port of the server.</param>
    public SimpleHTTPServer(string path, int port, string configFilename)
    {
        this.Initialize(path, port, configFilename);

    }

    /// <summary>
    /// Construct server with any open port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    public SimpleHTTPServer(string path, string configFilename)
    {
        //get an empty port
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        this.Initialize(path, port, configFilename);
    }

    /// <summary>
    /// Stop server and dispose all functions.
    /// </summary>
    public void Stop()
    {
        _done = true;
        _listener.Close();
    }

    private void Listen()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
        _listener.Start();
        while (!_done)
        {
            Console.WriteLine("Waiting for connection ( sabar karo ah raha hai bhai - In Urdu)...");
            try
            {
                HttpListenerContext context = _listener.GetContext();
                NumRequests += 1;
                Process(context);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        Console.WriteLine("Server stopped!");
    }

    /// <summary>
    /// Process an incoming HTTP request with the given context.
    /// </summary>
    /// <param name="context"></param>
    private void Process(HttpListenerContext context)
    {
        string filename = context.Request.Url.AbsolutePath;
        // keep track of how many times each path was requested 
        //include the leading slash in the path
        pathsRequested[filename]= pathsRequested.GetValueOrDefault(filename, 0)+1;
        //remove leading slash
        filename = filename.Substring(1);
    
        Console.WriteLine($"{filename} is the path");

        // check if the path is mapped to a servlet
        if (_servlets.ContainsKey(filename))
        {
            _servlets[filename].ProcessRequest(context);
            return;
        }

        // if the path is empty (i.e. http://blah:8080/ which yields hte path /)
        // look for a default index filename
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

        // search for the file in the root directory
        // this means we are serving the file, if we can find it
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
            // This sends a 404 if the file doesn't exist or cannot be read
            // TODO: customize the 404 page

            //I prompted chatgpt to give me some commonly used MIME types and their corresponding values for the context.Response.ContentType:
            // It gave me these all but I will be using text/plain because 
            //my 404 error message like "oh my dear error 404, where are you ?" is just a plain text

              //  "text/html": For HTML content.
               // "text/plain": For plain text content.
               // "application/json": For JSON data.
              //  "application/xml": For XML data.
               // "image/jpeg": For JPEG images.
               // "image/png": For PNG images.
               // "application/pdf": For PDF documents.
              //  "application/javascript": For JavaScript files.
              //  "text/css": For CSS stylesheets.
              //  "audio/mpeg": For MP3 audio files.
              //  "video/mp4": For MP4 video files.


string noPageExists="oh my dear error 404, where are you ?";

        //don't know what to add in byte so i just added the byte[] i saw in the from line 325
        // because that is how you did this part "context.Response.ContentLength64 = buffer.Length" , lets see if ths works or not
        byte[] buffer = new byte[1024 * 16];
        Stream input = new FileStream(filename, FileMode.Open);

          int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();


        context.Response.ContentType= "text/plain";  //I am jist setting the content type to plain text
        context.Response.ContentLength64 = buffer.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;


// I will do same thing for finding number of times 404 request was generated 
//like you did for PATHS count "pathsRequested[filename]= pathsRequested.GetValueOrDefault(filename, 0)+1;"

    noPageExistsCount[filename]= noPageExistsCount.GetValueOrDefault(filename, 0)+1;
    filename = filename.Substring(1);  
    //just did the same thing you did in the video for tracking the number of paths requested 
        }
        
        context.Response.OutputStream.Close();
    //The program/dotnet run works but I think this might be wrong but I still tried this one  

    }

    /// <summary>
    /// Initializes the server by setting up a listener thread on the given port
    /// </summary>
    /// <param name="path">the path of the root directory to serve files</param>
    /// <param name="port">the port to listen for connections</param>
    /// <param name = "configFilename"> the name of the JSON cofiguration file</param>
    private void Initialize(string path, int port, string configFilename)
    {
        this._rootDirectory = path;
        this._port = port;
//read config file
         var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };


 string text = File.ReadAllText(configFilename);
    var config  = JsonSerializer.Deserialize<Config>(text, options);
    //ASSIGN FROM THE CONFIG FILE
    _mimeTypeMappings= config.MimeTypes;
    _indexFiles=config.IndexFiles.ToArray();

        _serverThread = new Thread(this.Listen);
        _serverThread.Start();
    }


}

