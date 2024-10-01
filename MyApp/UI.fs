namespace SynthUI

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating

type MyTemplate = Template<"main.html", serverLoad=ServerLoad.WhenChanged, clientLoad=ClientLoad.FromDocument>

[<JavaScript>]
module UI =
    open Model
    open Forms

    let ItemRender (init: Item) =
        ItemForm init
        |> Form.Render (fun itemNumber name quantity ->
            MyTemplate.ItemTemplate()
                .ItemNumber(itemNumber)
                .Name(name)
                .Quantity(quantity)
                .Doc()
        )

    let OrderRender (init: Order) run =
        if not IsClient then
            MyTemplate.OrderTemplate()
                //.CssClasses("animate-pulse")
                .Doc()
        else
            OrderForm init
            |> Form.Run run
            |> Form.Render (fun name address items submitter ->
                MyTemplate.OrderTemplate()
                    .Name(name)
                    .Address(address)
                    .Items(
                        items.Render
                            (fun ops itemNumber name quantity ->
                                MyTemplate.ItemTemplate()
                                    .ItemNumber(itemNumber)
                                    .Name(name)
                                    .Quantity(quantity)
                                    .Remove(
                                        MyTemplate.ItemTemplate_Remove()
                                            .Remove(fun _ -> ops.Delete())
                                            .Doc()
                                    )
                                    .Doc()
                        )
                    )
                    .Items_Add(
                        MyTemplate.OrderTemplate_Items_Add()
                            .Add(fun _ -> items.Add Item.Default)
                            .Doc()
                    )
                    .Submit(
                        MyTemplate.OrderTemplate_Submit()
                            .Submit(fun _ -> submitter.Trigger())
                            .Doc()
                    )
                    .Doc()
            )
