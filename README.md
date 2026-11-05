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
docker run --rm --add-host host.docker.internal:host-gateway -v "$PWD/postman:/etc/newman" postman/newman:6.1.3-alpine \
  run FunBooksAndVideos.postman_collection.json -e local.postman_environment.json \
  --env-var baseUrl=http://host.docker.internal:5080
```

## Deploy it

Azure Container Apps runs the API and its worker, Azure SQL holds the data, and Temporal Cloud runs the workflows.
[Terraform](infra/) creates all three, and the [Deploy workflow](.github/workflows/deploy.yml) ships every commit
that passes CI on `main`, then runs the Postman collection against it.

1. Push the repository to GitHub. CI runs, and Deploy pushes the image to `ghcr.io/<owner>/<repo>`; its Azure steps
   are skipped until step 4.
2. Make that package public (package settings, *Change visibility*), so Container Apps can pull it without
   credentials.
3. Create the infrastructure. It needs the Azure CLI signed in and a Temporal Cloud account API key:

   ```shell
   az login
   export TEMPORAL_CLOUD_API_KEY=<account API key>
   cd infra
   cp terraform.tfvars.example terraform.tfvars   # then fill it in
   terraform init
   terraform apply
   ```

4. Copy `terraform output github_variables` into the repository's Actions variables. They are identifiers, not
   secrets: Deploy signs in to Azure with OpenID Connect.
5. Re-run Deploy, or push again. `terraform output api_url` is the API.
