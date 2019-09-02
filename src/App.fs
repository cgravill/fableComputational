module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Browser.Blob
open Browser.Types
open Fulma


open Elmish.HMR

type Model = {
  count: int
  page: int //DU
}

type Msg =
| NextPage
| PreviousPage
| Increment
| Decrement
| MassiveCalculation
| MassiveCalculationAsync

let init() : Model =
  {count=0; page=1}

let update (msg:Msg) (model:Model) =
    match msg with
    | NextPage -> {model with page = model.page + 1}
    | PreviousPage -> {model with page = model.page - 1}
    | Increment -> {model with count = model.count + 1 }
    | Decrement -> {model with count = model.count - 1 }
    | MassiveCalculation ->
      model
    | MassiveCalculationAsync ->
      model

type IActualModule =
  abstract isAwesome: unit -> bool

[<Import("default", @"./wasm/fibonacci.js")>]
let FibonacciModule :unit -> JS.Promise<IActualModule> = jsNative

[<Import("default", @"./wasm/dna.js")>]
let DNAModule :unit -> JS.Promise<IActualModule> = jsNative

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
  let response = """window=self; self.onmessage=function(e){postMessage('Worker: '+e.data);}"""

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

let energyCaclulation dispatch =

  (*import Module from './fibonacci.js'
    Module().then(function(mymod) {
        const fib = mymod.cwrap('fib', 'number', ['number']);
        console.log(fib(64));
    });*)

  DNAModule().``then``(fun energyModule ->
    JS.console.log(energyModule?energyWrapped("GACCTTACC")))
  |> ignore

  ()

let page0 (model:Model) dispatch =

  Hero.hero
    [
      Hero.IsFullHeight ]
    [
      Hero.body
        [ ]
        [ Container.container [ Container.IsFluid
                                Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
            [ Heading.h1 [ ]
                [ str "Intensive browser computation and Fable" ]
              Heading.h2 [ Heading.IsSubtitle ]
                [ str "Colin Gravill (@cgravill)" ] ] ]
    ]

  

          

let page1 (model:Model) dispatch  =
  div
    []
    [
      h1
        []
        [str "Sample application"]

      div
        []
        [ 
          Button.button
            [ Button.Props [OnClick (fun _ -> dispatch Increment)] ]
            [ str "+" ]
          div [] [ str (string model.count) ]
          Button.button
            [ Button.Props [OnClick (fun _ -> dispatch Decrement)] ]
            [ str "-" ]

        ]

      br []
      div
        []
        [
          button [ OnClick (fun _ -> doMassiveCalculation dispatch) ] [ str "Expensive calculation" ]
          button [ OnClick (fun _ -> doMassiveCalculationAsync dispatch) ] [ str "Expensive calculation (async)" ]
          button [ OnClick (fun _ -> doMassiveCalculationWorker dispatch) ] [ str "Expensive calculation (worker)" ]
          button [ OnClick (fun _ -> doMassiveCalculationWasm dispatch) ] [ str "Expensive calculation (wasm)" ]
          button [ OnClick (fun _ -> energyCaclulation dispatch) ] [ str "Energy calculation (wasm)" ]
        ]
    ]


let view (model:Model) dispatch =
  match model.page with
    | 0 -> page0 model dispatch
    | 1 -> page1 model dispatch
    | _ -> page0 model dispatch
  
let inputs dispatch =
    let update (e : KeyboardEvent, pressed) =
        match e.key with
        | "w" -> Increment |> dispatch
        | "a" -> Decrement |> dispatch
        | "ArrowLeft" -> PreviousPage |> dispatch
        | "ArrowRight" -> NextPage |> dispatch
        | code ->
          JS.console.log(sprintf "Code: %s" code)
    Browser.Dom.document.addEventListener("keydown", fun e -> update(e :?> _, true))

// App
Program.mkSimple init update view
|> Program.withReactSynchronous "elmish-app"
//|> Program.withReactBatched "elmish-app"
|> Program.withSubscription (fun _ -> [ Cmd.ofSub inputs ] |> Cmd.batch)
|> Program.withConsoleTrace
|> Program.run
