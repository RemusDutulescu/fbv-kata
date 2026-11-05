# FunBooksAndVideos

A purchase order processor for an online shop that sells books, videos and club memberships. Each business rule is
a small class that decides what an order needs; a Temporal workflow carries it out, durably.

- **BR1**: a membership in an order is active by the time the submission answers.
- **BR2**: physical products get a shipping slip.

## Run it

Needs Docker.

```shell
docker compose up --build
```

- API docs, to try it: <http://localhost:5080>
- Temporal UI, to watch the workflows: <http://localhost:8233>

To run the API from source instead (needs the .NET 10 SDK), start only its dependencies:

```shell
docker compose up sqlserver temporal
dotnet run --project src/FunBooksAndVideos.Api
```

## Test it

```shell
dotnet test
```

Docker must be running: the tests start their own SQL Server container, and the Temporal SDK downloads a dev server
on the first run.
