namespace TestRecords

open Chiron
open Chiron.Operators

type SimpleRecord =
    {
        Id      : int
        Name    : string
        Address : string
        Scores  : int[]
    }

    static member ToJson (x : SimpleRecord) =
        Json.write "id" x.Id
        *> Json.write "name" x.Name
        *> Json.write "address" x.Address
        *> Json.write "scores" x.Scores

    static member FromJson (_ : SimpleRecord) =
        (fun id name address scores ->
            {
                Id      = id
                Name    = name
                Address = address
                Scores  = scores
            })
        <!> Json.read "id"
        <*> Json.read "name"
        <*> Json.read "address"
        <*> Json.read "scores"