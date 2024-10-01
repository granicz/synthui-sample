Here is a sample DSL defining forms that can be nested. Each form can have one or more typed fields. The available types are basic types ("string", "int", and "boolean", for now), and list of another form or basic type.

    Item(ItemNumber: string; Name: string; Quantity: int)
    Order(From: string; FromAddress: string; Items: Item list)
    OrderPage: Order;

This defines an Item and Order form, and declares that an Order page should also be constructed based on the Order form.

From any DSL text, we create four files:

* Model.fs, containing the model types for each form, the endpoint type for a sitelet that encompasses every page declared, and abstract UI code for each form (WebSharper.Forms code that returns a Forms.Form value, without an actual Render call)
* "UI.fs" for actual render functions for each form that use WebSharper UI templates. Avoid generating HTML combinators. The forms that are defined to be pages will generate render functions that take a runner argument, that determines what to do with the result value of the form.
* "Client.fs" for functions that create each page on the client side with a dummy runner that logs each result on the JavaScript console.
* "Site.fs" to create a sitelet that exposes every page on separate sitelet endpoints.

Now, using the above DSL code, I will give you the content of each of the above four files. When you generate code, always follow the same style and format, never deviate from it.

Note that all four generated files will be in the same namespace. If the user requests a given namespace, use that, otherwise use SynthUI.

Model.fs:

```
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
```

Note that forms that are declared to be pages will call `Form.WithSubmit`, and those that are not won't. This ensures that pages can submit data.

UI.fs:

```
namespace SynthUI

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating

type MyTemplate = Template<"main.html", serverLoad=ServerLoad.WhenChanged, clientLoad=ClientLoad.Inline>

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
```

Note the "UI" module and how it's attributed with `[<JavaScript>]`, and how it contains `<form>Render` functions. Always use this structure, never deviate from it. Each render function receives the given form value to initialize itself from, and forms defined to be pages have an additional "run" argument as mentioned above. Each render function should start with a condition that checks whether the code is being run on the server or not, and if so, it should return the corresponding form template as is to cater to a basic form of server-side rendering.

The render functions you generate must always be based on WebSharper UI templates. For this purpose, you must always use a `MyTemplate` type and referencing `Main.html` in the templating type provider's argument, along with the options I used above. You must assume that this master template file always contains inner templates for every form we define using our DSL, and that every such inner template has the same placeholders as what's defined as field names for the given form.

You must generate code with the same structure as shown above for lists, using <xxx>.Render, where <xxx> is the formal parameter that stands for the given list. Underneath the hood, these have type `WebSharper.Forms.Form.Many.CollectionWithDefault<_>`, yielded by `Form.Many` in the code we showed in `Model.fs`.

Collection fields also need special treatment about the add/remove functionality. This requires that templates for forms that are sequenced in other forms have a `Remove` placeholder, which will contain the markup for a remove widget. This then must be accompanied by a `<form>Template_Remove` template, which gives this markup, along with a `Remove` event handler that is called when needed.

Similarly, templates for forms that have collection fields must have an `Add` placeholder, and a corresponding `<form>Template_<field>_Add` template with an `Add` event handler.

As shown in the above code, you must generate the `Add` and `Remove` event handlers correctly.

"Client.fs":

```fsharp
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
```

This file must have a `[<JavaScript>]`-annotated `Client` module, with `<form>Page ()` functions that construct test/dummy data and render their corresponding forms with it, then log the resulting values on the JavaScript console. Do NOT use the above data, use clean test data instead.

"Site.fs":

```fsharp
namespace SynthUI

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Server

module Site =
    open type WebSharper.UI.ClientServer

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.OrderPage ->
                Content.Page(
                    MyTemplate()
                        .Body(hydrate (Client.OrderPage ()))
                        .Doc()
                )
        )
```

This file must have the same structure as shown, with a `Site` module that contains a `[<Website>]`-annotated `Main` value for a sitelet that exposes each declared page as a standalone sitelet endpoint. Each such page must return the master template, and assume that it has a `Body` placeholder to render the given page into it. This rendered content must be hydrated.

