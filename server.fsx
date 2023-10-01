open System
open System.Net
open System.Net.Sockets
open System.Text

let port = 111

let handleClient (client : TcpClient) =
    let clientStream = client.GetStream()
    let reader = new System.IO.StreamReader(clientStream)
    let writer = new System.IO.StreamWriter(clientStream)
    let rec processMessages () =
        let message = reader.ReadLine()
        if message = null then
            Console.WriteLine("Client disconnected.")
        else
            Console.WriteLine("Received: {0}", message)

            // Split the message into parts
            let parts = message.Split [|' '|]
            
            // Check if the message is in the correct format
            if parts.Length >= 3 && parts.Length <= 5 &&
               (["add"; "subtract"; "multiply"; "divide"] |> List.contains parts.[0]) then
                
                // Initialize the result
                let mutable result = 0
                
                try
                    // Process the operation
                    match parts.[0] with
                    | "add" ->
                        for i in 1 .. min 4 (parts.Length - 1) do
                            // printfn "res= %d i is %d" result i
                            result <- result + Int32.Parse(parts.[i])
                            // printfn "res= %d i is %d" result i
                    | "subtract" ->
                        result <- Int32.Parse(parts.[1])
                        for i in 2 .. min 4 (parts.Length - 1) do
                            result <- result - Int32.Parse(parts.[i])
                    | "multiply" ->
                        result <- 1
                        for i in 1 .. min 4 (parts.Length - 1) do
                            result <- result * Int32.Parse(parts.[i])
                    | _ ->
                        // Invalid operation
                        result <- Int32.MinValue
                with
                | :? System.FormatException ->
                    // Invalid number format
                    result <- Int32.MinValue
                
                // printfn $"{result}"

                let mutable temp = result  

                // Check if all inputs are valid integers
                let mutable allValid = true
                for i in 1 .. parts.Length - 1 do
                    if not (Int32.TryParse(parts.[i], &temp)) then
                        allValid <- false

                if not allValid then
                    writer.WriteLine("-1: one or more of the inputs contain(s) non-number(s).")
                else
                    writer.WriteLine(result)
                writer.Flush()
            else
                // Send an error message to the client for invalid input
                let errorMessage =
                    match parts.[0] with
                    | _ when not (["add"; "subtract"; "multiply"] |> List.contains parts.[0]) ->
                        "-1: incorrect operation command."
                    | _ when parts.Length < 3 ->
                        "-2: number of inputs is less than two."
                    | _ when parts.Length > 5 ->
                        "-3: number of inputs is more than four."
                    | _ ->
                        "-4: one or more of the inputs contain(s) non-number(s)."
                writer.WriteLine(errorMessage)
                writer.Flush()

            // Continue processing messages
            processMessages ()

    try
        try
            processMessages ()
        with
            | :? System.IO.IOException ->
                Console.WriteLine("Client disconnected.")
    finally
        client.Close()

let startServer() =
    let listener = new TcpListener(IPAddress.Loopback, port)
    listener.Start()
    Console.WriteLine("Server is running and listening on port {0}.", port)

    try
        while true do
            let client = listener.AcceptTcpClient()
            async { return handleClient client } |> Async.Start
    finally
        listener.Stop()

startServer()
