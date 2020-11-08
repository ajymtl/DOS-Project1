#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Remote

let args = System.Environment.GetCommandLineArgs()
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
                    port = 8778
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

let GetRemoteWorker n = 
    if n = 1 then
        system.ActorSelection("akka.tcp://RemoteFSharp@192.168.0.193:8777/user/echoServer")
    else
        system.ActorSelection("akka.tcp://RemoteFSharp@192.168.0.85:8777/user/echoServer")

type Worker(name) =
    inherit Actor() // UntypedActor
    override x.OnReceive (message:obj) =
        let sender = x.Sender
        match message with
        | :? WorkerMessageObj as msg ->
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


type Boss(name) =
    inherit Actor() // UntypedActor
    let mutable finished_worker_count = 0L
    let mutable original_sender = null
    let worker_count = 8L
    override x.OnReceive (message:obj) =
        let sender = x.Sender
        match message with
        | :? BossMessageObj as msg ->
            original_sender <- sender
            let finish = msg.Finish
            let interval = msg.Interval
            let mutable remaining = finish
            let mutable filled = 0L
            let mutable workerfin = 0L

            for id in [0L .. worker_count - 1L] do
                if remaining <> 0L then
                    workerfin <- remaining/(worker_count-id)
                    remaining <- remaining - workerfin
                    if workerfin <> 0L then
                        if id = worker_count - 1L && args.Length = 6 && args.[5] = "true" then
                            let rworker = GetRemoteWorker 1
                            rworker.Tell {Start = filled+1L; Finish = filled+workerfin; Interval = interval}
                        elif id = worker_count - 2L && args.Length = 6 && args.[5] = "true" then
                            let rworker = GetRemoteWorker 2
                            rworker.Tell {Start = filled+1L; Finish = filled+workerfin; Interval = interval}
                        else
                            let aworker = system.ActorOf(Props(typedefof<Worker>,  [| string(id) :> obj |]))
                            aworker.Tell {Start = filled+1L; Finish = filled+workerfin; Interval = interval}    
                    filled <- workerfin + filled
        | :? OutputObj as msg ->
            printfn "%A" msg.Output
        | :? CompletionMessageObj as msg ->
            finished_worker_count <- finished_worker_count + 1L
            sender.Tell(PoisonPill.Instance);
            if finished_worker_count = worker_count then
                original_sender <! { Completed = true }
        | _ ->  printfn "FAILURE" // sender <! "FAILURE"


let Boss = system.ActorOf(Props(typedefof<Boss>, [| "Boss" :> obj |]))
let (task:Async<CompletionMessageObj>) = (Boss <? {Finish = args.[3]|>int64; Interval = args.[4]|>int64})
let response = Async.RunSynchronously (task)
Boss.Tell(PoisonPill.Instance);