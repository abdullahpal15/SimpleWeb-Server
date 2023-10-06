
using System.Text.Json;


static void TestJSON(){

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

 string text = File.ReadAllText("config.json");
    var config  = JsonSerializer.Deserialize<Config>(text, options);
    Console.WriteLine($"MimeMappings: {config.MimeTypes[".html"]}");
    Console.WriteLine($"IndexFiles:{config.IndexFiles[0]}");

}

static void TestJSON2() {
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    string text = File.ReadAllText(@"json/books.json");
    var books = JsonSerializer.Deserialize<List<Book>>(text, options);

    Book book = books[4];
    Console.WriteLine($"title: {book.Title}");
    Console.WriteLine($"authors: {book.Authors[0]}");
}

static void TestServer() {
    SimpleHTTPServer server = new SimpleHTTPServer("files", 8080,"config.json");
    string helpMessage = @" You can pleaseee useeee the following commands!!! :
help - display this help message
stop - stop the server
numreqs- display the number of requests
paths - dsiplay the number of times each path was requested 
nopagefound -display the number of 404 errors 
";
string unknowncommandmessage = @" Please either put in stop or help, I beg you dear user because if you need assistance, pal is here ";

    Console.WriteLine($"Server started!\n{helpMessage}");
    while (true)
    {
        Console.Write(">");
        // read line from console
        String command = Console.ReadLine();
        if (command.Equals("stop"))
        {
            server.Stop();
            break;
        }
        else if (command.Equals("help"))
        {
                Console.WriteLine(helpMessage);
        } 
        else if (command.Equals("numreqs"))
        {
            Console.WriteLine($"Number of requests: {server.NumRequests}");
             
        }
        else if (command.Equals("paths"))
        {
            foreach(var path in server.PathsRequested){
            Console.WriteLine($"{path.Key}: {path.Value}");
        }
        }
        else if (command.Equals("nopagefound"))
        {

            foreach (var errorcount in server.NoPageExistsCount){
            Console.WriteLine($"{errorcount.Key}: {errorcount.Value}");
        }
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine(unknowncommandmessage);
        }

        }
    }


//TestJSON();
TestServer();
