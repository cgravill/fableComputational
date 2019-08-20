module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Browser.Blob


open Elmish.HMR

type Model = int

type Msg =
| Increment
| Decrement
| MassiveCalculation
| MassiveCalculationAsync

let init() : Model = 0

let update (msg:Msg) (model:Model) =
    match msg with
    | Increment -> model + 1
    | Decrement -> model - 1
    | MassiveCalculation ->
      model
    | MassiveCalculationAsync ->
      model

type IActualModule =
  abstract isAwesome: unit -> bool

[<Import("default", @"E:\dev\fableComputational\src\wasm\fibonacci.js")>]
let FibonacciModule :unit -> JS.Promise<IActualModule> = jsNative

let doMassiveCalculation dispatch =
  JS.console.time("calc")
  for i in 0L..20000000L do
    if i % 20000L = 0L then
        JS.console.log(i)
    ()
  JS.console.timeEnd("calc")
  dispatch MassiveCalculation

let doMassiveCalculationAsync dispatch =
  JS.console.time("calcAsync")
  async {
    JS.console.log("really started")
    for i in 0L..20000000L do
      
      if i % 20000L = 0L then
        JS.console.log(i)
      ()
     
    JS.console.timeEnd("calcAsync")
    dispatch MassiveCalculationAsync
  }
  |> Async.StartImmediate

let [<Global>] URL: obj = jsNative

let funcyfunc bob =
  bob + "bob"

let doMassiveCalculationWorker dispatch =

  //https://stackoverflow.com/questions/10343913/how-to-create-a-web-worker-from-a-string/10372280#10372280
  //https://github.com/fable-compiler/repl/blob/master/src/App/Generator.fs#L107
  let response = """window=self; self.importScripts('http://localhost:8080/bundle.js'); self.onmessage=function(e){postMessage('Worker: '+e.data);}"""

  //let x = Browser.Blob.Blob.Create()

  //let x = Browser.Blob.Blob.Create(response, response)

  let asString = JS.JSON.stringify(funcyfunc)

  JS.console.log(asString)

  let parts: obj[] = [| response |]
  
  let options =
      JsInterop.jsOptions<Browser.Types.BlobPropertyBag>(fun o ->
          o.``type`` <- "text/javascript")

  let blobUrl = URL?createObjectURL(Blob.Create(parts, options))

  let worker = Browser.Dom.Worker.Create(blobUrl)

  let funci (ev:Browser.Types.MessageEvent) =
    JS.console.log(ev.data)
    JS.console.log("got message")

  worker.onmessage <- funci

  worker.postMessage("hi")

  ()

let doMassiveCalculationWasm dispatch =

  (*import Module from './fibonacci.js'
    Module().then(function(mymod) {
        const fib = mymod.cwrap('fib', 'number', ['number']);
        console.log(fib(64));
    });*)

  FibonacciModule().``then``(fun bob ->
    let fib = bob?cwrap("fib", "number", ["number"])
    JS.console.log(fib(64)))
  |> ignore

  ()

let view (model:Model) dispatch =

  div
    []
    [
      div
        []
        [ button [ OnClick (fun _ -> dispatch Increment) ] [ str "+" ]
          div [] [ str (string model) ]
          button [ OnClick (fun _ -> dispatch Decrement) ] [ str "-" ] ]

      br []
      div
        []
        [
          button [ OnClick (fun _ -> doMassiveCalculation dispatch) ] [ str "Expensive calculation" ]
          button [ OnClick (fun _ -> doMassiveCalculationAsync dispatch) ] [ str "Expensive calculation (async)" ]
          button [ OnClick (fun _ -> doMassiveCalculationWorker dispatch) ] [ str "Expensive calculation (worker)" ]
          button [ OnClick (fun _ -> doMassiveCalculationWasm dispatch) ] [ str "Expensive calculation (wasm)" ]
        ]
    ]
  


// App
Program.mkSimple init update view
|> Program.withReactSynchronous "elmish-app"
//|> Program.withReactBatched "elmish-app"
|> Program.withConsoleTrace
|> Program.run
