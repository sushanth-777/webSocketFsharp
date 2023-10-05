module client
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

let serverAddress = "127.0.0.1"
let port = 12345

let connectAndHandleServer () =
    async {
        try
            let client = new TcpClient(serverAddress, port)
            let stream = client.GetStream()
            use reader = new StreamReader(stream)
            use writer = new StreamWriter(stream)

            let response = reader.ReadLine()

            while true do
                Console.Write("Sending command: ")
                let command = Console.ReadLine()
                writer.WriteLine(command)
                writer.Flush()

                let result = reader.ReadLine()
                if result = "bye" then
                    Console.WriteLine("exit")
                    exit 0
                elif result = "terminate" then
                    exit 0
                else
                    Console.WriteLine($"Server response: {result}")
        with
            | :? SocketException ->
                Console.WriteLine("Failed to connect to the server.")
    }


Async.RunSynchronously (connectAndHandleServer())
