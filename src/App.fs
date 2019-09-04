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

open Monaco

open Elmish.HMR

type Model = {
  count: int64
  page: int
  primeFactors: int64[]
  outputs: List<string>
}

type Msg =
| NextPage
| PreviousPage
| Increment
| Decrement
| MassiveCalculation
| MassiveCalculationAsync
| ComputedPrimeFactors of int64[]
| UpdatedOutputs of string

let init() : Model =
  {count=0L; page=1; primeFactors=[||]; outputs=[]}

let maxPage = 20

let update (msg:Msg) (model:Model) =

    let adjustCountForDramaticalReasons page =
        match page with
        | 0 -> 0L
        | 2 -> 12L
        | 3 -> 3349L
        | 4 -> 5029784645645674576L
        | _ -> model.count

    match msg with
    | NextPage ->
      let newPage = min maxPage (model.page + 1)
      {model with page = newPage; count = adjustCountForDramaticalReasons newPage; primeFactors = [||]; outputs = [] }
    | PreviousPage ->
      let newPage = max 0 (model.page - 1)
      {model with page = newPage; count = adjustCountForDramaticalReasons newPage; primeFactors = [||]; outputs = [] }
    | Increment -> {model with count = model.count + 1L }
    | Decrement -> {model with count = model.count - 1L }
    | MassiveCalculation ->
      model
    | MassiveCalculationAsync ->
      model
    | ComputedPrimeFactors factors ->
      {model with primeFactors = factors}
    | UpdatedOutputs text ->
      {model with outputs = text :: model.outputs}

type IActualModule =
  abstract isAwesome: unit -> bool

//Better option: https://math.stackexchange.com/questions/185524/pollard-strassen-algorithm
let factorise n =
  let rec f number candidate acc = 
    if candidate = number then
        candidate::acc
    elif number % candidate = 0L then 
        f (number/candidate) candidate (candidate::acc)
    else
        f number (candidate+1L) acc
  f n 2L []
let factors (count:int64) = factorise count |> Array.ofList

let primeFactors count dispatch =
  dispatch (ComputedPrimeFactors (factors count))

let expensiveCalculationCode = """let expensiveCalculation dispatch =
  JS.console.time("calc")
  for i in 0L..20000000L do
    if i % 20000L = 0L then
      dispatch (UpdatedOutputs (string i))
      JS.console.log(i)
  JS.console.timeEnd("calc")
  dispatch MassiveCalculation"""

let expensiveCalculation dispatch =
  JS.console.time("calc")
  for i in 0L..20000000L do
    if i % 20000L = 0L then
      dispatch (UpdatedOutputs (string i))
      JS.console.log(i)
  JS.console.timeEnd("calc")
  dispatch MassiveCalculation

let expensiveCalculationAsyncCode = """let expensiveCalculationAsync dispatch =
  JS.console.time("calcAsync")
  async {
    JS.console.log("really started")
    for i in 0L..20000000L do
      if i % 20000L = 0L then
        dispatch (UpdatedOutputs (string i))
        JS.console.log(i)
    JS.console.timeEnd("calcAsync")
    dispatch MassiveCalculationAsync
  }
  |> Async.StartImmediate"""

let expensiveCalculationAsync dispatch =
  JS.console.time("calcAsync")
  async {
    JS.console.log("really started")
    for i in 0L..20000000L do
      if i % 20000L = 0L then
        dispatch (UpdatedOutputs (string i))
        JS.console.log(i)
    JS.console.timeEnd("calcAsync")
    dispatch MassiveCalculationAsync
  }
  |> Async.StartImmediate

let [<Global>] URL: obj = jsNative

let expensiveCalculationWorkerCode = """let expensiveCalculationWorker dispatch =

  //Needs to be self-contained, or otherwise arrange for called functions to be present via ImportScripts etc.
  let start() =
    self.onmessage <-
      fun e -> 
        self.postMessage("WorkerX: " + (string)e.data)
        for i = 0 to 20000000 do
          if i % 20000 = 0 then
            self.postMessage(i)

  //https://stackoverflow.com/questions/10343913/how-to-create-a-web-worker-from-a-string/10372280#10372280
  //https://github.com/fable-compiler/repl/blob/master/src/App/Generator.fs#L107
  //let response = "window=self; self.onmessage=function(e){postMessage('Worker: '+e.data);}"
  //let response = "self.onmessage=function(e){postMessage('Worker: '+e.data);}"
  let asString = start.ToString() + System.Environment.NewLine + "start();"
  
  JS.console.log(asString)

  let parts: obj[] = [| asString |]
  
  let options =
      JsInterop.jsOptions<Browser.Types.BlobPropertyBag>(fun o ->
          o.``type`` <- "text/javascript")

  let blobUrl = URL?createObjectURL(Blob.Create(parts, options))

  let worker = Browser.Dom.Worker.Create(blobUrl)

  let workerCallback (ev:Browser.Types.MessageEvent) =
    JS.console.log(ev.data)
    JS.console.log("got message")

  worker.onmessage <- workerCallback
  worker.postMessage("")"""

let [<Global>] self: Browser.Types.Worker = jsNative

let expensiveCalculationWorker dispatch =

  //Needs to be self-contained, or otherwise arrange for called functions to be present via ImportScripts etc.
  let start() =
    self.onmessage <-
      fun e -> 
        self.postMessage("WorkerX: " + (string)e.data)
        for i = 0 to 20000000 do
          if i % 20000 = 0 then
            self.postMessage(i)

  //https://stackoverflow.com/questions/10343913/how-to-create-a-web-worker-from-a-string/10372280#10372280
  //https://github.com/fable-compiler/repl/blob/master/src/App/Generator.fs#L107
  let asString = start.ToString() + System.Environment.NewLine + "start();"

  JS.console.log(asString)

  let parts: obj[] = [| asString |]
  
  let options =
      JsInterop.jsOptions<Browser.Types.BlobPropertyBag>(fun o ->
          o.``type`` <- "text/javascript")

  let blobUrl = URL?createObjectURL(Blob.Create(parts, options))

  let worker = Browser.Dom.Worker.Create(blobUrl)

  let workerCallback (ev:Browser.Types.MessageEvent) =
    dispatch (UpdatedOutputs (string ev.data))

  worker.onmessage <- workerCallback
  worker.postMessage("")

let doExpensiveCalculationWasmCode = """[<Import("default", @"./wasm/fibonacci.js")>]
let FibonacciModule :unit -> JS.Promise<IActualModule> = jsNative

let doExpensiveCalculationWasm dispatch =

  (*import Module from './fibonacci.js'
    Module().then(function(mymod) {
        const fib = mymod.cwrap('fib', 'number', ['number']);
        console.log(fib(64));
    });*)

  FibonacciModule().``then``(fun fibonacciModule ->
    let fib = fibonacciModule?cwrap("fib", "number", ["number"])
    JS.console.log(fib(64)))
  |> ignore"""

[<Import("default", @"./wasm/fibonacci.js")>]
let FibonacciModule :unit -> JS.Promise<IActualModule> = jsNative

let doExpensiveCalculationWasm dispatch =

  (*import Module from './fibonacci.js'
    Module().then(function(mymod) {
        const fib = mymod.cwrap('fib', 'number', ['number']);
        console.log(fib(64));
    });*)

  FibonacciModule().``then``(fun fibonacciModule ->
    let fib = fibonacciModule?cwrap("fib", "number", ["number"])
    JS.console.log(fib(64)))
  |> ignore

let energyCalculationCode = """[<Import("default", @"./wasm/dna.js")>]
let DNAModule :unit -> JS.Promise<IActualModule> = jsNative

let energyCaclulation dispatch =

  DNAModule().``then``(fun energyModule ->
    JS.console.log(energyModule?energyWrapped("GACCTTACC")))
  |> ignore"""

[<Import("default", @"./wasm/dna.js")>]
let DNAModule :unit -> JS.Promise<IActualModule> = jsNative

let energyCaclulation dispatch =

  DNAModule().``then``(fun energyModule ->
    JS.console.log(energyModule?energyWrapped("GACCTTACC")))
  |> ignore

let private fsharpEditorOptions (fontSize : float) (fontFamily : string) =
  jsOptions<Monaco.Editor.IEditorConstructionOptions>(fun o ->
      let minimapOptions = jsOptions<Monaco.Editor.IEditorMinimapOptions>(fun oMinimap ->
          oMinimap.enabled <- Some false
      )
      o.language <- Some "fsharp"
      o.fontSize <- Some fontSize
      o.theme <- Some "vs-dark"
      o.minimap <- Some minimapOptions
      o.fontFamily <- Some fontFamily
      o.fontLigatures <- Some (fontFamily = "Fira Code")
      o.fixedOverflowWidgets <- Some true
  )

let fsharpEditor model dispatch code =
  div
      [ Style [Height "500px"] ]
      [
        ReactEditor.editor [
          ReactEditor.Options (fsharpEditorOptions 20.0 "Fira Code")
          ReactEditor.Value code
        ]
    ]

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
                [ str "Colin Gravill (@cgravill)" ] ]
        ]
    ]


let sampleApplication (count:int64) dispatch =
  div
    []
    [
      

      br []

      div
        []
        [ 
          Button.button
            [ Button.Props [OnClick (fun _ -> dispatch Increment)] ]
            [ str "+" ]
          div [] [ str (string count) ]
          Button.button
            [ Button.Props [OnClick (fun _ -> dispatch Decrement)] ]
            [ str "-" ]

        ]
    ]

let pageTitle (model:Model) dispatch  =
  Hero.hero
    [
      Hero.IsFullHeight ]
    [
      Hero.body
        [ ]
        [ Container.container [ Container.IsFluid
                                Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
            
            [sampleApplication model.count dispatch]
        
        ]
    ]



let pageCounterGeneral (model:Model) dispatch  =

  let contents =
    [
      sampleApplication model.count dispatch
      br []
      Button.button
        [ Button.Props [OnClick (fun _ -> primeFactors model.count dispatch)] ]
        [ str "Prime factors" ]

      h1
        []
        [str (string model.primeFactors)]
    ]

  Hero.hero
    [
      Hero.IsFullHeight ]
    [
      Hero.body
        [ ]
        [ Container.container [ Container.IsFluid
                                Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]

            contents
        ]
    ]

let pageCounter1 = pageCounterGeneral
let pageCounter2 = pageCounterGeneral 
let pageCounter3 = pageCounterGeneral 

let pageGeneral code dispatchFunc text (model:Model) dispatch =
  let outputs =
    model.outputs
    |> List.rev
    |> String.concat "\n"
  let content =
    [
      sampleApplication model.count dispatch
      br []
      div
        []
        [
          fsharpEditor model dispatch code

          Button.button
            [ Button.Props [OnClick (fun _ -> dispatchFunc dispatch)] ]
            [ str text ]

          pre
              []
              [str (outputs.ToString())]
        ]
    ]

  Container.container [ Container.IsFluid ]
    [
      Content.content
        []
        content
    ]

let pageExpensiveCalculation = pageGeneral expensiveCalculationCode expensiveCalculation "Expensive calculation"
let pageExpensiveCalculationAsync = pageGeneral expensiveCalculationAsyncCode expensiveCalculationAsync "Expensive calculation (async)"
let pageWorker = pageGeneral expensiveCalculationWorkerCode expensiveCalculationWorker "Expensive calculation (web worker)"
let pageWasm = pageGeneral doExpensiveCalculationWasmCode doExpensiveCalculationWasm "Expensive calculation (wasm)"


let pageEnergyCalculation (model:Model) dispatch  =
  div
    []
    [
      sampleApplication model.count dispatch

      br []

      div
        []
        [
          fsharpEditor model dispatch energyCalculationCode
          Button.button
            [ Button.Props [OnClick (fun _ -> energyCaclulation dispatch)] ]
            [ str "Energy calculation (wasm)" ]
        ]
    ]

let pageSummary (model:Model) dispatch  =
  div
    []
    [
      sampleApplication model.count dispatch
    ]

let pages =
  [
    page0
    pageTitle
    pageCounter1
    pageCounter2
    pageCounter3
    pageExpensiveCalculation
    pageExpensiveCalculationAsync
    pageWorker
    pageWasm
    pageEnergyCalculation
    pageSummary
  ]

let view (model:Model) dispatch =
  let page = 
    match pages |> Seq.tryItem model.page with
    | Some page -> page
    | None -> Seq.last pages
  page model dispatch
  
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
