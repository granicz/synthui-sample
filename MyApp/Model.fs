(*
let OrderDSL = "
    Item(ItemNumber: string; Name: string; Quantity: int)
    Order(Name: string; Address: string; Items: Item list)
    OrderPage: Order;
    "
*)

namespace SynthUI

open WebSharper

type EndPoint =
    | [<EndPoint "/order">] OrderPage

[<JavaScript>]
module Model =
    type Item =
        {
            ItemNumber: string
            Name: string
            Quantity: int
        }
        static member Default =
            { ItemNumber=""; Name=""; Quantity=0 }

    and Order =
        {
            Name: string
            Address: string
            Items: Item seq
        }
        static member Default =
            { Name=""; Address=""; Items=Seq.empty }

[<JavaScript>]
module Forms =
    open WebSharper.Forms
    open Model

    let ItemForm (init: Item) =
        Form.Return (fun itemNumber name quantity ->
            { ItemNumber = itemNumber; Name = name; Quantity = quantity }
        )
        <*> (Form.Yield init.ItemNumber
            |> Validation.IsNotEmpty "Quantity can not be empty")
        <*> Form.Yield init.Name
        <*> Form.Yield init.Quantity

    let OrderForm (init: Order) =
        Form.Return (fun name address items ->
            { Name = name; Address = address; Items = items }
        )
        <*> Form.Yield init.Name
        <*> Form.Yield init.Address
        <*> Form.Many init.Items Item.Default ItemForm
        |> Form.WithSubmit

