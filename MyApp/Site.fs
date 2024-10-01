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
