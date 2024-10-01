namespace SynthUI

open WebSharper

[<JavaScript>]
module Client =

    let OrderPage () =
        let item1 =
            {
                Model.Item.ItemNumber = "a1234"
                Model.Item.Name = "Blue ball"
                Model.Item.Quantity = 12
            }
        let item2 = { item1 with ItemNumber = "a1235"; Name = "Red ball" }
        let order =
            {
                Model.Order.Name = "Henry Smith"
                Model.Order.Address = "123 Main St., Diamond, MO 64840, USA"
                Model.Order.Items = [item1; item2]
            }
        UI.OrderRender order
            <| fun order ->
                JavaScript.Console.Log (sprintf "%A" (List.ofSeq order.Items))
