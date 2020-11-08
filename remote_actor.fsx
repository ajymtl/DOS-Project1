#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Remote

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 8777
                    hostname = ""192.168.0.167""
                }
            }
        }")

let system = ActorSystem.Create("RemoteFSharp", configuration)


type WorkerMessageObj = {
    Start: int64;
    Finish: int64;
    Interval: int64; 
}

type BossMessageObj = {
    Finish: int64;
    Interval: int64;
}

type OutputObj = {
    Output: int64;
}

type CompletionMessageObj = {
    Completed: bool
}

let echoServer(mailbox: Actor<_>) = 
    let rec loop() =
        actor {
            let! message = mailbox.Receive()
            let sender = mailbox.Sender()
            match box message with
            | :? WorkerMessageObj as msg -> 
                printfn "Recieved message %A" msg
                let start = msg.Start
                let finish = msg.Finish
                let interval = msg.Interval
                let mutable sum:int64 = 0L
                let mutable lastsquared: int64 = 0L
                let mutable firstsquared: int64 = 0L
                for i in 0L .. finish-start do
                    if i=0L then
                        firstsquared <- int64(start)
                        for j in 0L .. interval-1L do
                            sum <- sum + (start + j)*(start + j)
                        lastsquared <- int64(start + interval - 1L)
                    else
                        lastsquared <- lastsquared + 1L
                        sum <- sum - (firstsquared * firstsquared) + (lastsquared * lastsquared)
                        firstsquared <- firstsquared + 1L

                    let sqrtsum: double = sqrt (double(sum))
                    if sqrtsum = floor(sqrtsum) then
                        let doublecheck: int64 = int64(floor(sqrtsum))
                        if doublecheck*doublecheck = sum then 
                            sender <! { Output = start + i }
                sender <! { Completed = true }
            | _ ->  printfn "FAILURE" // sender <! "FAILURE"
        }
    loop()
            
spawn system "echoServer" echoServer
System.Console.ReadKey() |> ignore