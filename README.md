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
- Traces, from the request through the workflow to each rule: <http://localhost:18888>

To run the API from source instead (needs the .NET 10 SDK), start only what it depends on:

```shell
docker compose up sqlserver temporal dashboard
dotnet run --project src/FunBooksAndVideos.Api
```

## Test it

```shell
dotnet test
```

Docker must be running: the tests start their own SQL Server container, and the Temporal SDK downloads a dev server
on the first run.

The [Postman collection](postman/) walks through the brief's example and the edge cases, asserting every response.
Import it with the `local` environment, or run it headless against the running stack:

```shell
docker run --rm --add-host host.docker.internal:host-gateway -v "$PWD/postman:/etc/newman" postman/newman:6-alpine \
  run FunBooksAndVideos.postman_collection.json -e local.postman_environment.json \
  --env-var baseUrl=http://host.docker.internal:5080
```
