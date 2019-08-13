module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fable.Core

open Elmish.HMR

type Model = int

type Msg =
| Increment
| Decrement
| MassiveCalculation

let init() : Model = 0

let update (msg:Msg) (model:Model) =
    match msg with
    | Increment -> model + 1
    | Decrement -> model - 1
    | MassiveCalculation ->
      JS.console.time("calc")
      for i in 0L..5000000000L do
        ()
      JS.console.timeEnd("calc")
      model

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
          button [ OnClick (fun _ -> dispatch MassiveCalculation) ] [ str "Expensive calculation" ]
        ]
    ]
  


// App
Program.mkSimple init update view
|> Program.withReactSynchronous "elmish-app"
//|> Program.withReactBatched "elmish-app"
|> Program.withConsoleTrace
|> Program.run
