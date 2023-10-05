module server
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open System.Threading

let port = 12345
let mutable clientCount: int = 0
let clientCountLock = new Object()
let clients = new Dictionary<int, TcpClient>()

let getClientId (client: TcpClient) =
    lock clientCountLock (fun () ->
        clientCount <- clientCount + 1
        clients.Add(clientCount, client)
        clientCount
    )

let handleClient (clientId: int) =
    async {
        let client = clients.[clientId]
        let clientStream = client.GetStream()
        use reader = new System.IO.StreamReader(clientStream)
        use writer = new System.IO.StreamWriter(clientStream)

        writer.WriteLine("Hello, Client " + clientId.ToString())
        writer.Flush()

        while true do
         try 
            let message = reader.ReadLine()

            if message = null then
                Console.WriteLine("Client disconnected.")
            else
                Console.WriteLine("Received: {0}", message)

                // Check if the client wants to disconnect
                if message = "bye" then
                    Console.WriteLine($"Responding to client {clientId} with result: -5")
                    writer.WriteLine("bye") // Signal client to exit
                    writer.Flush()
                    clients.Remove(clientId) |> ignore
                elif message = "terminate" then
                    Console.WriteLine($"Responding to client {clientId} with result: -5")
                    writer.WriteLine("terminate") // Signal client to exit
                    writer.Flush()
                    exit 0
                else
                    // Split the message into parts
                    let parts = message.Split [|' '|]

                    // Check if the message is in the correct format
                    if parts.Length >= 3 && parts.Length <= 5 && (["add"; "subtract"; "multiply"] |> List.contains parts.[0]) then

                        // Initialize the result
                        let mutable result = 0

                        try
                            // Process the operation
                            match parts.[0] with
                            | "add" ->
                                for i in 1 .. min 4 (parts.Length - 1) do
                                    result <- result + Int32.Parse(parts.[i])
                                writer.WriteLine(result)
                                Console.WriteLine($"Responding to client {clientId} with result: {result}")
                            | "subtract" ->
                                result <- Int32.Parse(parts.[1])
                                for i in 2 .. min 4 (parts.Length - 1) do
                                    result <- result - Int32.Parse(parts.[i])
                                writer.WriteLine(result)
                                Console.WriteLine($"Responding to client {clientId} with result: {result}")
                            | "multiply" ->
                                result <- 1
                                for i in 1 .. min 4 (parts.Length - 1) do
                                    result <- result * Int32.Parse(parts.[i])
                                writer.WriteLine(result)
                                Console.WriteLine($"Responding to client {clientId} with result: {result}")
                            | _ ->
                                // Invalid operation
                                result <- Int32.MinValue
                                writer.WriteLine(result)
                        with
                        | :? System.FormatException ->
                            writer.WriteLine("one or more of the inputs contain(s) non-number(s).")
                            Console.WriteLine($"Responding to client {clientId} with result: -4")

                        writer.Flush()
                    else
                        // Send an error message to the client for invalid input
                        let errorMessage =
                            match parts.[0] with
                            | _ when not (["add"; "subtract"; "multiply"] |> List.contains parts.[0]) ->
                                writer.WriteLine("incorrect operation command.")
                                Console.WriteLine($"Responding to client {clientId} with result: -1")
                            | _ when parts.Length < 3 ->
                                writer.WriteLine("number of inputs is less than two.")
                                Console.WriteLine($"Responding to client {clientId} with result: -2")
                            | _ when parts.Length > 5 ->
                                writer.WriteLine("number of inputs is more than four.")
                                Console.WriteLine($"Responding to client {clientId} with result: -3")
                            | _ ->
                                writer.WriteLine("one or more of the inputs contain(s) non-number(s).")
                                Console.WriteLine($"Responding to client {clientId} with result: -4")

                        writer.Flush()
         with
            | :? System.IO.IOException -> ignore
            | :? System.Net.Sockets.SocketException -> Console.WriteLine("Woopsie")
    }


let startServer() =
    let listener = new TcpListener(IPAddress.Loopback, port)
    listener.Start()
    Console.WriteLine("Server is running and listening on port {0}.", port)

    while true do
        let client = listener.AcceptTcpClient()
        let clientId = getClientId client
        async {
            let! _ =
                async {
                    handleClient clientId |> Async.RunSynchronously
                }
            return ()
        } |> Async.Start

startServer()
