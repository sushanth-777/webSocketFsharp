//client code

open System
open System.Net
open System.Net.Sockets
open System.Text

let port = 111

let sendMessage message =
    try
        let client = new TcpClient()
        client.Connect(IPAddress.Loopback, port)

        let clientStream = client.GetStream()
        let writer = new System.IO.StreamWriter(clientStream)
        let reader = new System.IO.StreamReader(clientStream)

        // Read user input from the terminal
        Console.Write("Enter a message to send to the server: ")
        let userInput = Console.ReadLine()

        // Send the user's input to the server
        writer.WriteLine(userInput)
        writer.Flush()

        // Receive and display the server's response
        let response = reader.ReadLine()
        Console.WriteLine("Server Response: {0}", response)

        // Close the client connection
        client.Close()
    with
        | :? System.IO.IOException ->
            Console.WriteLine("Client disconnected.")
        | ex ->
            Console.WriteLine("Error: {0}", ex.Message)

// Usage
while true do
    sendMessage ""
